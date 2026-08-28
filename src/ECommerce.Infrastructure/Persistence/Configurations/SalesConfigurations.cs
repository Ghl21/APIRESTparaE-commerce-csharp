using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de sales.Carts.</summary>
public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts", "sales");

        builder.HasKey(c => c.Id);

        builder.Ignore(c => c.SubTotal);
        builder.Ignore(c => c.TotalItems);

        // Un único carrito por usuario.
        builder.HasIndex(c => c.UserId).IsUnique().HasDatabaseName("UX_Carts_UserId");
    }
}

/// <summary>Mapeo de sales.CartItems.</summary>
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems", "sales");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Quantity).IsRequired();
        builder.Property(ci => ci.UnitPrice).IsRequired().HasPrecision(18, 2);

        builder.Ignore(ci => ci.LineTotal);

        builder.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique().HasDatabaseName("UX_CartItems_Cart_Product");

        builder.HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Product)
            .WithMany(p => p.CartItems)
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de sales.Orders.</summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", "sales");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(30);
        builder.Property(o => o.Status).IsRequired().HasConversion<int>();
        builder.Property(o => o.SubTotal).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.TaxAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.ShippingCost).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.DiscountAmount).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.Total).IsRequired().HasPrecision(18, 2);
        builder.Property(o => o.Notes).HasMaxLength(500);

        builder.Property(o => o.ShippingRecipientName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.ShippingStreet).IsRequired().HasMaxLength(400);
        builder.Property(o => o.ShippingCity).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingState).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingPostalCode).IsRequired().HasMaxLength(20);
        builder.Property(o => o.ShippingCountry).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingPhoneNumber).HasMaxLength(30);

        builder.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("UX_Orders_OrderNumber");
        builder.HasIndex(o => o.UserId).HasDatabaseName("IX_Orders_UserId");
        builder.HasIndex(o => o.Status).HasDatabaseName("IX_Orders_Status");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Orders_CreatedAt");

        // No se borran usuarios con historial de compras.
        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de sales.OrderItems.</summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", "sales");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(oi => oi.ProductSku).IsRequired().HasMaxLength(50);
        builder.Property(oi => oi.UnitPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(oi => oi.Quantity).IsRequired();
        builder.Property(oi => oi.LineTotal).IsRequired().HasPrecision(18, 2);

        builder.HasIndex(oi => oi.OrderId).HasDatabaseName("IX_OrderItems_OrderId");
        builder.HasIndex(oi => oi.ProductId).HasDatabaseName("IX_OrderItems_ProductId");

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de sales.Payments.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "sales");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Method).IsRequired().HasConversion<int>();
        builder.Property(p => p.Status).IsRequired().HasConversion<int>();
        builder.Property(p => p.Amount).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.TransactionId).HasMaxLength(60);
        builder.Property(p => p.CardLastFourDigits).HasMaxLength(4);
        builder.Property(p => p.FailureReason).HasMaxLength(300);

        builder.HasIndex(p => p.OrderId).HasDatabaseName("IX_Payments_OrderId");
        builder.HasIndex(p => p.TransactionId).HasDatabaseName("IX_Payments_TransactionId");

        builder.HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
