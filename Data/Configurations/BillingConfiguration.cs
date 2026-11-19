using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusServiceMarketingSystem.Models;

namespace NexusServiceMarketingSystem.Data.Configurations;

/// <summary>Mapping rules for the bill table.</summary>
public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    /// <summary>Bill table mapping including the unique bill number and the payment status.</summary>
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("Bills");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BillNumber).IsRequired().HasMaxLength(30);
        builder.Property(b => b.GeneratedByUserId).HasMaxLength(450);
        builder.Property(b => b.GeneratedByName).HasMaxLength(150);
        builder.Property(b => b.Remarks).HasMaxLength(1000);

        foreach (var money in new[]
                 {
                     builder.Property(b => b.PlanCharge),
                     builder.Property(b => b.LocalChargeAmount),
                     builder.Property(b => b.StdChargeAmount),
                     builder.Property(b => b.MobileChargeAmount),
                     builder.Property(b => b.ReplacementChargeAmount),
                     builder.Property(b => b.EquipmentChargeAmount),
                     builder.Property(b => b.PreviousDue),
                     builder.Property(b => b.DiscountAmount),
                     builder.Property(b => b.SubTotal),
                     builder.Property(b => b.TaxAmount),
                     builder.Property(b => b.TotalAmount),
                     builder.Property(b => b.AmountPaid),
                     builder.Property(b => b.AmountDue)
                 })
        {
            money.HasColumnType("decimal(18,2)");
        }
        builder.Property(b => b.DiscountPercent).HasColumnType("decimal(5,2)");
        builder.Property(b => b.TaxPercent).HasColumnType("decimal(5,2)");

        builder.HasIndex(b => b.BillNumber).IsUnique().HasDatabaseName("IX_Bills_BillNumber");
        // Powers the till date reports and the due list of the accounts department.
        builder.HasIndex(b => new { b.ConnectionId, b.PeriodStart });
        builder.HasIndex(b => new { b.Status, b.DueDate });

        builder.HasOne(b => b.Connection)
            .WithMany(c => c.Bills)
            .HasForeignKey(b => b.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Order)
            .WithMany()
            .HasForeignKey(b => b.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the bill line table.</summary>
public class BillLineConfiguration : IEntityTypeConfiguration<BillLine>
{
    /// <summary>Bill line table mapping.</summary>
    public void Configure(EntityTypeBuilder<BillLine> builder)
    {
        builder.ToTable("BillLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Description).IsRequired().HasMaxLength(300);
        builder.Property(l => l.LineType).HasMaxLength(60);
        builder.Property(l => l.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");

        builder.HasOne(l => l.Bill)
            .WithMany(b => b.Lines)
            .HasForeignKey(l => l.BillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Mapping rules for the payment table.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <summary>Payment table mapping including the unique receipt number.</summary>
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ReceiptNumber).IsRequired().HasMaxLength(30);
        builder.Property(p => p.ReceivedByUserId).HasMaxLength(450);
        builder.Property(p => p.ReceivedByName).HasMaxLength(150);
        builder.Property(p => p.Remarks).HasMaxLength(1000);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.ReceiptNumber).IsUnique().HasDatabaseName("IX_Payments_ReceiptNumber");
        builder.HasIndex(p => new { p.PaidOn, p.BillId });

        builder.HasOne(p => p.Bill)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Connection)
            .WithMany()
            .HasForeignKey(p => p.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the security deposit ledger.</summary>
public class SecurityDepositEntryConfiguration : IEntityTypeConfiguration<SecurityDepositEntry>
{
    /// <summary>Security deposit ledger mapping.</summary>
    public void Configure(EntityTypeBuilder<SecurityDepositEntry> builder)
    {
        builder.ToTable("SecurityDepositEntries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Amount).HasColumnType("decimal(18,2)");
        builder.Property(d => d.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(d => d.NetAmount).HasColumnType("decimal(18,2)");
        builder.Property(d => d.RefundedByUserId).HasMaxLength(450);
        builder.Property(d => d.Remarks).HasMaxLength(1000);

        builder.HasOne(d => d.Order)
            .WithMany()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Connection)
            .WithMany()
            .HasForeignKey(d => d.ConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the purchase order tables.</summary>
public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    /// <summary>Purchase order table mapping.</summary>
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PurchaseOrderNumber).IsRequired().HasMaxLength(30);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.CreatedByUserId).HasMaxLength(450);
        builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(p => p.AmountDue).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.PurchaseOrderNumber).IsUnique().HasDatabaseName("IX_PurchaseOrders_Number");
        builder.HasIndex(p => new { p.Status, p.PaymentStatus });

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the purchase order item table.</summary>
public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    /// <summary>Purchase order item table mapping.</summary>
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.PurchaseOrder)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapping rules for the feedback table.</summary>
public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    /// <summary>Feedback table mapping.</summary>
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedback");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Comments).HasMaxLength(2000);
        builder.Property(f => f.AdminResponse).HasMaxLength(1000);

        builder.HasOne(f => f.Customer)
            .WithMany()
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Order)
            .WithMany()
            .HasForeignKey(f => f.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.IsApproved, f.Rating });
    }
}

/// <summary>Mapping rules for the contact message table.</summary>
public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    /// <summary>Contact message table mapping.</summary>
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(150);
        builder.Property(m => m.Email).HasMaxLength(150);
        builder.Property(m => m.Phone).HasMaxLength(25);
        builder.Property(m => m.Subject).HasMaxLength(250);
        builder.Property(m => m.Message).IsRequired().HasMaxLength(4000);
        builder.HasIndex(m => m.IsRead);
    }
}

/// <summary>Mapping rules for the audit log table.</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <summary>Audit log table mapping.</summary>
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.UserName).HasMaxLength(150);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(120);
        builder.Property(a => a.EntityType).HasMaxLength(80);
        builder.Property(a => a.EntityId).HasMaxLength(80);
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(80);
        builder.HasIndex(a => new { a.CreatedAt, a.EntityType });
    }
}
