using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Data.Configurations;

/// <summary>Mapping rules for the master data tables (city, vendor, shop, employee, customer...).</summary>
public class MasterDataConfiguration : IEntityTypeConfiguration<City>
{
    /// <summary>City table mapping: the three digit city code must stay unique.</summary>
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Code3).IsRequired().HasMaxLength(3);

        // The city code is embedded in every Account ID, therefore it can never repeat.
        builder.HasIndex(c => c.Code3).IsUnique().HasDatabaseName("IX_Cities_Code3");
        builder.HasIndex(c => c.Name);
    }
}

/// <summary>Mapping rules for the vendor table.</summary>
public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    /// <summary>Vendor table mapping including the optional owner login.</summary>
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.BusinessName).IsRequired().HasMaxLength(150);
        builder.Property(v => v.OwnerName).HasMaxLength(120);
        builder.Property(v => v.Phone).HasMaxLength(25);
        builder.Property(v => v.Email).HasMaxLength(150);
        builder.Property(v => v.Address).HasMaxLength(400);
        builder.Property(v => v.StatusRemarks).HasMaxLength(500);

        // The owner login is optional until the administrator approves the vendor.
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.City)
            .WithMany()
            .HasForeignKey(v => v.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.BusinessName);
        builder.HasIndex(v => v.Status);
    }
}

/// <summary>Mapping rules for the retail shop table.</summary>
public class RetailShopConfiguration : IEntityTypeConfiguration<RetailShop>
{
    /// <summary>Retail shop table mapping.</summary>
    public void Configure(EntityTypeBuilder<RetailShop> builder)
    {
        builder.ToTable("RetailShops");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(150);
        builder.Property(r => r.ContactPerson).HasMaxLength(120);
        builder.Property(r => r.Phone).HasMaxLength(25);
        builder.Property(r => r.Address).HasMaxLength(400);

        // The delete behaviour is restricted (instead of cascade) so that SQL Server does not
        // end up with several cascade paths between the vendors and the shops.
        builder.HasOne(r => r.Vendor)
            .WithMany(v => v.RetailShops)
            .HasForeignKey(r => r.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.City)
            .WithMany()
            .HasForeignKey(r => r.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the employee table.</summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <summary>Employee table mapping.</summary>
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Designation).HasMaxLength(120);
        builder.Property(e => e.Phone).HasMaxLength(25);
        builder.Property(e => e.NationalId).HasMaxLength(40);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restricted delete behaviour - the records are soft deleted, therefore the foreign keys
        // must not build a cycle between the vendors, the shops and the employees.
        builder.HasOne(e => e.Vendor)
            .WithMany(v => v.Employees)
            .HasForeignKey(e => e.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RetailShop)
            .WithMany(r => r.Employees)
            .HasForeignKey(e => e.RetailShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.EmployeeType);
    }
}

/// <summary>Mapping rules for the customer table.</summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <summary>Customer table mapping.</summary>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FullName).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(25);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Address).HasMaxLength(400);
        builder.Property(c => c.IdProofType).HasMaxLength(60);
        builder.Property(c => c.IdProofNumber).HasMaxLength(60);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.City)
            .WithMany(city => city.Customers)
            .HasForeignKey(c => c.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Searching by phone number and by name is one of the required search filters.
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.FullName);
    }
}

/// <summary>Mapping rules for the supplier table.</summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    /// <summary>Supplier table mapping.</summary>
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.ContactName).HasMaxLength(120);
        builder.Property(s => s.Phone).HasMaxLength(25);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Address).HasMaxLength(400);

        builder.HasOne(s => s.City)
            .WithMany()
            .HasForeignKey(s => s.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.Name);
    }
}

/// <summary>Mapping rules for the product / equipment master.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Product table mapping.</summary>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Type).HasMaxLength(80);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Icon).HasMaxLength(80);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ReplacementCharge).HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Name);
    }
}

/// <summary>Mapping rules for the service category table.</summary>
public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    /// <summary>Service category table mapping.</summary>
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(1);
        builder.Property(c => c.Icon).HasMaxLength(80);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.SecurityDeposit).HasColumnType("decimal(18,2)");

        // The category code is the first character of every Order and Account ID.
        builder.HasIndex(c => c.Code).IsUnique().HasDatabaseName("IX_ServiceCategories_Code");
    }
}

/// <summary>Mapping rules for the plan table.</summary>
public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    /// <summary>Plan table mapping including the vendor approval workflow.</summary>
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Features).HasMaxLength(2000);
        builder.Property(p => p.Icon).HasMaxLength(80);
        builder.Property(p => p.ApprovalRemarks).HasMaxLength(500);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.LocalRate).HasColumnType("decimal(18,2)");
        builder.Property(p => p.StdRate).HasColumnType("decimal(18,2)");
        builder.Property(p => p.MobileMsgRate).HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Plans)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // A null VendorId marks an official Nexus plan.
        builder.HasOne(p => p.Vendor)
            .WithMany(v => v.Plans)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        // The storefront filters on category, price and approval state.
        builder.HasIndex(p => new { p.CategoryId, p.ApprovalStatus, p.IsActive });
        builder.HasIndex(p => p.Price);
    }
}

/// <summary>Mapping rules for the discount rule table.</summary>
public class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    /// <summary>Discount rule table mapping.</summary>
    public void Configure(EntityTypeBuilder<DiscountRule> builder)
    {
        builder.ToTable("DiscountRules");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Description).HasMaxLength(300);
        builder.Property(d => d.DiscountPercent).HasColumnType("decimal(5,2)");
        builder.HasIndex(d => d.MinConnections);
    }
}

/// <summary>Mapping rules for the settings table.</summary>
public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    /// <summary>Setting table mapping.</summary>
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(400);
        builder.Property(s => s.Description).HasMaxLength(400);
        builder.Property(s => s.Group).HasMaxLength(60);

        builder.HasIndex(s => s.Key).IsUnique().HasDatabaseName("IX_Settings_Key");
    }
}

/// <summary>Mapping rules for the sequence counter table.</summary>
public class SequenceCounterConfiguration : IEntityTypeConfiguration<SequenceCounter>
{
    /// <summary>Sequence counter table mapping.</summary>
    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.ToTable("SequenceCounters");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(40);
        builder.Property(s => s.Prefix).HasMaxLength(4);

        // The counter name is looked up row by row while holding a lock.
        builder.HasIndex(s => s.Name).IsUnique().HasDatabaseName("IX_SequenceCounters_Name");
    }
}

/// <summary>Mapping rules for the extended application user.</summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <summary>Adds the Nexus specific columns on top of the Identity user.</summary>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Phone).HasMaxLength(25);
        builder.Property(u => u.Address).HasMaxLength(400);
        builder.Property(u => u.ProfileImagePath).HasMaxLength(300);

        builder.HasIndex(u => u.Phone);
    }
}
