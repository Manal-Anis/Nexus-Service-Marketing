using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>The equipment of the partner: the stock and the devices that are issued.</summary>
public class EquipmentController : VendorControllerBase
{
    public EquipmentController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the stock of the equipment and the issued devices.</summary>
    public async Task<IActionResult> Index(string? search = null)
    {
        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var products = await Db.Products.AsNoTracking()
            .Include(p => p.Supplier)
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            products = products.Where(p => p.Name.Contains(term) || (p.Type != null && p.Type.Contains(term))).ToList();
        }

        var assignments = await Db.ConnectionEquipment.AsNoTracking()
            .Include(e => e.Product)
            .Include(e => e.Connection)
            .Include(e => e.Connection!.Customer)
            .Where(e => e.Connection != null && e.Connection.VendorId == vendorId.Value)
            .OrderByDescending(e => e.AssignedAt)
            .ToListAsync();

        var model = new VendorEquipmentViewModel
        {
            Products = products,
            Assignments = assignments,
            LowStockCount = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.ReorderLevel),
            OutOfStockCount = products.Count(p => p.StockQuantity <= 0),
            TotalUnits = products.Sum(p => p.StockQuantity),
            AssignedUnits = assignments.Count(e => !e.IsReplaced)
        };

        ViewData["Title"] = "Equipment";
        ViewData["Subtitle"] = "The stock of the equipment and the devices that are issued on your connections.";
        ViewData["CurrentSearch"] = search;

        return View(model);
    }
}
