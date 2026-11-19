using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>
/// The purchase orders of the store room. The order runs through Draft, Placed and Received, and
/// the stock of the network grows when the goods are received.
/// </summary>
public class PurchaseOrdersController : AdminControllerBase
{
    private readonly SequenceService _sequence;

    public PurchaseOrdersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        SequenceService sequence)
        : base(db, currentUser, audit)
    {
        _sequence = sequence;
    }

    /// <summary>Lists the purchase orders.</summary>
    public async Task<IActionResult> Index(PurchaseOrderStatus? status = null)
    {
        var query = Db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .Where(p => !p.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var orders = await query.OrderByDescending(p => p.OrderDate).ToListAsync();

        ViewData["Title"] = "Purchase orders";
        ViewData["Subtitle"] = "The restocking of the store room and the money owed to the suppliers.";
        ViewData["CurrentStatus"] = status;

        return View(orders);
    }

    /// <summary>Shows the detail of one purchase order.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var order = await Db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (order is null)
        {
            return Missing("This purchase order does not exist.");
        }

        ViewData["Title"] = order.PurchaseOrderNumber;
        ViewData["Subtitle"] = $"{order.Supplier?.Name} - {order.TotalAmount:N2} Rs.";

        return View(order);
    }

    /// <summary>Opens the form that raises a new purchase order.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormModelAsync(new PurchaseOrderFormViewModel());

        ViewData["Title"] = "Raise a purchase order";
        ViewData["Subtitle"] = "The order starts as a draft and can be placed afterwards.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderFormViewModel model)
    {
        await PopulateFormAsync(model);

        model.Items = model.Items.Where(i => i.ProductId > 0 && i.Quantity > 0).ToList();

        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Items), "Please add at least one line with a product and a quantity.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Raise a purchase order";
            return View(model);
        }

        var serial = await _sequence.NextSerialAsync("PURCHASE");
        var order = new PurchaseOrder
        {
            PurchaseOrderNumber = $"PO-{DateTime.Today.Year}-{serial.ToString("D4")}",
            SupplierId = model.SupplierId,
            OrderDate = model.OrderDate,
            ExpectedDate = model.ExpectedDate,
            Status = PurchaseOrderStatus.Draft,
            PaymentStatus = model.PaymentStatus,
            CreatedByUserId = UserId,
            CreatedAt = DateTime.Now
        };

        foreach (var item in model.Items)
        {
            var product = await Db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product is null)
            {
                continue;
            }

            order.Items.Add(new PurchaseOrderItem
            {
                Product = product,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = Math.Round(item.Quantity * item.UnitCost, 2)
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.LineTotal);
        order.AmountDue = order.TotalAmount;

        Db.PurchaseOrders.Add(order);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("PurchaseOrder.Created", "PurchaseOrder", order.PurchaseOrderNumber, UserId, UserName, IpAddress,
            $"The purchase order {order.PurchaseOrderNumber} was raised for {order.TotalAmount:N2} Rs.");

        TempData["Success"] = $"The purchase order {order.PurchaseOrderNumber} has been raised.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>Sends the draft order to the supplier.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(int id)
    {
        var order = await Db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (order is null)
        {
            return Missing("This purchase order does not exist.");
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            TempData["Error"] = "Only a draft order can be placed with the supplier.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        order.Status = PurchaseOrderStatus.Placed;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("PurchaseOrder.Placed", "PurchaseOrder", order.PurchaseOrderNumber, UserId, UserName, IpAddress,
            $"The purchase order {order.PurchaseOrderNumber} was placed with the supplier.");

        TempData["Success"] = $"The purchase order {order.PurchaseOrderNumber} was placed with the supplier.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>Receives the goods - the stock of the network grows by the ordered quantity.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id)
    {
        var order = await Db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (order is null)
        {
            return Missing("This purchase order does not exist.");
        }

        if (order.Status == PurchaseOrderStatus.Received)
        {
            TempData["Error"] = "This purchase order was already received.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        foreach (var item in order.Items)
        {
            var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product is null)
            {
                continue;
            }

            product.StockQuantity += item.Quantity;
        }

        order.Status = PurchaseOrderStatus.Received;
        order.ReceivedOn = DateTime.Now;
        await Db.SaveChangesAsync();

        await Audit.LogAsync("PurchaseOrder.Received", "PurchaseOrder", order.PurchaseOrderNumber, UserId, UserName, IpAddress,
            $"The goods of {order.PurchaseOrderNumber} were received, the stock was increased.");

        TempData["Success"] = $"The goods of {order.PurchaseOrderNumber} were received and the stock was increased.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>Records the payment that was made to the supplier.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, decimal amount)
    {
        var order = await Db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (order is null)
        {
            return Missing("This purchase order does not exist.");
        }

        if (amount <= 0m)
        {
            TempData["Error"] = "Please enter the amount that was paid to the supplier.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        order.AmountPaid = Math.Min(order.TotalAmount, order.AmountPaid + amount);
        order.AmountDue = Math.Max(0m, order.TotalAmount - order.AmountPaid);
        order.PaymentStatus = order.AmountDue <= 0m ? SupplierPaymentStatus.Paid : SupplierPaymentStatus.Partial;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("PurchaseOrder.Paid", "PurchaseOrder", order.PurchaseOrderNumber, UserId, UserName, IpAddress,
            $"{amount:N2} Rs were paid to the supplier of {order.PurchaseOrderNumber}.");

        TempData["Success"] = $"{amount:N2} Rs have been paid to the supplier.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>
    /// The smart reorder suggestion: the products whose stock reached the reorder level, with the
    /// quantity that should be ordered.
    /// </summary>
    public async Task<IActionResult> Reorder()
    {
        var products = await Db.Products.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var suggestions = products
            .Where(p => p.StockQuantity <= p.ReorderLevel)
            .Select(p => new ReorderSuggestionViewModel
            {
                Product = p,
                SuggestedQuantity = Math.Max(1, (p.ReorderLevel * 2) - p.StockQuantity),
                Severity = p.StockQuantity <= 0 ? "Out of stock" : "Below the reorder level"
            })
            .OrderBy(s => s.Product.StockQuantity)
            .ToList();

        ViewData["Title"] = "Reorder suggestion";
        ViewData["Subtitle"] = "The products that reached their reorder level.";

        return View(suggestions);
    }

    private async Task<PurchaseOrderFormViewModel> BuildFormModelAsync(PurchaseOrderFormViewModel model)
    {
        await PopulateFormAsync(model);
        return model;
    }

    private async Task PopulateFormAsync(PurchaseOrderFormViewModel model)
    {
        model.Suppliers = await Db.Suppliers.AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        model.Products = await Db.Products.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
