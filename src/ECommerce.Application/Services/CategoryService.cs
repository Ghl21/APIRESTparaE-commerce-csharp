using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

/// <summary>Gestión de las categorías del catálogo, incluida su jerarquía.</summary>
public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;

    public CategoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                Category = c,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(ct);

        return categories.Select(x => x.Category.ToDto(x.ProductCount)).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await _context.Categories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                Category = c,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Categoría", id);

        return result.Category.ToDto(result.ProductCount);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var slug = await GenerateUniqueSlugAsync(name, null, ct);

        await EnsureParentExistsAsync(request.ParentCategoryId, ct);

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(category.Id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Categoría", id);

        if (request.ParentCategoryId == id)
        {
            throw new BusinessRuleException("Una categoría no puede ser su propia categoría padre.");
        }

        await EnsureParentExistsAsync(request.ParentCategoryId, ct);
        await EnsureNoCycleAsync(id, request.ParentCategoryId, ct);

        var name = request.Name.Trim();

        if (!string.Equals(name, category.Name, StringComparison.Ordinal))
        {
            category.Slug = await GenerateUniqueSlugAsync(name, id, ct);
        }

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        category.ParentCategoryId = request.ParentCategoryId;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(category.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Categoría", id);

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id, ct);
        if (hasProducts)
        {
            throw new BusinessRuleException(
                "No se puede eliminar la categoría porque tiene productos asociados. Desactívela en su lugar.");
        }

        var hasChildren = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id, ct);
        if (hasChildren)
        {
            throw new BusinessRuleException("No se puede eliminar la categoría porque tiene subcategorías.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsureParentExistsAsync(int? parentCategoryId, CancellationToken ct)
    {
        if (parentCategoryId is null)
        {
            return;
        }

        var exists = await _context.Categories.AnyAsync(c => c.Id == parentCategoryId, ct);
        if (!exists)
        {
            throw new NotFoundException("Categoría padre", parentCategoryId);
        }
    }

    /// <summary>Evita que la jerarquía forme un ciclo (A -> B -> A).</summary>
    private async Task EnsureNoCycleAsync(int categoryId, int? parentCategoryId, CancellationToken ct)
    {
        var currentId = parentCategoryId;
        var visited = new HashSet<int>();

        while (currentId.HasValue)
        {
            if (currentId.Value == categoryId)
            {
                throw new BusinessRuleException("La jerarquía de categorías indicada genera un ciclo.");
            }

            if (!visited.Add(currentId.Value))
            {
                break;
            }

            currentId = await _context.Categories
                .Where(c => c.Id == currentId.Value)
                .Select(c => c.ParentCategoryId)
                .FirstOrDefaultAsync(ct);
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, int? excludeId, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.Generate(name, 120);

        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "categoria";
        }

        var slug = baseSlug;
        var suffix = 1;

        while (await _context.Categories.AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId), ct))
        {
            suffix++;
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }
}
