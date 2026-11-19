using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Data.Configurations;

/// <summary>Mapping rules for the order header table.</summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>Order table mapping including the unique 11 character order number.</summary>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderId).IsRequired().HasMaxLength(11);
        builder.Property(o => o.TypeLetter).IsRequired().HasMaxLength(1);
        builder.Property(o => o.InstallationAddress).HasMaxLength(400);
        builder.Property(o => o.ContactNumber).HasMaxLength(25);
        builder.Property(o => o.ExistingLandlineAccountId).HasMaxLength(16);
        builder.Property(o => o.FeasibilityRemarks).HasMaxLength(2000);
        builder.Property(o => o.CancelReason).HasMaxLength(500);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(60);
        builder.Property(o => o.PlacedByUserId).HasMaxLength(450);

        foreach (var money in new[]
                 {
                     builder.Property(o => o.SecurityDepositAmount),
                     builder.Property(o => o.SecurityDepositPayable),
                     builder.Property(o => o.AdvanceAmount),
                     builder.Property(o => o.AdvancePayable),
                     builder.Property(o => o.EquipmentAmount),
                     builder.Property(o => o.TaxAmount),
                     builder.Property(o => o.TotalAmount)
                 })
        {
            money.HasColumnType("decimal(18,2)");
        }
        builder.Property(o => o.DiscountPercent).HasColumnType("decimal(5,2)");
        builder.Property(o => o.TaxPercent).HasColumnType("decimal(5,2)");

        // The public order number can never repeat.
        builder.HasIndex(o => o.OrderId).IsUnique().HasDatabaseName("IX_Orders_OrderId");
        // Supports the advanced search by status, customer and date.
        builder.HasIndex(o => new { o.Status, o.PlacedAt });
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.VendorId);
        builder.HasIndex(o => o.TypeLetter);

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Vendor)
            .WithMany(v => v.Orders)
            .HasForeignKey(o => o.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.RetailShop)
            .WithMany()
            .HasForeignKey(o => o.RetailShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Plan)
            .WithMany()
            .HasForeignKey(o => o.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.City)
            .WithMany()
            .HasForeignKey(o => o.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.EquipmentProduct)
            .WithMany()
            .HasForeignKey(o => o.EquipmentProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the order status history table.</summary>
public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    /// <summary>Status history table mapping.</summary>
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ChangedByUserId).HasMaxLength(450);
        builder.Property(h => h.ChangedByName).HasMaxLength(150);
        builder.Property(h => h.Remark).HasMaxLength(1000);

        builder.HasOne(h => h.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.OrderId, h.ChangedAt });
    }
}

/// <summary>Mapping rules for the feasibility check table.</summary>
public class OrderFeasibilityCheckConfiguration : IEntityTypeConfiguration<OrderFeasibilityCheck>
{
    /// <summary>Feasibility check table mapping.</summary>
    public void Configure(EntityTypeBuilder<OrderFeasibilityCheck> builder)
    {
        builder.ToTable("OrderFeasibilityChecks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.DistanceParameters).HasMaxLength(1000);
        builder.Property(c => c.ServerAvailability).HasMaxLength(1000);
        builder.Property(c => c.CompanyRules).HasMaxLength(1000);
        builder.Property(c => c.Remarks).HasMaxLength(2000);
        builder.Property(c => c.CheckedByUserId).HasMaxLength(450);
        builder.Property(c => c.CheckedByName).HasMaxLength(150);

        builder.HasOne(c => c.Order)
            .WithMany(o => o.FeasibilityChecks)
            .HasForeignKey(c => c.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Mapping rules for the order document table.</summary>
public class OrderDocumentConfiguration : IEntityTypeConfiguration<OrderDocument>
{
    /// <summary>Order document table mapping.</summary>
    public void Configure(EntityTypeBuilder<OrderDocument> builder)
    {
        builder.ToTable("OrderDocuments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentType).IsRequired().HasMaxLength(60);
        builder.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(300);
        builder.Property(d => d.StoredFileName).IsRequired().HasMaxLength(300);
        builder.Property(d => d.RelativePath).IsRequired().HasMaxLength(500);
        builder.Property(d => d.ContentType).HasMaxLength(120);
        builder.Property(d => d.UploadedByUserId).HasMaxLength(450);

        builder.HasOne(d => d.Order)
            .WithMany(o => o.Documents)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the connection table.</summary>
public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    /// <summary>Connection table mapping including the unique 16 character account number.</summary>
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ToTable("Connections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AccountId).IsRequired().HasMaxLength(16);
        builder.Property(c => c.StatusReason).HasMaxLength(1000);
        builder.Property(c => c.StatusChangedByUserId).HasMaxLength(450);
        builder.Property(c => c.InstallationAddress).HasMaxLength(400);
        builder.Property(c => c.ContactNumber).HasMaxLength(25);
        builder.Property(c => c.MonthlyCharge).HasColumnType("decimal(18,2)");
        builder.Property(c => c.SecurityDeposit).HasColumnType("decimal(18,2)");

        // The public account number can never repeat.
        builder.HasIndex(c => c.AccountId).IsUnique().HasDatabaseName("IX_Connections_AccountId");
        builder.HasIndex(c => new { c.Status, c.CustomerId });
        builder.HasIndex(c => c.VendorId);

        builder.HasOne(c => c.Order)
            .WithMany(o => o.Connections)
            .HasForeignKey(c => c.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Customer)
            .WithMany(cu => cu.Connections)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Vendor)
            .WithMany()
            .HasForeignKey(c => c.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Plan)
            .WithMany()
            .HasForeignKey(c => c.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.City)
            .WithMany()
            .HasForeignKey(c => c.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the connection equipment table.</summary>
public class ConnectionEquipmentConfiguration : IEntityTypeConfiguration<ConnectionEquipment>
{
    /// <summary>Connection equipment table mapping.</summary>
    public void Configure(EntityTypeBuilder<ConnectionEquipment> builder)
    {
        builder.ToTable("ConnectionEquipment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SerialNumber).HasMaxLength(80);
        builder.Property(e => e.AssignedByUserId).HasMaxLength(450);
        builder.Property(e => e.ReplacementReason).HasMaxLength(1000);
        builder.Property(e => e.ReplacementChargeAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.Connection)
            .WithMany(c => c.Equipment)
            .HasForeignKey(e => e.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the plan change history table.</summary>
public class PlanChangeHistoryConfiguration : IEntityTypeConfiguration<PlanChangeHistory>
{
    /// <summary>Plan change history table mapping.</summary>
    public void Configure(EntityTypeBuilder<PlanChangeHistory> builder)
    {
        builder.ToTable("PlanChangeHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Reason).HasMaxLength(1000);
        builder.Property(h => h.RequestedByUserId).HasMaxLength(450);
        builder.Property(h => h.RequestedByName).HasMaxLength(150);
        builder.Property(h => h.AppliedByUserId).HasMaxLength(450);

        builder.HasOne(h => h.Connection)
            .WithMany(c => c.PlanChanges)
            .HasForeignKey(h => h.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Both plan references use the restricted delete behaviour, because SQL Server rejects a
        // table that points twice to the same table with a cascading action.
        builder.HasOne(h => h.OldPlan)
            .WithMany()
            .HasForeignKey(h => h.OldPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.NewPlan)
            .WithMany()
            .HasForeignKey(h => h.NewPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
