using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

// =================================================================================================
// NEXUS SERVICE MARKETING SYSTEM - application startup
// ASP.NET Core MVC (.NET 10) + ASP.NET Core Identity + Entity Framework Core (Code First)
// =================================================================================================

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------------------------
// 1. DATABASE - SQL Server (LocalDB by default, SQL Server Express is supported as well)
// ---------------------------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("The connection string 'DefaultConnection' is missing from appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name);
    });

    // Detailed errors are helpful while the project is being developed.
    options.EnableDetailedErrors();
});

// ---------------------------------------------------------------------------------------------
// 2. IDENTITY - users, roles and the password / lockout policy
// ---------------------------------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "Nexus.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // A visitor of the /Admin area lands on the separate administrator login page.
    options.Events.OnRedirectToLogin = context =>
    {
        var target = context.Request.Path.StartsWithSegments("/Admin") ? "/Account/AdminLogin" : "/Account/Login";
        var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
        context.Response.Redirect($"{target}?returnUrl={returnUrl}");
        return Task.CompletedTask;
    };
});

// ---------------------------------------------------------------------------------------------
// 3. APPLICATION SERVICES - all business logic lives outside the controllers
// ---------------------------------------------------------------------------------------------
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<SequenceService>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<FeasibilityService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<CsvExportService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ConnectionService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<SupportService>();
builder.Services.AddScoped<SearchService>();

builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// ---------------------------------------------------------------------------------------------
// 4. MVC - controllers, views and the three dashboard areas
// ---------------------------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // Every POST is protected with an antiforgery token.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// ---------------------------------------------------------------------------------------------
// 5. MIDDLEWARE PIPELINE
// ---------------------------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------------------------------------
// 6. ROUTING - the three dashboard areas and the public pages
// ---------------------------------------------------------------------------------------------
// The three dashboards are reached through their area name ("/Admin/...", "/Vendor/...").
// The public pages keep the plain "{controller}/{action}" route, so a link of the website
// never carries an area name.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------------------------------------------------------------------------------------
// 7. DATABASE CREATION AND DEMO DATA SEEDING - the application is ready after the first start
// ---------------------------------------------------------------------------------------------
await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

/// <summary>
/// Exposed so the integration tests can start the application with a WebApplicationFactory.
/// </summary>
public partial class Program
{
}
