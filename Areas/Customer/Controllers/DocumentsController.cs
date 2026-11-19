using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Controllers;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Areas.Customer.Controllers;

/// <summary>The documents the customer uploaded against the orders.</summary>
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DocumentsController : BaseAreaController
{
    private readonly FileStorageService _files;

    public DocumentsController(ApplicationDbContext db, CurrentUserService currentUser, AuditService audit, FileStorageService files)
        : base(db, currentUser, audit)
    {
        _files = files;
    }

    /// <summary>Lists the uploaded documents and offers the upload form.</summary>
    public async Task<IActionResult> Index()
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var documents = await Db.OrderDocuments.AsNoTracking()
            .Include(d => d.Order)
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == customerId && !o.IsDeleted)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        ViewData["Title"] = "My documents";
        ViewData["Subtitle"] = "Identity proofs and other papers. Only PDF, JPG and PNG files up to 2 MB are accepted.";

        return View(new DocumentUploadViewModel { Orders = orders, Documents = documents });
    }

    /// <summary>Stores an uploaded document outside the public web folder.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        var orders = await Db.Orders.AsNoTracking()
            .Include(o => o.Plan)
            .Where(o => o.CustomerId == customerId && !o.IsDeleted)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();

        model.Orders = orders;
        model.Documents = await Db.OrderDocuments.AsNoTracking()
            .Include(d => d.Order)
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Please choose a file.");
            return View(nameof(Index), model);
        }

        if (model.OrderId.HasValue && !orders.Any(o => o.Id == model.OrderId.Value))
        {
            ModelState.AddModelError(nameof(model.OrderId), "This order does not belong to your account.");
            return View(nameof(Index), model);
        }

        var customer = await Db.Customers.AsNoTracking().Include(c => c.City).FirstAsync(c => c.Id == customerId);
        var cityName = customer.City?.Name ?? "General";
        var referenceId = model.OrderId.HasValue ? orders.First(o => o.Id == model.OrderId.Value).OrderId : customerId.ToString();

        var upload = await _files.SaveAsync(model.File, DateTime.Now.Year, cityName, referenceId);

        if (!upload.Success)
        {
            ModelState.AddModelError(nameof(model.File), upload.Error ?? "The file could not be stored.");
            return View(nameof(Index), model);
        }

        Db.OrderDocuments.Add(new OrderDocument
        {
            OrderId = model.OrderId,
            CustomerId = customerId,
            DocumentType = model.DocumentType,
            OriginalFileName = model.File.FileName,
            StoredFileName = upload.StoredFileName ?? string.Empty,
            RelativePath = upload.RelativePath ?? string.Empty,
            FileSizeBytes = upload.FileSizeBytes,
            ContentType = upload.ContentType,
            UploadedByUserId = profile.UserId
        });

        await Db.SaveChangesAsync();

        await Audit.LogAsync("Document.Uploaded", "OrderDocument", referenceId, profile.UserId, profile.Email, IpAddress,
            $"Document '{model.File.FileName}' uploaded.");

        TempData["Success"] = "The document has been stored and sent to the partner.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Opens an uploaded document after the ownership has been checked.</summary>
    public async Task<IActionResult> Download(int id)
    {
        var profile = await CurrentUser.GetProfileAsync();
        if (profile.CustomerId is not int customerId)
        {
            return Forbid();
        }

        var document = await Db.OrderDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.CustomerId == customerId);

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

        await Audit.LogAsync("Document.Downloaded", "OrderDocument", document.Id.ToString(), profile.UserId, profile.Email, IpAddress,
            $"Document '{document.OriginalFileName}' opened.");

        return PhysicalFile(physicalPath, document.ContentType ?? "application/octet-stream", document.OriginalFileName);
    }
}
