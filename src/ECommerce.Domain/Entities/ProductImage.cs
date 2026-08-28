using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Imagen adicional asociada a un producto.</summary>
public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string Url { get; set; } = string.Empty;

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }
}
