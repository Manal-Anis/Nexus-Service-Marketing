using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Vendor.Controllers;

/// <summary>
/// The counter receipts of the partner. Nexus is a cash on delivery network, therefore the only
/// thing that happens here is the registration of the cash received at the counter.
/// </summary>
public class PaymentsController : VendorControllerBase
{
    private readonly PaymentService _payments;
    private readonly ConnectionService _connections;

    public PaymentsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit,
        PaymentService payments, ConnectionService connections)
        : base(db, currentUser, audit)
    {
        _payments = payments;
        _connections = connections;
    }

    /// <summary>Shows the receipts and the security deposit ledger of the partner.</summary>
    public async Task<IActionResult> Index(string? status = null)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanCollect(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var bills = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .Include(b => b.Connection!.Plan)
            .Where(b => b.Connection != null && b.Connection.VendorId == vendorId.Value)
            .OrderByDescending(b => b.GeneratedOn)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BillStatus>(status, true, out var parsed))
        {
            bills = bills.Where(b => b.Status == parsed).ToList();
        }

        var payments = await Db.Payments.AsNoTracking()
            .Include(p => p.Bill)
            .Include(p => p.Bill!.Connection)
            .Where(p => p.Bill != null && p.Bill.Connection != null && p.Bill.Connection.VendorId == vendorId.Value)
            .OrderByDescending(p => p.PaidOn)
            .ToListAsync();

        var deposits = await Db.SecurityDepositEntries.AsNoTracking()
            .Include(d => d.Connection)
            .Include(d => d.Order)
            .Where(d => d.Connection != null && d.Connection.VendorId == vendorId.Value)
            .OrderByDescending(d => d.HeldOn)
            .ToListAsync();

        var model = new VendorPaymentsViewModel
        {
            Bills = bills,
            Payments = payments,
            Deposits = deposits,
            TotalCollected = payments.Sum(p => p.Amount),
            TotalOutstanding = bills.Where(b => b.Status == BillStatus.Due).Sum(b => b.AmountDue),
            DepositsHeld = deposits.Where(d => d.Status == SecurityDepositStatus.Held).Sum(d => d.NetAmount),
            DepositsRefunded = deposits.Where(d => d.Status == SecurityDepositStatus.Refunded).Sum(d => d.NetAmount)
        };

        ViewData["Title"] = "Payments";
        ViewData["Subtitle"] = "Cash received at your counter and the refundable deposits you hold.";
        ViewData["CurrentStatus"] = status;

        return View(model);
    }

    /// <summary>Opens the form that registers a receipt against an invoice.</summary>
    [HttpGet]
    public async Task<IActionResult> Record(int billId)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanCollect(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var bill = await Db.Bills.AsNoTracking()
            .Include(b => b.Connection)
            .Include(b => b.Connection!.Customer)
            .FirstOrDefaultAsync(b => b.Id == billId && b.Connection != null && b.Connection.VendorId == vendorId.Value);

        if (bill is null)
        {
            TempData["Error"] = "This invoice does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var model = new RecordPaymentViewModel
        {
            BillId = bill.Id,
            BillNumber = bill.BillNumber,
            AccountId = bill.Connection?.AccountId ?? string.Empty,
            CustomerName = bill.Connection?.Customer?.FullName ?? string.Empty,
            AmountDue = bill.AmountDue,
            TotalAmount = bill.TotalAmount,
            Amount = bill.AmountDue
        };

        ViewData["Title"] = "Record a receipt";
        ViewData["Subtitle"] = "Cash on delivery - the customer pays at the counter.";

        return View(model);
    }

    /// <summary>Stores the cash receipt and updates the state of the invoice.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(RecordPaymentViewModel model)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanCollect(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var bill = await Db.Bills
            .Include(b => b.Connection)
            .FirstOrDefaultAsync(b => b.Id == model.BillId && b.Connection != null && b.Connection.VendorId == vendorId.Value);

        if (bill is null)
        {
            TempData["Error"] = "This invoice does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model.BillNumber = bill.BillNumber;
            model.AccountId = bill.Connection?.AccountId ?? string.Empty;
            model.TotalAmount = bill.TotalAmount;
            ViewData["Title"] = "Record a receipt";
            return View(model);
        }

        var result = await _payments.RecordPaymentAsync(bill, model.Amount, UserId, UserName,
            model.Remarks, model.PaidOn);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The receipt could not be recorded.");
            model.BillNumber = bill.BillNumber;
            model.AccountId = bill.Connection?.AccountId ?? string.Empty;
            model.TotalAmount = bill.TotalAmount;
            ViewData["Title"] = "Record a receipt";
            return View(model);
        }

        await Audit.LogAsync("Payment.Recorded", "Bill", bill.BillNumber, UserId, UserName, IpAddress,
            $"{model.Amount:N2} Rs received in cash. Receipt {result.ReceiptNumber}.");

        // The overdue rule is applied again, a paid bill may restore a suspended connection.
        await _connections.ApplyOverdueRuleAsync(vendorId);

        TempData["Success"] = $"Receipt {result.ReceiptNumber} has been recorded. {result.AmountDue:N2} Rs are still due.";
        return RedirectToAction("Details", "Billing", new { area = "Vendor", id = bill.Id });
    }

    /// <summary>Marks the whole outstanding amount of an invoice as paid.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPaid(int billId)
    {
        var type = await GetEmployeeTypeAsync();
        var denied = DenyIfNotAllowed(VendorPermissions.CanCollect(type));
        if (denied is not null)
        {
            return denied;
        }

        var vendorId = await GetVendorIdAsync();
        if (vendorId is null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        var bill = await Db.Bills
            .Include(b => b.Connection)
            .FirstOrDefaultAsync(b => b.Id == billId && b.Connection != null && b.Connection.VendorId == vendorId.Value);

        if (bill is null)
        {
            TempData["Error"] = "This invoice does not belong to your service desk.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _payments.MarkAsPaidAsync(bill, UserId, UserName);

        if (!result.Success)
        {
            TempData["Error"] = result.Error ?? "The invoice could not be marked as paid.";
            return RedirectToAction(nameof(Index));
        }

        await Audit.LogAsync("Payment.MarkAsPaid", "Bill", bill.BillNumber, UserId, UserName, IpAddress,
            $"The invoice was marked as fully paid. Receipt {result.ReceiptNumber}.");

        // The overdue rule is applied again, a paid bill may restore a suspended connection.
        await _connections.ApplyOverdueRuleAsync(vendorId);

        TempData["Success"] = $"Receipt {result.ReceiptNumber} has been recorded - the invoice {bill.BillNumber} is fully paid.";
        return RedirectToAction("Details", "Billing", new { area = "Vendor", id = bill.Id });
    }
}
