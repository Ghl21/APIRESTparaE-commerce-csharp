using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Producto del catálogo.</summary>
public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int Stock { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string? MainImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Columna rowversion usada para control de concurrencia optimista.</summary>
    public byte[]? RowVersion { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>Precio efectivo de venta (aplica descuento cuando existe y es menor al precio base).</summary>
    public decimal EffectivePrice =>
        DiscountPrice.HasValue && DiscountPrice.Value > 0m && DiscountPrice.Value < Price
            ? DiscountPrice.Value
            : Price;
}
