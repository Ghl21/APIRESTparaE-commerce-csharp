using System.ComponentModel.DataAnnotations;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Catalog;

/// <summary>Producto en listados.</summary>
public class ProductListItemDto
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public decimal EffectivePrice { get; set; }

    public int Stock { get; set; }

    public bool InStock { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? MainImageUrl { get; set; }

    public bool IsActive { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }
}

/// <summary>Detalle completo de un producto.</summary>
public class ProductDto : ProductListItemDto
{
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<ProductImageDto> Images { get; set; } = Array.Empty<ProductImageDto>();
}

/// <summary>Imagen de producto.</summary>
public class ProductImageDto
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>Datos para crear un producto.</summary>
public class CreateProductRequest
{
    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(50, MinimumLength = 3)]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0.01, 9999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Price { get; set; }

    [Range(0.0, 9999999.99, ErrorMessage = "El precio con descuento no puede ser negativo.")]
    public decimal? DiscountPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una categoría válida.")]
    public int CategoryId { get; set; }

    [StringLength(500)]
    [Url(ErrorMessage = "La URL de la imagen no es válida.")]
    public string? MainImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateProductImageRequest> Images { get; set; } = new();
}

/// <summary>Imagen adicional al crear/actualizar un producto.</summary>
public class CreateProductImageRequest
{
    [Required]
    [StringLength(500)]
    [Url(ErrorMessage = "La URL de la imagen no es válida.")]
    public string Url { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>Datos para actualizar un producto.</summary>
public class UpdateProductRequest : CreateProductRequest
{
}

/// <summary>Ajuste manual de stock.</summary>
public class UpdateStockRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }
}

/// <summary>Filtros de búsqueda del catálogo.</summary>
public class ProductQueryParameters : QueryParameters
{
    /// <summary>Texto libre buscado en nombre, SKU y descripción.</summary>
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    [Range(0, 9999999.99)]
    public decimal? MinPrice { get; set; }

    [Range(0, 9999999.99)]
    public decimal? MaxPrice { get; set; }

    public bool? InStock { get; set; }

    /// <summary>Cuando es null se devuelven sólo los productos activos.</summary>
    public bool? IsActive { get; set; }
}
