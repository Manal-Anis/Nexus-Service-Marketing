using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>
/// The feasibility queue of the technical department. A Dial-Up order of a customer without a
/// Nexus landline carries two checks (landline and internet), every other order carries one.
/// </summary>
public class FeasibilityController : VendorControllerBase
{
    private readonly FeasibilityService _feasibility;
    private readonly OrderService _orders;

    public FeasibilityController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        FeasibilityService feasibility, OrderService orders)
        : base(db, currentUser, audit)
    {
        _feasibility = feasibility;
        _orders = orders;
    }

    /// <summary>Lists the orders that wait for a feasibility result.</summary>
    public async Task<IActionResult> Index(string? orderId = null)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanOperate(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Include(o => o.Plan!.Category)
            .Include(o => o.Customer)
            .Include(o => o.City)
            .Include(o => o.FeasibilityChecks)
            .Where(o => o.VendorId == vendorId.Value && !o.IsDeleted)
            .Where(o => o.Status == OrderStatus.Placed || o.Status == OrderStatus.FeasibilityPending)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync();

        // The order that was opened from the dashboard is moved to the top of the list.
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            var selected = orders.FirstOrDefault(o => o.OrderId == orderId);
            if (selected is not null)
            {
                orders.Remove(selected);
                orders.Insert(0, selected);
            }
        }

        ViewData["Title"] = "Feasibility queue";
        ViewData["Subtitle"] = "Record the survey result - a connection can only be created on a feasible order.";
        ViewData["SelectedOrderId"] = orderId;

        return View(orders);
    }

    /// <summary>Records the result of a single feasibility check.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(FeasibilityCheckViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanOperate(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var check = await Db.Orders
            .Include(o => o.FeasibilityChecks)
            .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.VendorId == vendorId.Value);

        if (check is null)
        {
            TempData["Error"] = "This order does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var order = check;
        var entry = order.FeasibilityChecks.FirstOrDefault(c => c.Id == model.CheckId);

        if (entry is null)
        {
            TempData["Error"] = "The check could not be found.";
            return RedirectToAction(nameof(Index), new { orderId = order.OrderId });
        }

        entry.Result = model.Result;
        entry.DistanceParameters = model.DistanceParameters;
        entry.ServerAvailability = model.ServerAvailability;
        entry.CompanyRules = model.CompanyRules;
        entry.Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? entry.Remarks : model.Remarks;
        entry.CheckedByUserId = UserId;
        entry.CheckedByName = UserName;
        entry.CheckedAt = DateTime.Now;

        // The aggregate result of the order is worked out from every check.
        var result = _feasibility.ResolveResult(order.FeasibilityChecks);
        order.FeasibilityStatus = result;

        if (result != FeasibilityResult.Pending)
        {
            var newStatus = _feasibility.MapToOrderStatus(result);
            await _orders.ChangeStatusAsync(order, newStatus,
                result == FeasibilityResult.Feasible
                    ? "Every required feasibility check passed."
                    : "At least one required feasibility check failed.",
                UserId, UserName);
        }
        else if (order.Status == OrderStatus.Placed)
        {
            await _orders.ChangeStatusAsync(order, OrderStatus.FeasibilityPending,
                "The survey has been started.", UserId, UserName);
        }
        else
        {
            await Db.SaveChangesAsync();
        }

        await Audit.LogAsync("Feasibility.Recorded", "Order", order.OrderId, UserId, UserName, IpAddress,
            $"{entry.CheckType.ToDisplayName()} check recorded as {model.Result.ToDisplayName()}.");

        TempData["Success"] = result == FeasibilityResult.Pending
            ? $"{entry.CheckType.ToDisplayName()} check saved. The order waits for the remaining check."
            : $"The survey of {order.OrderId} is complete - the order is now {result.ToDisplayName().ToLowerInvariant()}.";

        return RedirectToAction(nameof(Index), new { orderId = order.OrderId });
    }
}
