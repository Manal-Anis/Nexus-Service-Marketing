using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>The customers that are served by the partner.</summary>
public class CustomersController : VendorControllerBase
{
    private readonly FileStorageService _files;

    public CustomersController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        FileStorageService files)
        : base(db, currentUser, audit)
    {
        _files = files;
    }

    /// <summary>Lists the customers of the partner.</summary>
    public async Task<IActionResult> Index(string? search = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var customerIds = await Db.Orders.AsNoTracking()
            .Where(o => o.VendorId == vendorId.Value && !o.IsDeleted)
            .Select(o => o.CustomerId)
            .Distinct()
            .ToListAsync();

        var query = Db.Customers.AsNoTracking()
            .Include(c => c.City)
            .Where(c => customerIds.Contains(c.Id) && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.FullName.Contains(term) ||
                                     (c.Phone != null && c.Phone.Contains(term)) ||
                                     (c.Email != null && c.Email.Contains(term)) ||
                                     (c.Address != null && c.Address.Contains(term)));
        }

        var customers = await query.OrderBy(c => c.FullName).ToListAsync();

        var connections = await Db.Connections.AsNoTracking()
            .Where(c => c.VendorId == vendorId.Value)
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Where(b => b.Connection != null && b.Connection.VendorId == vendorId.Value)
            .ToListAsync();

        var model = new VendorCustomersViewModel
        {
            Customers = customers,
            Search = search,
            TotalCount = customers.Count,
            ActiveConnections = connections.Count(c => c.Status == ConnectionStatus.Active),
            OutstandingAmount = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue)
        };

        ViewData["Title"] = "Customers";
        ViewData["Subtitle"] = "The customers your desk serves.";

        return View(model);
    }

    /// <summary>Shows the orders, connections, invoices and documents of one customer.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var customer = await Db.Customers.AsNoTracking()
            .Include(c => c.City)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (customer is null)
        {
            TempData["Error"] = "This customer does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == id && o.VendorId == vendorId.Value && !o.IsDeleted)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        var connections = await Db.Connections.AsNoTracking()
            .Include(c => c.Plan)
            .Where(c => c.CustomerId == id && c.VendorId == vendorId.Value)
            .ToListAsync();

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Where(b => b.Connection != null && b.Connection.CustomerId == id && b.Connection.VendorId == vendorId.Value)
            .OrderByDescending(b => b.GeneratedOn)
            .ToListAsync();

        var documents = await Db.OrderDocuments.AsNoTracking()
            .Include(d => d.Order)
            .Where(d => d.CustomerId == id && d.Order != null && d.Order.VendorId == vendorId.Value)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var model = new VendorCustomerDetailsViewModel
        {
            Customer = customer,
            Orders = orders,
            Connections = connections,
            Bills = bills,
            Documents = documents,
            TotalBilled = bills.Sum(b => b.TotalAmount),
            TotalPaid = bills.Sum(b => b.AmountPaid),
            TotalDue = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue)
        };

        ViewData["Title"] = customer.FullName;
        ViewData["Subtitle"] = $"{customer.City?.Name ?? "-"} - {customer.Phone ?? "-"}";

        return View(model);
    }

    /// <summary>Opens a document of one of the customers of the partner.</summary>
    public async Task<IActionResult> Document(int id)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return Forbid();
        }

        var document = await Db.OrderDocuments.AsNoTracking()
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == id && d.Order != null && d.Order.VendorId == vendorId.Value);

        if (document is null)
        {
            return NotFound();
        }

        var physicalPath = _files.ResolvePhysicalPath(document.RelativePath);
        if (physicalPath is null || !System.IO.File.Exists(physicalPath))
        {
            TempData["Error"] = "The stored file could not be found.";
            return RedirectToAction(nameof(Index));
        }

        await Audit.LogAsync("Document.Downloaded", "OrderDocument", document.Id.ToString(), UserId, UserName, IpAddress,
            $"Document '{document.OriginalFileName}' opened by the partner.");

        return PhysicalFile(physicalPath, document.ContentType ?? "application/octet-stream", document.OriginalFileName);
    }
}
