using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Catalog;

/// <summary>Categoría del catálogo.</summary>
public class CategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public string? ParentCategoryName { get; set; }

    public bool IsActive { get; set; }

    public int ProductCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Datos para crear una categoría.</summary>
public class CreateCategoryRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>Datos para actualizar una categoría.</summary>
public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}
