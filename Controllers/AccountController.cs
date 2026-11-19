using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;
using NexusServiceMarketingSystem.ViewModels;

namespace NexusServiceMarketingSystem.Controllers;

/// <summary>Registration, sign in and password management.</summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly CatalogService _catalog;
    private readonly AuditService _audit;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db, CatalogService catalog, AuditService audit, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _catalog = catalog;
        _audit = audit;
        _logger = logger;
    }

    // ---------------------------------------------------------------- sign in
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "The email address or the password is not correct.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, await DescribeLockedAccountAsync(user, model.Password));
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _audit.LogAsync("Account.SignIn", "Account", user.Id, user.Id, user.Email ?? model.Email, HttpContext.Connection.RemoteIpAddress?.ToString(), "Signed in successfully.");
            return RedirectToDashboard(returnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "The account is temporarily locked after five failed attempts. Please try again in ten minutes.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "The email address or the password is not correct.");
        return View(model);
    }

    // ---------------------------------------------------------------- administrator sign in
    /// <summary>Separate sign in page of the administrators ("/Account/AdminLogin").</summary>
    [AllowAnonymous]
    public IActionResult AdminLogin(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        const string notAdmin = "These details do not belong to an administrator account.";

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive || !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            ModelState.AddModelError(string.Empty, notAdmin);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _audit.LogAsync("Account.AdminSignIn", "Account", user.Id, user.Id, user.Email ?? model.Email, HttpContext.Connection.RemoteIpAddress?.ToString(), "Administrator signed in.");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                && returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "The account is temporarily locked after five failed attempts. Please try again in ten minutes."
            : notAdmin);
        return View(model);
    }

    // ---------------------------------------------------------------- sign out
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        await _signInManager.SignOutAsync();
        return Redirect(returnUrl ?? "/");
    }

    // ---------------------------------------------------------------- register
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        await PopulateCitiesAsync();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        await PopulateCitiesAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            Address = model.Address.Trim(),
            CityId = model.CityId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        // The customer profile that carries the orders and the connections.
        _db.Customers.Add(new Customer
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Address = user.Address,
            CityId = model.CityId,
            IsActive = true,
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        await _audit.LogAsync("Account.Register", "Account", user.Id, user.Id, user.Email ?? email, HttpContext.Connection.RemoteIpAddress?.ToString(), "Customer account created.");

        TempData["Success"] = "Welcome to Nexus. Your account is ready, you can order a plan now.";
        return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
    }

    // ---------------------------------------------------------------- forgot password
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // The mail server is not part of the project, so the reset link is shown on the page
            // and written to the log for the demonstration.
            var link = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, code }, Request.Scheme);

            TempData["ResetLink"] = link;
            _logger.LogWarning("Password reset requested for {Email}. Reset link: {Link}", user.Email, link);
        }

        return View("ForgotPasswordConfirmation");
    }

    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ---------------------------------------------------------------- reset password
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? code = null)
    {
        if (email is null || code is null)
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = code });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "The password reset request is no longer valid.");
            return View(model);
        }

        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _audit.LogAsync("Account.PasswordReset", "Account", user.Id, user.Id, user.Email ?? model.Email, HttpContext.Connection.RemoteIpAddress?.ToString(), "Password reset by the owner of the account.");
        TempData["Success"] = "Your password has been changed. You can sign in now.";
        return RedirectToAction(nameof(Login));
    }

    // ---------------------------------------------------------------- change password
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        await _audit.LogAsync("Account.ChangePassword", "Account", user.Id, user.Id, user.Email, HttpContext.Connection.RemoteIpAddress?.ToString(), "Password changed by the owner of the account.");

        TempData["Success"] = "Your password has been updated.";
        return RedirectToAction(nameof(ChangePassword));
    }

    // ---------------------------------------------------------------- access denied
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------------------------------------------------------------- vendor registration
    [AllowAnonymous]
    public async Task<IActionResult> RegisterVendor()
    {
        await PopulateCitiesAsync();
        return View(new VendorRegistrationViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterVendor(VendorRegistrationViewModel model)
    {
        await PopulateCitiesAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();

        // One email address belongs to one account or one registration request only.
        if (await _userManager.FindByEmailAsync(email) is not null
            || await _db.Vendors.AnyAsync(v => v.Email == email && !v.IsDeleted))
        {
            ModelState.AddModelError(nameof(model.Email), "An account or a registration with this email address already exists.");
            return View(model);
        }

        // The login account and the vendor record are saved together or not at all.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.OwnerName.Trim(),
            Phone = model.Phone.Trim(),
            Address = model.Address.Trim(),
            CityId = model.CityId,
            IsActive = false // locked until the administrator approves the partner
        };

        var created = await _userManager.CreateAsync(user, model.Password);

        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Vendor");

        var vendor = new Vendor
        {
            UserId = user.Id,
            BusinessName = model.BusinessName.Trim(),
            OwnerName = model.OwnerName.Trim(),
            Phone = model.Phone.Trim(),
            Email = email,
            Address = model.Address.Trim(),
            CityId = model.CityId,
            Status = VendorStatus.Pending,
            StatusRemarks = "Registration request: " + model.Details.Trim()
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _audit.LogAsync("Vendor.Registered", "Vendor", vendor.Id.ToString(), user.Id, email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            $"The partner {vendor.BusinessName} sent a registration request.");

        model.Submitted = true;
        return View(model);
    }

    // ---------------------------------------------------------------- helpers
    /// <summary>
    /// Explains why a locked account cannot sign in. The reason of a partner account (waiting for the
    /// approval, rejected or blocked) is only revealed after the correct password was typed.
    /// </summary>
    private async Task<string> DescribeLockedAccountAsync(ApplicationUser user, string password)
    {
        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            return "The email address or the password is not correct.";
        }

        var vendor = await _db.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(v => v.UserId == user.Id && !v.IsDeleted);

        if (vendor is not null)
        {
            switch (vendor.Status)
            {
                case VendorStatus.Pending:
                    return "Your partner registration is still waiting for the approval of the administrator. You can sign in as soon as it is approved.";
                case VendorStatus.Rejected:
                    return "Your partner registration was rejected. " + vendor.StatusRemarks;
                case VendorStatus.Blocked:
                    return "Your partner account has been blocked by the administrator. Please contact the support desk.";
            }
        }

        return "This account has been blocked. Please contact the support desk.";
    }

    private async Task PopulateCitiesAsync()
    {
        var cities = await _catalog.GetActiveCitiesAsync();
        ViewBag.Cities = cities;
    }

    private IActionResult RedirectToDashboard(string? returnUrl)
    {
        // The start page is not a useful place after the sign in, the user lands on the dashboard.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            && returnUrl != "/" && !returnUrl.StartsWith("/Home", StringComparison.OrdinalIgnoreCase)
            && !BelongsToAnotherRole(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (User.IsInRole("Vendor"))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Vendor" });
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
    }

    /// <summary>
    /// True when the address points to the dashboard of a role that the visitor does not own.
    /// Such an address would end on the access denied page, the visitor lands on his own dashboard.
    /// </summary>
    private bool BelongsToAnotherRole(string returnUrl)
    {
        var area = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Vendor") ? "Vendor" : "Customer";

        foreach (var other in new[] { "Admin", "Vendor", "Customer" })
        {
            if (!string.Equals(other, area, StringComparison.OrdinalIgnoreCase)
                && returnUrl.StartsWith("/" + other, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

}
