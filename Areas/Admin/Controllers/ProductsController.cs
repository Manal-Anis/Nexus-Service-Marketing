using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Admin.Controllers;

/// <summary>The equipment master and the stock of the network.</summary>
public class ProductsController : AdminControllerBase
{
    public ProductsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the equipment with the stock and the reorder level.</summary>
    public async Task<IActionResult> Index(string? search = null, string? stock = null)
    {
        var query = Db.Products.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) ||
                                     (p.Type != null && p.Type.Contains(term)));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        if (stock == "low")
        {
            products = products.Where(p => p.StockQuantity <= p.ReorderLevel).ToList();
        }
        else if (stock == "out")
        {
            products = products.Where(p => p.StockQuantity <= 0).ToList();
        }

        ViewData["Title"] = "Equipment";
        ViewData["Subtitle"] = "The master data of the devices and the stock of the store room.";
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentStock"] = stock;
        ViewData["LowStockCount"] = await Db.Products.AsNoTracking()
            .CountAsync(p => !p.IsDeleted && p.IsActive && p.StockQuantity <= p.ReorderLevel);
        ViewData["OutOfStockCount"] = await Db.Products.AsNoTracking()
            .CountAsync(p => !p.IsDeleted && p.IsActive && p.StockQuantity <= 0);

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormModelAsync(new ProductFormViewModel());

        ViewData["Title"] = "Register a product";
        ViewData["Subtitle"] = "The device that is handed over to the customer with the installation.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        await PopulateFormAsync(model);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Register a product";
            return View(model);
        }

        var product = new Product
        {
            Name = model.Name.Trim(),
            Type = model.Type?.Trim(),
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            ReorderLevel = model.ReorderLevel,
            ReplacementCharge = model.ReplacementCharge,
            SupplierId = model.SupplierId,
            Description = model.Description?.Trim(),
            IsActive = model.IsActive
        };

        Db.Products.Add(product);
        await Db.SaveChangesAsync();

        await Audit.LogAsync("Product.Created", "Product", product.Id.ToString(), UserId, UserName, IpAddress,
            $"The product {product.Name} was registered with {product.StockQuantity} unit(s) in stock.");

        TempData["Success"] = $"The product {product.Name} has been registered.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await Db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product is null)
        {
            return Missing("This product does not exist.");
        }

        var model = await BuildFormModelAsync(new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Type = product.Type,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ReorderLevel = product.ReorderLevel,
            ReplacementCharge = product.ReplacementCharge,
            SupplierId = product.SupplierId,
            Description = product.Description,
            IsActive = product.IsActive
        });

        ViewData["Title"] = $"Edit {product.Name}";
        ViewData["Subtitle"] = "The master data of the device.";

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
    {
        await PopulateFormAsync(model);

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == model.Id && !p.IsDeleted);

        if (product is null)
        {
            return Missing("This product does not exist.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = $"Edit {product.Name}";
            return View(model);
        }

        product.Name = model.Name.Trim();
        product.Type = model.Type?.Trim();
        product.Price = model.Price;
        product.StockQuantity = model.StockQuantity;
        product.ReorderLevel = model.ReorderLevel;
        product.ReplacementCharge = model.ReplacementCharge;
        product.SupplierId = model.SupplierId;
        product.Description = model.Description?.Trim();
        product.IsActive = model.IsActive;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Product.Updated", "Product", product.Id.ToString(), UserId, UserName, IpAddress,
            $"The product {product.Name} was edited.");

        TempData["Success"] = $"{product.Name} has been updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Adjusts the stock of a product. The stock can never become negative.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int id, int delta, string? reason)
    {
        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product is null)
        {
            return Missing("This product does not exist.");
        }

        if (product.StockQuantity + delta < 0)
        {
            TempData["Error"] = "The stock can never become negative.";
            return RedirectToAction(nameof(Index));
        }

        var before = product.StockQuantity;
        product.StockQuantity += delta;

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Product.StockAdjusted", "Product", product.Id.ToString(), UserId, UserName, IpAddress,
            $"The stock of {product.Name} was moved from {before} to {product.StockQuantity}. {reason}");

        TempData["Success"] = $"The stock of {product.Name} is now {product.StockQuantity} unit(s).";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductFormViewModel> BuildFormModelAsync(ProductFormViewModel model)
    {
        await PopulateFormAsync(model);
        return model;
    }

    private async Task PopulateFormAsync(ProductFormViewModel model)
    {
        model.Suppliers = await Db.Suppliers.AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
