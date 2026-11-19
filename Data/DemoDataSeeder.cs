using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NexusServiceMarketingSystem.Models;
using NexusServiceMarketingSystem.Services;

namespace NexusServiceMarketingSystem.Data;

/// <summary>
/// Inserts the demo data of the Nexus Service Marketing System: roles, settings, cities, the full
/// service catalogue, vendors, employees, customers, orders, connections, bills, payments and
/// feedback. The data is inserted only once - on the very first start of the application.
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>Password of the seeded administrator account.</summary>
    public const string AdminPassword = "Admin@123";

    /// <summary>Password of the seeded vendor owner account.</summary>
    public const string VendorPassword = "Vendor@123";

    /// <summary>Password of the seeded employee accounts.</summary>
    public const string StaffPassword = "Staff@123";

    /// <summary>Password of the seeded customer accounts.</summary>
    public const string CustomerPassword = "Customer@123";

    /// <summary>Setting key that marks a completed seeding run.</summary>
    private const string SeedMarkerKey = "System.SeededVersion";

    /// <summary>
    /// Removes the demo rows of a half finished seeding run so the catalogue can be inserted
    /// again. The rows are deleted from the child tables towards the parent tables.
    /// </summary>
    private static async Task ResetDemoDataAsync(ApplicationDbContext db)
    {
        // The order of the statements follows the foreign key dependencies of the schema.
        var tables = new[]
        {
            "BillLines", "Payments", "SecurityDepositEntries", "Bills",
            "ConnectionEquipment", "PlanChangeHistory", "Connections",
            "OrderDocuments", "OrderFeasibilityChecks", "OrderStatusHistory", "Orders",
            "Feedback", "PurchaseOrderItems", "PurchaseOrders", "Plans",
            "Products", "Employees", "RetailShops", "Vendors", "Customers",
            "Suppliers", "ServiceCategories", "DiscountRules", "Cities",
            "SequenceCounters", "ContactMessages", "AuditLogs", "Settings"
        };

        // The identity accounts keep a link to their city, that link is cleared before the cities go.
        await db.Database.ExecuteSqlRawAsync("UPDATE [AspNetUsers] SET [CityId] = NULL");

        foreach (var table in tables)
        {
            // The table names come from the constant list above, they are never user input.
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
#pragma warning restore EF1002
        }
    }

    /// <summary>Seeds the whole demo catalogue.</summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var sequence = provider.GetRequiredService<SequenceService>();

        // The demo data is inserted only once - the marker setting proves that the seeding ran.
        if (await db.Settings.AnyAsync(s => s.Key == SeedMarkerKey))
        {
            return;
        }

        // A first start that was interrupted leaves the database half filled. In that case the
        // incomplete rows are removed and the catalogue is inserted from scratch.
        if (await db.Cities.AnyAsync())
        {
            await ResetDemoDataAsync(db);
        }

        // ======================================================================= 1. ROLES
        foreach (var roleName in new[] { "Admin", "Vendor", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // ======================================================================= 2. SETTINGS
        var settings = new[]
        {
            new Setting { Key = SettingsService.TaxPercentageKey, Value = "12.24", Group = "Billing", Description = "Service tax applied to the whole bill." },
            new Setting { Key = SettingsService.CurrencyKey, Value = "Rs", Group = "General", Description = "Currency symbol used on every screen and invoice." },
            new Setting { Key = SettingsService.OverdueDaysKey, Value = "30", Group = "Billing", Description = "Days after the due date before a connection is flagged as overdue." },
            new Setting { Key = SettingsService.PaymentGraceDaysKey, Value = "15", Group = "Billing", Description = "Grace days allowed on a freshly generated bill." },
            new Setting { Key = SettingsService.SupportPhoneKey, Value = "111-123456", Group = "General", Description = "Nexus customer care number." },
            new Setting { Key = SettingsService.SupportEmailKey, Value = "support@nexus.com", Group = "General", Description = "Nexus customer care email." },
            new Setting { Key = SettingsService.CompanyAddressKey, Value = "Nexus Communication System, I.I. Chundrigar Road, Karachi", Group = "General", Description = "Head office address." }
        };
        db.Settings.AddRange(settings);

        // ======================================================================= 3. DISCOUNT RULES
        db.DiscountRules.AddRange(
            new DiscountRule { MinConnections = 1, MaxConnections = 9, DiscountPercent = 0m, Description = "Fewer than 10 connections - no bulk discount." },
            new DiscountRule { MinConnections = 10, MaxConnections = 15, DiscountPercent = 25m, Description = "10 to 15 connections - 25% discount." },
            new DiscountRule { MinConnections = 16, MaxConnections = 25, DiscountPercent = 50m, Description = "16 to 25 connections - 50% discount." },
            new DiscountRule { MinConnections = 26, MaxConnections = 50, DiscountPercent = 75m, Description = "26 to 50 connections - 75% discount." },
            new DiscountRule { MinConnections = 51, MaxConnections = null, DiscountPercent = 100m, Description = "51 connections and above - 100% discount." });

        // ======================================================================= 4. CITIES
        var cityNames = new[]
        {
            ("Karachi", "001"), ("Lahore", "002"), ("Islamabad", "003"), ("Rawalpindi", "004"),
            ("Faisalabad", "005"), ("Multan", "006"), ("Peshawar", "007"), ("Quetta", "008"),
            ("Hyderabad", "009"), ("Sialkot", "010")
        };
        var cities = cityNames.Select(c => new City { Name = c.Item1, Code3 = c.Item2 }).ToList();
        db.Cities.AddRange(cities);

        // ======================================================================= 5. SERVICE CATEGORIES
        var dialUp = new ServiceCategory
        {
            Name = "Dial-Up Internet", Code = "D", Icon = "bi-dial-up", SortOrder = 1,
            SecurityDeposit = 3250m,
            Description = "Internet access over the ordinary telephone line. A Nexus landline is compulsory."
        };
        var broadband = new ServiceCategory
        {
            Name = "Broadband Internet", Code = "B", Icon = "bi-wifi", SortOrder = 2,
            SecurityDeposit = 5000m,
            Description = "Always on internet connection with a dedicated DSLAM port at your street."
        };
        var landline = new ServiceCategory
        {
            Name = "Landline Connection", Code = "T", Icon = "bi-telephone", SortOrder = 3,
            SecurityDeposit = 2500m,
            Description = "Fixed telephone line with local, STD and mobile messaging call charges."
        };
        db.ServiceCategories.AddRange(dialUp, broadband, landline);

        // ======================================================================= 6. SEQUENCE COUNTERS
        foreach (var letter in new[] { "D", "T", "B" })
        {
            db.SequenceCounters.Add(new SequenceCounter { Name = $"ORDER-{letter}", Prefix = letter, CurrentValue = 0 });
            db.SequenceCounters.Add(new SequenceCounter { Name = $"ACCOUNT-{letter}", Prefix = letter, CurrentValue = 0 });
        }
        db.SequenceCounters.Add(new SequenceCounter { Name = "BILL", Prefix = "INV", CurrentValue = 0 });
        db.SequenceCounters.Add(new SequenceCounter { Name = "RECEIPT", Prefix = "RCP", CurrentValue = 0 });

        // The cities, the categories and the counters are stored first, because the suppliers,
        // the products and the plans reference them through their foreign keys.
        await db.SaveChangesAsync();

        // ======================================================================= 7. SUPPLIERS
        var suppliers = new[]
        {
            new Supplier { Name = "Delta Networks Pvt. Ltd.", ContactName = "Imran Qureshi", Phone = "0300-1122334", Email = "sales@deltanetworks.pk", Address = "Plot 22, Korangi Industrial Area, Karachi", CityId = cities[0].Id },
            new Supplier { Name = "Commsgear Technologies", ContactName = "Sana Javed", Phone = "0321-5566778", Email = "info@commsgear.pk", Address = "Industrial Area, Lahore", CityId = cities[1].Id },
            new Supplier { Name = "Signal Works", ContactName = "Bilal Ahmed", Phone = "0333-4455667", Email = "orders@signalworks.pk", Address = "I-9 Industrial Estate, Islamabad", CityId = cities[2].Id }
        };
        db.Suppliers.AddRange(suppliers);
        await db.SaveChangesAsync();

        // ======================================================================= 8. PRODUCTS / EQUIPMENT
        var products = new[]
        {
            new Product { Name = "Wi-Fi Router", Type = "Router", Price = 4500m, StockQuantity = 40, ReorderLevel = 15, ReplacementCharge = 4500m, SupplierId = suppliers[0].Id, Icon = "bi-router", Description = "Dual band Wi-Fi router for a broadband connection." },
            new Product { Name = "ADSL Modem", Type = "Modem", Price = 3000m, StockQuantity = 35, ReorderLevel = 12, ReplacementCharge = 3000m, SupplierId = suppliers[0].Id, Icon = "bi-hdd-network", Description = "ADSL2+ modem used with the Wi-Fi router." },
            new Product { Name = "Dial-Up Modem", Type = "Modem", Price = 1800m, StockQuantity = 25, ReorderLevel = 10, ReplacementCharge = 1800m, SupplierId = suppliers[2].Id, Icon = "bi-modem", Description = "External dial-up modem with a fast connect chip." },
            new Product { Name = "Premium Dual-Band Router", Type = "Router", Price = 8500m, StockQuantity = 18, ReorderLevel = 8, ReplacementCharge = 8500m, SupplierId = suppliers[0].Id, Icon = "bi-router-fill", Description = "Premium dual band router with a gigabit switch." },
            new Product { Name = "Wi-Fi Range Extender", Type = "Accessory", Price = 3200m, StockQuantity = 22, ReorderLevel = 8, ReplacementCharge = 3200m, SupplierId = suppliers[1].Id, Icon = "bi-broadcast-pin", Description = "Extends the wireless coverage inside a large house." },
            new Product { Name = "Ethernet Patch Cable (5m)", Type = "Accessory", Price = 350m, StockQuantity = 120, ReorderLevel = 40, ReplacementCharge = 350m, SupplierId = suppliers[1].Id, Icon = "bi-ethernet", Description = "Cat 6 patch cable used between the modem and the router." },
            new Product { Name = "Power Adapter", Type = "Accessory", Price = 600m, StockQuantity = 90, ReorderLevel = 30, ReplacementCharge = 600m, SupplierId = suppliers[1].Id, Icon = "bi-plug", Description = "Replacement power supply for the modem or router." },
            new Product { Name = "Splitter Kit", Type = "Accessory", Price = 450m, StockQuantity = 60, ReorderLevel = 20, ReplacementCharge = 450m, SupplierId = suppliers[2].Id, Icon = "bi-bezier2", Description = "ADSL splitter used for the voice and data ports." },
            new Product { Name = "Wireless USB Adapter", Type = "Accessory", Price = 1450m, StockQuantity = 28, ReorderLevel = 10, ReplacementCharge = 1450m, SupplierId = suppliers[2].Id, Icon = "bi-usb-symbol", Description = "USB adapter for a desktop computer without Wi-Fi." },
            new Product { Name = "VoIP Handset", Type = "Telephone", Price = 2100m, StockQuantity = 30, ReorderLevel = 10, ReplacementCharge = 2100m, SupplierId = suppliers[1].Id, Icon = "bi-telephone-fill", Description = "Digital desk phone supplied with a landline connection." }
        };
        db.Products.AddRange(products);

        await db.SaveChangesAsync();

        // ======================================================================= 9. VENDORS AND STAFF
        var vendors = new[]
        {
            new Vendor { BusinessName = "Nexus Connect Retail", OwnerName = "Hamid Rahman", Phone = "0321-1234567", Email = "vendor1@nexus.com", Address = "Shop 14, Zamzam Plaza, Karachi", CityId = cities[0].Id, Status = VendorStatus.Approved },
            new Vendor { BusinessName = "SmartLink Communications", OwnerName = "Nadia Farooq", Phone = "0300-7654321", Email = "vendor2@nexus.com", Address = "54-B, Blue Area, Lahore", CityId = cities[1].Id, Status = VendorStatus.Approved },
            new Vendor { BusinessName = "Metro Tele Services", OwnerName = "Kashif Iqbal", Phone = "0333-2221100", Email = "vendor3@nexus.com", Address = "Office 7, F-6 Markaz, Islamabad", CityId = cities[2].Id, Status = VendorStatus.Approved },
            new Vendor { BusinessName = "SkyNet Broadband Point", OwnerName = "Ayesha Siddiqui", Phone = "0345-9988776", Email = "vendor4@nexus.com", Address = "Shop 3, Rania Plaza, Rawalpindi", CityId = cities[3].Id, Status = VendorStatus.Approved },
            new Vendor { BusinessName = "Falak Communication (Pending)", OwnerName = "Zeeshan Ali", Phone = "0311-8877665", Email = "pending.vendor@nexus.com", Address = "Multan", CityId = cities[5].Id, Status = VendorStatus.Pending }
        };
        db.Vendors.AddRange(vendors);
        await db.SaveChangesAsync();

        // The owner of the first vendor signs in with the documented demo credentials.
        var vendorOwner = await CreateUserAsync(userManager, "vendor1@nexus.com", VendorPassword, "Hamid Rahman", "+92 321 1234567", "Vendor");
        vendors[0].UserId = vendorOwner.Id;
        db.Vendors.Update(vendors[0]);

        var owner2 = await CreateUserAsync(userManager, "vendor2@nexus.com", VendorPassword, "Nadia Farooq", "+92 300 7654321", "Vendor");
        vendors[1].UserId = owner2.Id;
        db.Vendors.Update(vendors[1]);

        var owner3 = await CreateUserAsync(userManager, "vendor3@nexus.com", VendorPassword, "Kashif Iqbal", "+92 333 2221100", "Vendor");
        vendors[2].UserId = owner3.Id;
        db.Vendors.Update(vendors[2]);

        var owner4 = await CreateUserAsync(userManager, "vendor4@nexus.com", VendorPassword, "Ayesha Siddiqui", "+92 345 9988776", "Vendor");
        vendors[3].UserId = owner4.Id;
        db.Vendors.Update(vendors[3]);

        // ---------------------------------------------------------------- retail shops
        var shops = new[]
        {
            new RetailShop { VendorId = vendors[0].Id, Name = "Nexus Connect - Zamzam Plaza", CityId = cities[0].Id, Address = "Shop 14, Zamzam Plaza, Karachi", Phone = "0321-1234567", ContactPerson = "Hamid Rahman" },
            new RetailShop { VendorId = vendors[0].Id, Name = "Nexus Connect - North Karachi", CityId = cities[0].Id, Address = "Shop 3, Star Plaza, North Karachi", Phone = "0321-1234570", ContactPerson = "Sajid Khan" },
            new RetailShop { VendorId = vendors[1].Id, Name = "SmartLink - Blue Area", CityId = cities[1].Id, Address = "54-B, Blue Area, Lahore", Phone = "0300-7654321", ContactPerson = "Nadia Farooq" },
            new RetailShop { VendorId = vendors[2].Id, Name = "Metro Tele - F-6 Markaz", CityId = cities[2].Id, Address = "Office 7, F-6 Markaz, Islamabad", Phone = "0333-2221100", ContactPerson = "Kashif Iqbal" },
            new RetailShop { VendorId = vendors[3].Id, Name = "SkyNet - Rania Plaza", CityId = cities[3].Id, Address = "Shop 3, Rania Plaza, Rawalpindi", Phone = "0345-9988776", ContactPerson = "Ayesha Siddiqui" }
        };
        db.RetailShops.AddRange(shops);

        // The shops are stored first, because the employees reference them through their foreign key.
        await db.SaveChangesAsync();

        // ---------------------------------------------------------------- employees of every type
        var employees = new List<Employee>
        {
            // Vendor 1 - the manager plus one employee of every type.
            new Employee { VendorId = vendors[0].Id, EmployeeType = EmployeeType.Manager, Designation = "Vendor Owner", JoiningDate = new DateTime(2018, 4, 2), Phone = "+92 321 1234567" },
            new Employee { VendorId = vendors[0].Id, RetailShopId = shops[0].Id, EmployeeType = EmployeeType.Technical, Designation = "Senior Technician", JoiningDate = new DateTime(2020, 7, 15), Phone = "+92 321 1234568" },
            new Employee { VendorId = vendors[0].Id, RetailShopId = shops[0].Id, EmployeeType = EmployeeType.Accounts, Designation = "Accounts Officer", JoiningDate = new DateTime(2021, 1, 11), Phone = "+92 321 1234569" },
            new Employee { VendorId = vendors[0].Id, RetailShopId = shops[0].Id, EmployeeType = EmployeeType.Retail, Designation = "Counter Executive", JoiningDate = new DateTime(2022, 9, 5), Phone = "+92 321 1234571" },
            // Vendor 2 - 3
            new Employee { VendorId = vendors[1].Id, EmployeeType = EmployeeType.Technical, Designation = "Field Technician", JoiningDate = new DateTime(2019, 6, 20), Phone = "+92 300 7654322" },
            new Employee { VendorId = vendors[1].Id, EmployeeType = EmployeeType.Accounts, Designation = "Billing Assistant", JoiningDate = new DateTime(2021, 5, 3), Phone = "+92 300 7654323" },
            new Employee { VendorId = vendors[1].Id, RetailShopId = shops[2].Id, EmployeeType = EmployeeType.Retail, Designation = "Sales Executive", JoiningDate = new DateTime(2023, 2, 14), Phone = "+92 300 7654324" },
            new Employee { VendorId = vendors[2].Id, EmployeeType = EmployeeType.Technical, Designation = "Network Technician", JoiningDate = new DateTime(2020, 11, 9), Phone = "+92 333 2221101" },
            new Employee { VendorId = vendors[2].Id, EmployeeType = EmployeeType.Accounts, Designation = "Accounts Officer", JoiningDate = new DateTime(2022, 3, 28), Phone = "+92 333 2221102" },
            new Employee { VendorId = vendors[3].Id, EmployeeType = EmployeeType.Technical, Designation = "Field Technician", JoiningDate = new DateTime(2021, 8, 17), Phone = "+92 345 9988777" },
            new Employee { VendorId = vendors[3].Id, RetailShopId = shops[4].Id, EmployeeType = EmployeeType.Retail, Designation = "Counter Executive", JoiningDate = new DateTime(2023, 6, 1), Phone = "+92 345 9988778" }
        };
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        // The documented demo staff accounts are linked to the employees of the first vendor.
        var technicalUser = await CreateUserAsync(userManager, "technical1@nexus.com", StaffPassword, "Bilal Technical", "+92 321 1234568", "Vendor");
        employees[1].UserId = technicalUser.Id;

        var accountsUser = await CreateUserAsync(userManager, "accounts1@nexus.com", StaffPassword, "Ayesha Accounts", "+92 321 1234569", "Vendor");
        employees[2].UserId = accountsUser.Id;

        var retailUser = await CreateUserAsync(userManager, "retail1@nexus.com", StaffPassword, "Sajid Retail", "+92 321 1234571", "Vendor");
        employees[3].UserId = retailUser.Id;

        await db.SaveChangesAsync();

        // ======================================================================= 10. CUSTOMERS
        var customerSeeds = new[]
        {
            ("Ayesha Khan", "customer1@nexus.com", "+92 300 1112233", "House 12, Block 3, Gulshan-e-Iqbal", 0),
            ("Muhammad Bilal", "customer2@nexus.com", "+92 301 2223344", "Flat 4B, Sea Breeze Plaza, Clifton", 0),
            ("Sana Yousaf", "customer3@nexus.com", "+92 302 3334455", "House 7, Street 22, DHA Phase 6", 0),
            ("Usman Tariq", null, "+92 303 4445566", "House 15, Model Town", 1),
            ("Hina Aslam", null, "+92 304 5556677", "Office 9, Mall Road", 1),
            ("Fahad Mehmood", null, "+92 305 6667788", "House 2, Satellite Town", 3),
            ("Rabia Noor", null, "+92 306 7778899", "House 44, University Road", 2),
            ("Imran Shah", null, "+92 307 8889900", "House 21, Askari VI", 0)
        };

        var customers = new List<Customer>();
        foreach (var seed in customerSeeds)
        {
            var customer = new Customer
            {
                FullName = seed.Item1,
                Phone = seed.Item3,
                Email = seed.Item2,
                Address = seed.Item4,
                CityId = cities[seed.Item5].Id,
                IdProofType = "CNIC",
                IdProofNumber = "42101-" + (1000000 + seed.Item5 * 137 + customers.Count * 271)
            };

            // Only the first three customers receive a login account, the rest are walk-in customers.
            if (!string.IsNullOrEmpty(seed.Item2))
            {
                var user = await CreateUserAsync(userManager, seed.Item2, CustomerPassword, seed.Item1, seed.Item3, "Customer");
                customer.UserId = user.Id;
            }

            customers.Add(customer);
        }
        db.Customers.AddRange(customers);

        // ======================================================================= 11. ADMIN ACCOUNT
        await CreateUserAsync(userManager, "admin@nexus.com", AdminPassword, "System Administrator", "+92 111 123456", "Admin");
        db.AuditLogs.Add(new AuditLog
        {
            Action = "System.Seeded",
            EntityType = "System",
            Details = "The demo catalogue of the Nexus Service Marketing System was inserted.",
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();

        // ======================================================================= 12. SERVICE PLANS
        db.Plans.AddRange(BuildPlans(dialUp, broadband, landline, vendors, cities[0]));
        await db.SaveChangesAsync();

        // =============================================== 13. ORDERS, CONNECTIONS, BILLS, PAYMENTS
        await SeedTransactionsAsync(db, sequence, settings, customers, vendors, shops, cities, dialUp, broadband, landline, products, userManager);

        // ======================================================================= 14. PURCHASE ORDERS
        var purchaseOrders = new[]
        {
            new PurchaseOrder
            {
                PurchaseOrderNumber = "PO-2026-0001", SupplierId = suppliers[0].Id, OrderDate = new DateTime(2026, 1, 12),
                ExpectedDate = new DateTime(2026, 1, 26), Status = PurchaseOrderStatus.Received, ReceivedOn = new DateTime(2026, 1, 24),
                PaymentStatus = SupplierPaymentStatus.Paid, Notes = "Quarterly restocking of routers and modems."
            },
            new PurchaseOrder
            {
                PurchaseOrderNumber = "PO-2026-0002", SupplierId = suppliers[2].Id, OrderDate = new DateTime(2026, 2, 3),
                ExpectedDate = new DateTime(2026, 2, 18), Status = PurchaseOrderStatus.Placed,
                PaymentStatus = SupplierPaymentStatus.Unpaid, Notes = "Dial-up modems for the northern region."
            }
        };
        db.PurchaseOrders.AddRange(purchaseOrders);
        await db.SaveChangesAsync();

        purchaseOrders[0].Items.Add(new PurchaseOrderItem { ProductId = products[0].Id, Quantity = 25, UnitCost = 3900m, LineTotal = 97500m });
        purchaseOrders[0].Items.Add(new PurchaseOrderItem { ProductId = products[1].Id, Quantity = 25, UnitCost = 2600m, LineTotal = 65000m });
        purchaseOrders[1].Items.Add(new PurchaseOrderItem { ProductId = products[2].Id, Quantity = 30, UnitCost = 1550m, LineTotal = 46500m });

        purchaseOrders[0].TotalAmount = 162500m;
        purchaseOrders[0].AmountPaid = 162500m;
        purchaseOrders[0].AmountDue = 0m;
        purchaseOrders[1].TotalAmount = 46500m;
        purchaseOrders[1].AmountPaid = 0m;
        purchaseOrders[1].AmountDue = 46500m;
        await db.SaveChangesAsync();

        // ======================================================================= 15. FEEDBACK AND MESSAGES
        db.Feedback.AddRange(
            new Feedback { CustomerId = customers[0].Id, Rating = 5, Comments = "The broadband connection was installed within 48 hours and the technician was very professional.", IsApproved = true, CreatedAt = DateTime.Now.AddDays(-20) },
            new Feedback { CustomerId = customers[1].Id, Rating = 4, Comments = "Billing is clear and the monthly statement matches the usage I was charged for.", IsApproved = true, CreatedAt = DateTime.Now.AddDays(-15) },
            new Feedback { CustomerId = customers[2].Id, Rating = 5, Comments = "The call charges on my landline invoice were itemised properly, exactly as promised.", IsApproved = true, CreatedAt = DateTime.Now.AddDays(-9) },
            new Feedback { CustomerId = customers[3].Id, Rating = 3, Comments = "The installation took two days longer than the promised date.", IsApproved = false, CreatedAt = DateTime.Now.AddDays(-5) },
            new Feedback { CustomerId = customers[4].Id, Rating = 4, Comments = "Good value for the money, the security deposit was refunded on request.", IsApproved = true, CreatedAt = DateTime.Now.AddDays(-3) },
            new Feedback { CustomerId = customers[5].Id, Rating = 2, Comments = "The customer care line was busy for a long time.", IsApproved = false, CreatedAt = DateTime.Now.AddDays(-1) });

        db.ContactMessages.AddRange(
            new ContactMessage { Name = "Kamran Sheikh", Email = "kamran@example.com", Phone = "0300-5551212", Subject = "Broadband availability", Message = "Please let me know whether Broadband is available in my street.", CreatedAt = DateTime.Now.AddDays(-4) },
            new ContactMessage { Name = "Nadia Anwar", Email = "nadia@example.com", Phone = "0321-7778989", Subject = "Bill query", Message = "I would like a copy of my last invoice for my office records.", CreatedAt = DateTime.Now.AddDays(-2) });

        await db.SaveChangesAsync();

        // The marker proves that the whole demo catalogue was inserted successfully.
        db.Settings.Add(new Setting
        {
            Key = SeedMarkerKey,
            Value = "1.0",
            Group = "General",
            Description = "Marker that shows that the demo catalogue is already in the database."
        });

        await db.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------------------------
    // Helper methods
    // -----------------------------------------------------------------------------------------

    /// <summary>Creates a login account with the given role and returns the created user.</summary>
    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email,
        string password, string fullName, string phone, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            Phone = phone,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"The account {email} could not be created: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    /// <summary>Builds the complete service catalogue of the platform.</summary>
    private static List<Plan> BuildPlans(ServiceCategory dialUp, ServiceCategory broadband, ServiceCategory landline,
        Vendor[] vendors, City defaultCity)
    {
        var plans = new List<Plan>();

        // --------------------------------------------------------------------- DIAL-UP INTERNET
        plans.Add(new Plan
        {
            Name = "Dial-Up 10 Hours Pack", CategoryId = dialUp.Id, Kind = PlanKind.Hourly, Hours = 10,
            ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 500m, Icon = "bi-hourglass-split",
            Description = "Ten hours of Dial-Up internet for one month. The pack is valid for thirty days from the date of activation and the unused hours are not carried forward.",
            Features = "10 hours of Dial-Up internet,Valid for 30 days,24x7 Nexus dial-up server access,Confirmation on activation"
        });
        plans.Add(new Plan
        {
            Name = "Dial-Up 30 Hours Pack", CategoryId = dialUp.Id, Kind = PlanKind.Hourly, Hours = 30,
            ValidityDays = 90, BillingPeriod = BillingPeriod.Quarterly, Price = 1300m, Icon = "bi-hourglass-split",
            Description = "Thirty hours of Dial-Up internet for three months at a better rate than the ten hour pack.",
            Features = "30 hours of Dial-Up internet,Valid for 3 months,Lower rate per hour,Confirmation on activation"
        });
        plans.Add(new Plan
        {
            Name = "Dial-Up 60 Hours Pack", CategoryId = dialUp.Id, Kind = PlanKind.Hourly, Hours = 60,
            ValidityDays = 180, BillingPeriod = BillingPeriod.HalfYearly, Price = 2600m, Icon = "bi-hourglass-split",
            Description = "Sixty hours of Dial-Up internet for six months - the most economical hourly pack.",
            Features = "60 hours of Dial-Up internet,Valid for 6 months,Lowest rate per hour,Confirmation on activation"
        });
        plans.Add(new Plan
        {
            Name = "Dial-Up Unlimited 28 Kbps - Monthly", CategoryId = dialUp.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 28, ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 750m, Icon = "bi-speedometer2",
            Description = "Unlimited Dial-Up internet at 28 Kbps, renewed every month.",
            Features = "Unlimited usage,Up to 28 Kbps speed,Valid for 1 month,24x7 support"
        });
        plans.Add(new Plan
        {
            Name = "Dial-Up Unlimited 28 Kbps - Quarterly", CategoryId = dialUp.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 28, ValidityDays = 90, BillingPeriod = BillingPeriod.Quarterly, Price = 1500m, Icon = "bi-speedometer2",
            Description = "Unlimited Dial-Up internet at 28 Kbps for three months.",
            Features = "Unlimited usage,Up to 28 Kbps speed,Valid for 3 months,Free monthly support visits"
        });
        plans.Add(new Plan
        {
            Name = "Dial-Up Unlimited 56 Kbps - Monthly", CategoryId = dialUp.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 56, ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 1000m, Icon = "bi-speedometer",
            Description = "Unlimited Dial-Up internet at the faster 56 Kbps speed, renewed every month.",
            Features = "Unlimited usage,Up to 56 Kbps speed,Valid for 1 month,Priority customer care"
        });
        plans.Add(new Plan
        {
            Name = "Dial-Up Unlimited 56 Kbps - Quarterly", CategoryId = dialUp.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 56, ValidityDays = 90, BillingPeriod = BillingPeriod.Quarterly, Price = 1800m, Icon = "bi-speedometer",
            Description = "Unlimited Dial-Up internet at 56 Kbps for three months - the best Dial-Up value.",
            Features = "Unlimited usage,Up to 56 Kbps speed,Valid for 3 months,Priority customer care"
        });

        // ----------------------------------------------------------------------- BROADBAND
        plans.Add(new Plan
        {
            Name = "Broadband 30 Hours Pack", CategoryId = broadband.Id, Kind = PlanKind.Hourly, Hours = 30,
            ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 1750m, Icon = "bi-hourglass-split",
            Description = "Thirty hours of Broadband internet for one month, ideal for a moderate home usage.",
            Features = "30 hours of Broadband internet,Valid for 30 days,Wi-Fi router on request,Confirmation on activation"
        });
        plans.Add(new Plan
        {
            Name = "Broadband 60 Hours Pack", CategoryId = broadband.Id, Kind = PlanKind.Hourly, Hours = 60,
            ValidityDays = 180, BillingPeriod = BillingPeriod.HalfYearly, Price = 3150m, Icon = "bi-hourglass-split",
            Description = "Sixty hours of Broadband internet for six months at an attractive rate.",
            Features = "60 hours of Broadband internet,Valid for 6 months,Lower rate per hour,Free installation"
        });
        plans.Add(new Plan
        {
            Name = "Broadband Unlimited 64 Kbps - Monthly", CategoryId = broadband.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 64, ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 2250m, Icon = "bi-wifi",
            Description = "Unlimited Broadband internet at 64 Kbps, renewed every month.",
            Features = "Unlimited usage,Up to 64 Kbps speed,Valid for 1 month,Email support"
        });
        plans.Add(new Plan
        {
            Name = "Broadband Unlimited 64 Kbps - Quarterly", CategoryId = broadband.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 64, ValidityDays = 90, BillingPeriod = BillingPeriod.Quarterly, Price = 4000m, Icon = "bi-wifi",
            Description = "Unlimited Broadband internet at 64 Kbps for three months.",
            Features = "Unlimited usage,Up to 64 Kbps speed,Valid for 3 months,Free monthly support visits"
        });
        plans.Add(new Plan
        {
            Name = "Broadband Unlimited 128 Kbps - Monthly", CategoryId = broadband.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 128, ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 3500m, Icon = "bi-wifi",
            Description = "Unlimited Broadband internet at 128 Kbps for streaming and online classes.",
            Features = "Unlimited usage,Up to 128 Kbps speed,Valid for 1 month,Priority customer care"
        });
        plans.Add(new Plan
        {
            Name = "Broadband Unlimited 128 Kbps - Quarterly", CategoryId = broadband.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 128, ValidityDays = 90, BillingPeriod = BillingPeriod.Quarterly, Price = 4450m, Icon = "bi-wifi",
            Description = "Unlimited Broadband internet at 128 Kbps for three months - the premium Broadband plan.",
            Features = "Unlimited usage,Up to 128 Kbps speed,Valid for 3 months,Priority customer care,Free installation"
        });

        // -------------------------------------------------------------------------- LANDLINE
        plans.Add(new Plan
        {
            Name = "Landline Local Unlimited - Yearly", CategoryId = landline.Id, Kind = PlanKind.Rental,
            BillingPeriod = BillingPeriod.Yearly, Price = 750m, LocalRate = 5.50m, Icon = "bi-telephone",
            Description = "Yearly landline rental with unlimited local calls charged at Rs 5.50 per minute.",
            Features = "Rs 750 yearly rental,Rs 5.50 per local minute,Unlimited local calls,Digital billing"
        });
        plans.Add(new Plan
        {
            Name = "Landline Local Monthly", CategoryId = landline.Id, Kind = PlanKind.Rental,
            BillingPeriod = BillingPeriod.Monthly, Price = 350m, LocalRate = 7.50m, Icon = "bi-telephone",
            Description = "Monthly landline rental with local calls at Rs 7.50 per minute.",
            Features = "Rs 350 monthly rental,Rs 7.50 per local minute,Monthly digital bill,Easy reconnection"
        });
        plans.Add(new Plan
        {
            Name = "Landline Local 1 Hour Pack", CategoryId = landline.Id, Kind = PlanKind.Hourly, Hours = 1,
            BillingPeriod = BillingPeriod.Hourly, Price = 200m, LocalRate = 5.50m, Icon = "bi-clock",
            Description = "A single hour of landline usage for Rs 200 - the quick pay-per-use pack.",
            Features = "One hour of talk time,Rs 200 flat rate,No monthly rental,Valid immediately after purchase"
        });
        plans.Add(new Plan
        {
            Name = "Landline STD Monthly", CategoryId = landline.Id, Kind = PlanKind.Rental,
            BillingPeriod = BillingPeriod.Monthly, Price = 1250m, LocalRate = 7.00m, StdRate = 22.50m, MobileMsgRate = 10.00m,
            Icon = "bi-telephone-fill",
            Description = "Monthly landline package including local, STD and mobile messaging call charges.",
            Features = "Rs 1250 monthly rental,Rs 7.00 per local minute,Rs 22.50 per STD minute,Rs 10.00 per mobile message minute"
        });
        plans.Add(new Plan
        {
            Name = "Landline STD Half-Yearly", CategoryId = landline.Id, Kind = PlanKind.Rental,
            BillingPeriod = BillingPeriod.HalfYearly, Price = 4200m, LocalRate = 6.00m, StdRate = 20.00m, MobileMsgRate = 11.50m,
            Icon = "bi-telephone-fill",
            Description = "Six months of landline service at reduced call rates.",
            Features = "Rs 4200 for 6 months,Rs 6.00 per local minute,Rs 20.00 per STD minute,Rs 11.50 per mobile message minute"
        });
        plans.Add(new Plan
        {
            Name = "Landline STD Yearly", CategoryId = landline.Id, Kind = PlanKind.Rental,
            BillingPeriod = BillingPeriod.Yearly, Price = 7500m, LocalRate = 6.00m, StdRate = 17.50m, MobileMsgRate = 12.50m,
            Icon = "bi-telephone-fill",
            Description = "Yearly landline package with the lowest STD and mobile messaging rates of the catalogue.",
            Features = "Rs 7500 yearly rental,Rs 6.00 per local minute,Rs 17.50 per STD minute,Rs 12.50 per mobile message minute"
        });

        // --------------------------------------- plans submitted by the vendors (need approval)
        plans.Add(new Plan
        {
            Name = "SmartLink Home Broadband 256 Kbps", CategoryId = broadband.Id, Kind = PlanKind.Unlimited,
            SpeedKbps = 256, ValidityDays = 30, BillingPeriod = BillingPeriod.Monthly, Price = 3950m, Icon = "bi-wifi",
            VendorId = vendors[1].Id, ApprovalStatus = PlanApprovalStatus.Approved,
            Description = "Vendor plan: unlimited home internet at 256 Kbps with a free router installation.",
            Features = "Unlimited usage,Up to 256 Kbps speed,Free router installation,Local support desk"
        });
        plans.Add(new Plan
        {
            Name = "Metro Tele Business Landline", CategoryId = landline.Id, Kind = PlanKind.Rental,
            BillingPeriod = BillingPeriod.Monthly, Price = 1450m, LocalRate = 6.50m, StdRate = 21.00m, MobileMsgRate = 10.50m,
            Icon = "bi-briefcase", VendorId = vendors[2].Id, ApprovalStatus = PlanApprovalStatus.Approved,
            Description = "Vendor plan: landline package designed for small offices with a dedicated relationship manager.",
            Features = "Rs 1450 monthly rental,Reduced STD rates,Dedicated support manager,Priority fault repair"
        });
        plans.Add(new Plan
        {
            Name = "SkyNet Student Dial-Up Pack", CategoryId = dialUp.Id, Kind = PlanKind.Hourly, Hours = 20,
            ValidityDays = 60, BillingPeriod = BillingPeriod.Monthly, Price = 900m, Icon = "bi-mortarboard",
            VendorId = vendors[3].Id, ApprovalStatus = PlanApprovalStatus.Pending,
            Description = "Vendor plan awaiting approval: twenty hours of Dial-Up internet for students.",
            Features = "20 hours of Dial-Up internet,Valid for 60 days,Student discount,Confirmation on activation"
        });

        // The catalogue of the administrator is published straight away, only the plans that the
        // vendors submitted wait for the approval of the administrator.
        foreach (var plan in plans.Where(p => p.VendorId is null))
        {
            plan.ApprovalStatus = PlanApprovalStatus.Approved;
        }

        return plans;
    }

    /// <summary>
    /// Inserts the demo orders, connections, bills and payments. The data covers every status of the
    /// order lifecycle, all three connection states and all bulk discount tiers.
    /// </summary>
    private static async Task SeedTransactionsAsync(ApplicationDbContext db, SequenceService sequence,
        Setting[] settings, List<Customer> customers, Vendor[] vendors, RetailShop[] shops, List<City> cities,
        ServiceCategory dialUp, ServiceCategory broadband, ServiceCategory landline, Product[] products,
        UserManager<ApplicationUser> userManager)
    {
        var taxPercent = 12.24m;
        var rules = await db.DiscountRules.AsNoTracking().ToListAsync();
        var plans = await db.Plans.AsNoTracking().ToListAsync();
        var discountService = new DiscountService();

        Plan PlanByName(string name) => plans.First(p => p.Name == name);

        // -------------------------------------------------------------------------
        // Local helper that builds a complete order with its timeline and checks.
        // -------------------------------------------------------------------------
        async Task<Order> CreateOrderAsync(int customerIndex, string planName, int vendorIndex, int? shopIndex,
            int quantity, int cityIndex, string address, string phone, OrderStatus status, string? existingLandline,
            bool requiresLandline, int daysAgo, string? notes = null)
        {
            var plan = PlanByName(planName);
            var category = plan.CategoryId == dialUp.Id ? dialUp : plan.CategoryId == broadband.Id ? broadband : landline;
            var letter = category.Code;

            var serial = await sequence.NextOrderSerialAsync(letter[0]);
            var orderId = CodeGenerator.FormatOrderId(letter[0], serial);

            var grossDeposit = category.SecurityDeposit * quantity;
            var grossAdvance = plan.Price * quantity;
            var breakdown = discountService.Calculate(grossDeposit, grossAdvance, quantity, rules);

            var placedAt = DateTime.Now.AddDays(-daysAgo);

            var order = new Order
            {
                OrderId = orderId,
                TypeLetter = letter,
                CustomerId = customers[customerIndex].Id,
                VendorId = vendors[vendorIndex].Id,
                RetailShopId = shopIndex.HasValue ? shops[shopIndex.Value].Id : null,
                PlanId = plan.Id,
                Quantity = quantity,
                CityId = cities[cityIndex].Id,
                InstallationAddress = address,
                ContactNumber = phone,
                ExistingLandlineAccountId = existingLandline,
                RequiresLandline = requiresLandline,
                Status = status,
                FeasibilityStatus = status switch
                {
                    OrderStatus.Feasible or OrderStatus.ConnectionProvided => FeasibilityResult.Feasible,
                    OrderStatus.NotFeasible => FeasibilityResult.NotFeasible,
                    _ => FeasibilityResult.Pending
                },
                DiscountPercent = breakdown.DiscountPercent,
                SecurityDepositAmount = grossDeposit,
                SecurityDepositPayable = breakdown.NetSecurityDeposit,
                AdvanceAmount = grossAdvance,
                AdvancePayable = breakdown.NetAdvance,
                TaxPercent = taxPercent,
                PlacedAt = placedAt,
                Notes = notes
            };

            var taxable = breakdown.NetSecurityDeposit + breakdown.NetAdvance;
            order.TaxAmount = Math.Round(taxable * (taxPercent / 100m), 2);
            order.TotalAmount = Math.Round(taxable + order.TaxAmount, 2);

            // The status timeline of the order.
            order.StatusHistory.Add(new OrderStatusHistory
            {
                FromStatus = null, ToStatus = OrderStatus.Placed, ChangedAt = placedAt,
                ChangedByName = "Online customer", Remark = "Order received through the website."
            });

            if (status != OrderStatus.Placed)
            {
                order.StatusHistory.Add(new OrderStatusHistory
                {
                    FromStatus = OrderStatus.Placed, ToStatus = OrderStatus.FeasibilityPending,
                    ChangedAt = placedAt.AddHours(6), ChangedByName = "Vendor Desk",
                    Remark = "Order forwarded to the technical department."
                });
            }

            // The feasibility checks of the order.
            var checkTypes = new List<FeasibilityCheckType>();

            if (letter == "D")
            {
                checkTypes.Add(requiresLandline ? FeasibilityCheckType.Landline : FeasibilityCheckType.Internet);
                if (requiresLandline)
                {
                    checkTypes.Add(FeasibilityCheckType.Internet);
                }
            }
            else if (letter == "T")
            {
                checkTypes.Add(FeasibilityCheckType.Landline);
            }
            else
            {
                checkTypes.Add(FeasibilityCheckType.Internet);
            }

            foreach (var checkType in checkTypes)
            {
                var result = status switch
                {
                    OrderStatus.NotFeasible => FeasibilityResult.NotFeasible,
                    OrderStatus.Feasible or OrderStatus.ConnectionProvided => FeasibilityResult.Feasible,
                    _ => FeasibilityResult.Pending
                };

                order.FeasibilityChecks.Add(new OrderFeasibilityCheck
                {
                    CheckType = checkType,
                    Result = result,
                    DistanceParameters = "Exchange distance 2.4 km, cable length inside the building 35 metres.",
                    ServerAvailability = checkType == FeasibilityCheckType.Internet
                        ? "Nexus DSLAM port 14 is free and the dial-up / broadband server is reachable."
                        : "Nexus telephone exchange 07 serves the street and has four free pairs.",
                    CompanyRules = "Address proof supplied, no pending dues on the existing account.",
                    Remarks = result == FeasibilityResult.NotFeasible
                        ? "The distance to the nearest exchange is outside the serviceable radius."
                        : result == FeasibilityResult.Pending
                            ? "Awaiting the technical inspection."
                            : "All parameters are within the serviceable range.",
                    CheckedByName = result == FeasibilityResult.Pending ? null : "Bilal Technical",
                    CheckedAt = result == FeasibilityResult.Pending ? null : placedAt.AddHours(30)
                });
            }

            if (status == OrderStatus.Feasible || status == OrderStatus.ConnectionProvided)
            {
                order.StatusHistory.Add(new OrderStatusHistory
                {
                    FromStatus = OrderStatus.FeasibilityPending, ToStatus = OrderStatus.Feasible,
                    ChangedAt = placedAt.AddHours(30), ChangedByName = "Bilal Technical",
                    Remark = "Feasibility confirmed for every required check."
                });
            }

            if (status == OrderStatus.NotFeasible)
            {
                order.StatusHistory.Add(new OrderStatusHistory
                {
                    FromStatus = OrderStatus.FeasibilityPending, ToStatus = OrderStatus.NotFeasible,
                    ChangedAt = placedAt.AddHours(30), ChangedByName = "Bilal Technical",
                    Remark = "The address is outside the serviceable area of the exchange."
                });
            }

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            if (status == OrderStatus.ConnectionProvided)
            {
                order.StatusHistory.Add(new OrderStatusHistory
                {
                    FromStatus = OrderStatus.Feasible, ToStatus = OrderStatus.ConnectionProvided,
                    ChangedAt = placedAt.AddDays(2), ChangedByName = "Bilal Technical",
                    Remark = "Connection activated and the equipment handed over."
                });
                order.CompletedAt = placedAt.AddDays(2);
                await db.SaveChangesAsync();
            }

            return order;
        }

        // --------------------------------------------------------------- 1. broadband order
        var order1 = await CreateOrderAsync(0, "Broadband Unlimited 64 Kbps - Monthly", 0, 0, 1, 0,
            "House 12, Block 3, Gulshan-e-Iqbal, Karachi", "+92 300 1112233", OrderStatus.ConnectionProvided, null, false, 95);

        // --------------------------------------------------------------- 2. landline order
        var order2 = await CreateOrderAsync(1, "Landline STD Monthly", 0, 0, 1, 0,
            "Flat 4B, Sea Breeze Plaza, Clifton, Karachi", "+92 301 2223344", OrderStatus.ConnectionProvided, null, false, 80);

        // --------------------------------------------------------------- 3. dial-up without landline
        var order3 = await CreateOrderAsync(2, "Dial-Up 30 Hours Pack", 0, 0, 1, 0,
            "House 7, Street 22, DHA Phase 6, Karachi", "+92 302 3334455", OrderStatus.ConnectionProvided, null, true, 120);

        // --------------------------------------------------------------- 4. bulk order - 12 connections (25% discount)
        var order4 = await CreateOrderAsync(3, "Broadband Unlimited 128 Kbps - Monthly", 1, 2, 12, 1,
            "Head office, Mall Road, Lahore", "+92 303 4445566", OrderStatus.ConnectionProvided, null, false, 110,
            "Corporate bulk order for a software house.");

        // --------------------------------------------------------------- 5. bulk order - 20 connections (50% discount)
        var order5 = await CreateOrderAsync(4, "Broadband Unlimited 128 Kbps - Quarterly", 1, 2, 20, 1,
            "Campus, University Road, Islamabad", "+92 304 5556677", OrderStatus.ConnectionProvided, null, false, 100,
            "Bulk order for a university campus.");

        // --------------------------------------------------------------- 6. bulk order - 30 connections (75% discount)
        var order6 = await CreateOrderAsync(5, "Broadband Unlimited 64 Kbps - Quarterly", 2, 3, 30, 2,
            "Industrial Area, Islamabad", "+92 305 6667788", OrderStatus.ConnectionProvided, null, false, 90,
            "Bulk order for an industrial estate.");

        // --------------------------------------------------------------- 7. bulk order - 60 connections (100% discount)
        var order7 = await CreateOrderAsync(6, "Broadband Unlimited 128 Kbps - Quarterly", 2, 3, 60, 2,
            "Warehouse, Satellite Town, Rawalpindi", "+92 306 7778899", OrderStatus.ConnectionProvided, null, false, 80,
            "Bulk order for a warehouse network.");

        // --------------------------------------------------------------- 8. dial-up with an existing landline
        var order8 = await CreateOrderAsync(7, "Dial-Up Unlimited 56 Kbps - Monthly", 0, 0, 1, 0,
            "House 21, Askari VI, Karachi", "+92 307 8889900", OrderStatus.ConnectionProvided, null, false, 70);

        // --------------------------------------------------------------- 9. orders still waiting
        await CreateOrderAsync(0, "Dial-Up 60 Hours Pack", 0, 0, 1, 0,
            "House 12, Block 3, Gulshan-e-Iqbal, Karachi", "+92 300 1112233", OrderStatus.FeasibilityPending, null, true, 5);

        await CreateOrderAsync(1, "Broadband Unlimited 128 Kbps - Monthly", 0, 1, 1, 0,
            "Flat 4B, Sea Breeze Plaza, Clifton, Karachi", "+92 301 2223344", OrderStatus.FeasibilityPending, null, false, 4);

        await CreateOrderAsync(2, "Landline Local Monthly", 0, 0, 1, 0,
            "House 7, Street 22, DHA Phase 6, Karachi", "+92 302 3334455", OrderStatus.Placed, null, false, 2);

        await CreateOrderAsync(3, "Landline STD Yearly", 1, 2, 1, 1,
            "Head office, Mall Road, Lahore", "+92 303 4445566", OrderStatus.Placed, null, false, 1);

        // --------------------------------------------------------------- 10. not feasible and cancelled orders
        await CreateOrderAsync(4, "Broadband Unlimited 128 Kbps - Monthly", 1, 2, 1, 3,
            "Village outside the exchange area, Rawalpindi", "+92 304 5556677", OrderStatus.NotFeasible, null, false, 9,
            "The nearest Nexus exchange is 19 kilometres away.");

        var cancelled = await CreateOrderAsync(5, "Landline Local 1 Hour Pack", 0, 0, 1, 0,
            "Office 9, Mall Road, Lahore", "+92 305 6667788", OrderStatus.Cancelled, null, false, 7);
        cancelled.CancelReason = "The customer changed the plan before the feasibility visit.";

        // --------------------------------------------------------------- 11. a walk-in order of the retail counter
        var order11 = await CreateOrderAsync(6, "Landline Local Unlimited - Yearly", 3, 4, 1, 3,
            "Shop 3, Rania Plaza, Rawalpindi", "+92 306 7778899", OrderStatus.ConnectionProvided, null, false, 40);

        // --------------------------------------------------------------- 12. feasible - waiting for the installation team
        await CreateOrderAsync(4, "Broadband Unlimited 64 Kbps - Monthly", 2, 3, 1, 2,
            "Plot 22, F-10 Markaz, Islamabad", "+92 304 5556677", OrderStatus.Feasible, null, false, 6,
            "The feasibility visit passed, the installation team is being scheduled.");

        // --------------------------------------------------------------- 13. feasible dial-up - both checks passed
        await CreateOrderAsync(5, "Dial-Up 10 Hours Pack", 0, 0, 1, 0,
            "Flat 2, Sea Breeze Plaza, Clifton, Karachi", "+92 305 6667788", OrderStatus.Feasible, null, true, 3,
            "The landline and the internet feasibility checks both passed.");

        // --------------------------------------------------------------- 14. feasible landline order of the second vendor
        await CreateOrderAsync(2, "Landline STD Half-Yearly", 1, 2, 1, 1,
            "54-B, Blue Area, Lahore", "+92 302 3334455", OrderStatus.Feasible, null, false, 2,
            "The exchange can serve the address with two free pairs.");

        // --------------------------------------------------------------- 15. a fresh order of the retail counter
        await CreateOrderAsync(7, "Landline Local Monthly", 3, 4, 1, 3,
            "House 5, Satellite Town, Rawalpindi", "+92 307 8889900", OrderStatus.Placed, null, false, 1,
            "Order placed at the counter of SkyNet - Rania Plaza.");

        // --------------------------------------------------------------- 16. broadband order waiting for the survey
        await CreateOrderAsync(1, "Broadband 30 Hours Pack", 3, 4, 1, 3,
            "Office 11, Rania Plaza, Rawalpindi", "+92 301 2223344", OrderStatus.FeasibilityPending, null, false, 8);

        // --------------------------------------------------------------- 17. a dial-up order that needs a new landline
        await CreateOrderAsync(3, "Dial-Up Unlimited 28 Kbps - Quarterly", 1, 2, 1, 1,
            "House 44, Model Town, Lahore", "+92 303 4445566", OrderStatus.FeasibilityPending, null, true, 6);

        await db.SaveChangesAsync();

        // =========================================================================================
        // CONNECTIONS, BILLS AND PAYMENTS
        // =========================================================================================

        async Task<Connection> CreateConnectionAsync(Order order, ConnectionStatus status, string? statusReason,
            int equipmentProductIndex, decimal monthlyCharge)
        {
            var city = await db.Cities.AsNoTracking().FirstAsync(c => c.Id == order.CityId);
            var product = products[equipmentProductIndex];

            var serial = await sequence.NextAccountSerialAsync(order.TypeLetter[0]);
            var accountId = CodeGenerator.FormatAccountId(order.TypeLetter[0], city.Code3, serial);

            var connection = new Connection
            {
                AccountId = accountId,
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                VendorId = order.VendorId,
                PlanId = order.PlanId,
                CityId = order.CityId,
                Status = status,
                StatusReason = statusReason,
                StatusChangedAt = status == ConnectionStatus.Active ? null : DateTime.Now.AddDays(-3),
                StatusChangedByUserId = status == ConnectionStatus.Active ? null : "technical1@nexus.com",
                ActivationDate = order.PlacedAt.AddDays(2),
                MonthlyCharge = monthlyCharge,
                SecurityDeposit = order.SecurityDepositPayable,
                InstallationAddress = order.InstallationAddress,
                ContactNumber = order.ContactNumber
            };

            db.Connections.Add(connection);

            // The equipment is issued from the stock and the stock is reduced.
            product.StockQuantity -= 1;
            connection.Equipment.Add(new ConnectionEquipment
            {
                ProductId = product.Id,
                SerialNumber = "NX-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant(),
                AssignedAt = DateTime.Now.AddDays(-10),
                AssignedByUserId = "technical1@nexus.com"
            });

            await db.SaveChangesAsync();
            return connection;
        }

        async Task<Bill> CreateBillAsync(Connection connection, BillType billType, decimal planCharge,
            decimal localCharge, decimal stdCharge, decimal mobileCharge, decimal previousDue, bool paid,
            decimal? replacementCharge = null, string? remarks = null)
        {
            var billSerial = await sequence.NextSerialAsync("BILL");
            var billNumber = $"INV-{billSerial.ToString("D6")}";

            var gross = planCharge + localCharge + stdCharge + mobileCharge + (replacementCharge ?? 0m) + previousDue;
            var tax = Math.Round(gross * (taxPercent / 100m), 2);
            var total = Math.Round(gross + tax, 2);

            // The billing is postpaid. The activation invoice is raised on the day the service starts
            // and is due at the end of the first period, the periodic invoice covers the period that
            // follows and is raised once that period has ended.
            var activation = connection.ActivationDate;
            var periodStart = billType == BillType.Activation ? activation : activation.AddDays(30);
            var periodEnd = periodStart.AddDays(30);
            var generatedOn = billType == BillType.Activation ? activation : periodEnd;

            var bill = new Bill
            {
                BillNumber = billNumber,
                ConnectionId = connection.Id,
                OrderId = connection.OrderId,
                BillType = billType,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                PlanCharge = planCharge,
                LocalChargeAmount = localCharge,
                StdChargeAmount = stdCharge,
                MobileChargeAmount = mobileCharge,
                ReplacementChargeAmount = replacementCharge ?? 0m,
                PreviousDue = previousDue,
                TaxPercent = taxPercent,
                TaxAmount = tax,
                SubTotal = Math.Round(gross, 2),
                TotalAmount = total,
                GeneratedByUserId = "accounts1@nexus.com",
                GeneratedByName = "Ayesha Accounts",
                GeneratedOn = generatedOn,
                DueDate = periodEnd,
                Remarks = remarks ?? "Payable at the Nexus counter (cash on delivery)."
            };

            bill.Lines.Add(new BillLine { Description = "Plan charge", LineType = "Plan", Quantity = 1m, UnitPrice = planCharge, Amount = planCharge, SortOrder = 1 });
            if (localCharge > 0m) bill.Lines.Add(new BillLine { Description = "Local calls", LineType = "Local", Amount = localCharge, SortOrder = 2 });
            if (stdCharge > 0m) bill.Lines.Add(new BillLine { Description = "STD calls", LineType = "STD", Amount = stdCharge, SortOrder = 3 });
            if (mobileCharge > 0m) bill.Lines.Add(new BillLine { Description = "Mobile messaging", LineType = "Mobile", Amount = mobileCharge, SortOrder = 4 });
            if (replacementCharge > 0m) bill.Lines.Add(new BillLine { Description = "Equipment replacement charge", LineType = "Replacement", Amount = replacementCharge.Value, SortOrder = 5 });
            if (previousDue > 0m) bill.Lines.Add(new BillLine { Description = "Previous unpaid balance", LineType = "Previous Due", Amount = previousDue, SortOrder = 6 });

            if (paid)
            {
                bill.AmountPaid = total;
                bill.AmountDue = 0m;
                bill.Status = BillStatus.Paid;
            }
            else
            {
                bill.AmountPaid = 0m;
                bill.AmountDue = total;
                bill.Status = BillStatus.Due;
            }

            db.Bills.Add(bill);
            await db.SaveChangesAsync();

            if (paid)
            {
                // The receipt is attached to the bill through the navigation property so that the
                // foreign key is resolved by Entity Framework while it saves the graph.
                var receiptSerial = await sequence.NextSerialAsync("RECEIPT");
                bill.Payments.Add(new Payment
                {
                    ConnectionId = connection.Id,
                    ReceiptNumber = $"RCP-{receiptSerial.ToString("D6")}",
                    Amount = total,
                    PaidOn = bill.GeneratedOn.AddDays(2) > DateTime.Now ? DateTime.Now : bill.GeneratedOn.AddDays(2),
                    ReceivedByUserId = "retail1@nexus.com",
                    ReceivedByName = "Sajid Retail",
                    Remarks = "Cash received at the counter."
                });

                await db.SaveChangesAsync();
            }

            return bill;
        }

        // Connection 1 - broadband, active, bill paid.
        var connection1 = await CreateConnectionAsync(order1, ConnectionStatus.Active, null, 0, 2250m);
        await CreateBillAsync(connection1, BillType.Activation, 2250m, 0m, 0m, 0m, 0m, true);
        await CreateBillAsync(connection1, BillType.Periodic, 2250m, 0m, 0m, 0m, 0m, true,
            remarks: "Monthly broadband bill - payable at the Nexus counter.");

        // Connection 2 - landline, active, monthly bill still due.
        var connection2 = await CreateConnectionAsync(order2, ConnectionStatus.Active, null, 9, 1250m);
        await CreateBillAsync(connection2, BillType.Activation, 1250m, 0m, 0m, 0m, 0m, true);
        await CreateBillAsync(connection2, BillType.Periodic, 1250m, 180m, 240m, 60m, 0m, false,
            remarks: "Monthly landline bill including local, STD and mobile messaging usage.");

        // The Dial-Up order number 8 was placed by a customer that already owns an active Nexus
        // landline, therefore only the internet feasibility check was required.
        order8.ExistingLandlineAccountId = connection2.AccountId;
        await db.SaveChangesAsync();

        // Connection 3 - dial-up with a landline, temporarily inactive because the bill is overdue.
        var connection3 = await CreateConnectionAsync(order3, ConnectionStatus.TemporarilyInactive,
            "The monthly bill stayed unpaid beyond the allowed period, the service was temporarily stopped.", 2, 1300m);
        await CreateBillAsync(connection3, BillType.Activation, 1300m, 0m, 0m, 0m, 0m, true);
        await CreateBillAsync(connection3, BillType.Periodic, 1300m, 0m, 0m, 0m, 0m, false,
            remarks: "Unpaid bill - the connection has been temporarily inactive.");

        // Connection 4 - permanently inactive, the security deposit was refunded.
        var connection4 = await CreateConnectionAsync(order4, ConnectionStatus.PermanentlyInactive,
            "The corporate customer moved its office and closed the connections permanently.", 0, 3500m);
        await CreateBillAsync(connection4, BillType.Activation, 3500m, 0m, 0m, 0m, 0m, true);
        db.SecurityDepositEntries.Add(new SecurityDepositEntry
        {
            ConnectionId = connection4.Id,
            OrderId = order4.Id,
            Amount = 5000m,
            DiscountAmount = 1250m,
            NetAmount = 3750m,
            Status = SecurityDepositStatus.Refunded,
            HeldOn = order4.PlacedAt,
            RefundedOn = DateTime.Now.AddDays(-6),
            RefundedByUserId = "vendor1@nexus.com",
            Remarks = "Security deposit refunded after the permanent closure of the connection."
        });

        // Connections 5 to 8 - the remaining bulk orders.
        var connection5 = await CreateConnectionAsync(order5, ConnectionStatus.Active, null, 3, 4450m);
        await CreateBillAsync(connection5, BillType.Activation, 4450m, 0m, 0m, 0m, 0m, true);

        var connection6 = await CreateConnectionAsync(order6, ConnectionStatus.Active, null, 1, 4000m);
        await CreateBillAsync(connection6, BillType.Activation, 4000m, 0m, 0m, 0m, 0m, true);

        var connection7 = await CreateConnectionAsync(order7, ConnectionStatus.Active, null, 1, 4450m);
        await CreateBillAsync(connection7, BillType.Activation, 4450m, 0m, 0m, 0m, 0m, true);

        var connection8 = await CreateConnectionAsync(order8, ConnectionStatus.Active, null, 2, 1000m);
        await CreateBillAsync(connection8, BillType.Activation, 1000m, 0m, 0m, 0m, 0m, true);

        // Connection 9 - the walk-in order of the retail counter.
        var connection9 = await CreateConnectionAsync(order11, ConnectionStatus.Active, null, 9, 750m);
        await CreateBillAsync(connection9, BillType.Activation, 750m, 0m, 0m, 0m, 0m, false);

        // Security deposit ledger entries of the active connections.
        db.SecurityDepositEntries.AddRange(
            new SecurityDepositEntry
            {
                ConnectionId = connection1.Id, OrderId = order1.Id, Amount = 5000m, DiscountAmount = 0m, NetAmount = 5000m,
                Status = SecurityDepositStatus.Held, HeldOn = order1.PlacedAt, Remarks = "Refundable deposit of the broadband connection."
            },
            new SecurityDepositEntry
            {
                ConnectionId = connection2.Id, OrderId = order2.Id, Amount = 2500m, DiscountAmount = 0m, NetAmount = 2500m,
                Status = SecurityDepositStatus.Held, HeldOn = order2.PlacedAt, Remarks = "Refundable deposit of the landline connection."
            },
            new SecurityDepositEntry
            {
                ConnectionId = connection3.Id, OrderId = order3.Id, Amount = 3250m, DiscountAmount = 0m, NetAmount = 3250m,
                Status = SecurityDepositStatus.Held, HeldOn = order3.PlacedAt, Remarks = "Refundable deposit of the dial-up connection."
            });

        await db.SaveChangesAsync();
    }
}
