using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The receipts of the signed in customer.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class PaymentsController : BaseAreaController
{
    public PaymentsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit)
        : base(db, currentUser, audit)
    {
    }

    /// <summary>Lists the payments of the customer.</summary>
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var payments = await Db.Payments.AsNoTracking()
            .Include(p => p.Bill)
            .Include(p => p.Bill!.Connection)
            .Where(p => p.Bill != null && p.Bill.Connection != null && p.Bill.Connection.CustomerId == customerId)
            .OrderByDescending(p => p.PaidOn)
            .ToListAsync();

        var deposits = await Db.SecurityDepositEntries.AsNoTracking()
            .Include(d => d.Connection)
            .Include(d => d.Order)
            .Where(d => d.Connection != null && d.Connection.CustomerId == customerId)
            .OrderByDescending(d => d.HeldOn)
            .ToListAsync();

        ViewData["Title"] = "Payments and receipts";
        ViewData["Subtitle"] = "The cash payments that were received for your account.";

        return View(new CustomerPaymentsViewModel { Payments = payments, Deposits = deposits });
    }
}
