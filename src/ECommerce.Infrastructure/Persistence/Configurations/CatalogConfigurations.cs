using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo de catalog.Categories.</summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "catalog");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(140);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.IsActive).IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("UX_Categories_Slug");

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de catalog.Products.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "catalog");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(220);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Price).IsRequired().HasPrecision(18, 2);
        builder.Property(p => p.DiscountPrice).HasPrecision(18, 2);
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.MainImageUrl).HasMaxLength(500);
        builder.Property(p => p.IsActive).IsRequired();

        // Control de concurrencia optimista sobre el stock.
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Ignore(p => p.EffectivePrice);

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("UX_Products_Sku");
        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("UX_Products_Slug");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Products_CategoryId");
        builder.HasIndex(p => p.Name).HasDatabaseName("IX_Products_Name");

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Mapeo de catalog.ProductImages.</summary>
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", "catalog");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Url).IsRequired().HasMaxLength(500);
        builder.Property(pi => pi.AltText).HasMaxLength(200);
        builder.Property(pi => pi.DisplayOrder).IsRequired();

        builder.HasIndex(pi => pi.ProductId).HasDatabaseName("IX_ProductImages_ProductId");

        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Mapeo de catalog.Reviews.</summary>
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", "catalog");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(150);
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.IsApproved).IsRequired();

        // Un usuario sólo puede reseñar una vez cada producto.
        builder.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique().HasDatabaseName("UX_Reviews_Product_User");

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
