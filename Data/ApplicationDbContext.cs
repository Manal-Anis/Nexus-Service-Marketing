using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Data;

/// <summary>
/// Entity Framework Core database context for the Nexus Service Marketing System.
/// The model is built with the Code First approach and every change is tracked through
/// the migrations stored in the Data/Migrations folder.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>Creates the context using the options supplied by dependency injection.</summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ------------------------------ Master data ----------------------------------------------
    /// <summary>Cities with their unique three digit Nexus codes.</summary>
    public DbSet<City> Cities => Set<City>();
    /// <summary>Partner sellers / retail outlets of Nexus.</summary>
    public DbSet<Vendor> Vendors => Set<Vendor>();
    /// <summary>Retail counters belonging to the vendors.</summary>
    public DbSet<RetailShop> RetailShops => Set<RetailShop>();
    /// <summary>Employees created and maintained by the administrator.</summary>
    public DbSet<Employee> Employees => Set<Employee>();
    /// <summary>Customer master records.</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Equipment suppliers.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    /// <summary>Equipment / product master with stock information.</summary>
    public DbSet<Product> Products => Set<Product>();
    /// <summary>Service categories with their security deposits.</summary>
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    /// <summary>Service plans offered on the platform.</summary>
    public DbSet<Plan> Plans => Set<Plan>();
    /// <summary>Bulk discount tiers.</summary>
    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();
    /// <summary>Application settings (tax, currency, overdue days).</summary>
    public DbSet<Setting> Settings => Set<Setting>();
    /// <summary>Serial counters used to build Order and Account identifiers.</summary>
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();

    // ------------------------------ Transactions ---------------------------------------------
    /// <summary>Order header.</summary>
    public DbSet<Order> Orders => Set<Order>();
    /// <summary>Order status timeline.</summary>
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    /// <summary>Individual feasibility checks.</summary>
    public DbSet<OrderFeasibilityCheck> OrderFeasibilityChecks => Set<OrderFeasibilityCheck>();
    /// <summary>Documents uploaded by the customers.</summary>
    public DbSet<OrderDocument> OrderDocuments => Set<OrderDocument>();
    /// <summary>Provisioned services.</summary>
    public DbSet<Connection> Connections => Set<Connection>();
    /// <summary>Equipment issued against connections.</summary>
    public DbSet<ConnectionEquipment> ConnectionEquipment => Set<ConnectionEquipment>();
    /// <summary>Plan change request history.</summary>
    public DbSet<PlanChangeHistory> PlanChangeHistory => Set<PlanChangeHistory>();

    // ------------------------------ Billing ----------------------------------------------------
    /// <summary>Bills raised against the connections.</summary>
    public DbSet<Bill> Bills => Set<Bill>();
    /// <summary>Charge lines of the bills.</summary>
    public DbSet<BillLine> BillLines => Set<BillLine>();
    /// <summary>Cash receipts recorded by the vendors.</summary>
    public DbSet<Payment> Payments => Set<Payment>();
    /// <summary>Security deposit ledger.</summary>
    public DbSet<SecurityDepositEntry> SecurityDepositEntries => Set<SecurityDepositEntry>();

    // ------------------------------ Procurement and support ---------------------------------
    /// <summary>Purchase orders raised towards the suppliers.</summary>
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    /// <summary>Lines of the purchase orders.</summary>
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    /// <summary>Customer feedback.</summary>
    public DbSet<Feedback> Feedback => Set<Feedback>();
    /// <summary>Messages received through the contact form.</summary>
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    /// <summary>Audit trail of the sensitive actions.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Builds the entity model, the indexes and the relationship constraints.</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // The Identity tables keep their default names (AspNetUsers, AspNetRoles, ...).
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Every enum is stored as a readable string so the database stays self explanatory.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType.IsEnum)
                {
                    // The generic converter is created through reflection so every enum can be
                    // converted without naming each one of them.
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                    property.SetValueConverter((ValueConverter?)Activator.CreateInstance(converterType));
                }
            }
        }
    }
}
