using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Categoría del catálogo. Soporta jerarquía mediante <see cref="ParentCategoryId"/>.</summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
