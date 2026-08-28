using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

/// <summary>Consulta y administración del catálogo de productos.</summary>
public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductListItemDto>> GetAllAsync(
        ProductQueryParameters parameters,
        CancellationToken ct = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // Por defecto el catálogo público sólo muestra productos activos.
        if (parameters.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == parameters.IsActive.Value);
        }
        else
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Sku.Contains(term) ||
                (p.Description != null && p.Description.Contains(term)));
        }

        if (parameters.CategoryId.HasValue)
        {
            // Incluye los productos de las subcategorías directas.
            var categoryId = parameters.CategoryId.Value;
            query = query.Where(p => p.CategoryId == categoryId || p.Category.ParentCategoryId == categoryId);
        }

        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= parameters.MaxPrice.Value);
        }

        if (parameters.InStock.HasValue)
        {
            query = parameters.InStock.Value
                ? query.Where(p => p.Stock > 0)
                : query.Where(p => p.Stock <= 0);
        }

        query = parameters.SortBy?.ToLowerInvariant() switch
        {
            "name" => parameters.SortDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "price" => parameters.SortDescending
                ? query.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                : query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "stock" => parameters.SortDescending
                ? query.OrderByDescending(p => p.Stock)
                : query.OrderBy(p => p.Stock),
            "sku" => parameters.SortDescending
                ? query.OrderByDescending(p => p.Sku)
                : query.OrderBy(p => p.Sku),
            _ => parameters.SortDescending
                ? query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
                : query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Slug = p.Slug,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                EffectivePrice = p.DiscountPrice != null && p.DiscountPrice > 0m && p.DiscountPrice < p.Price
                    ? p.DiscountPrice.Value
                    : p.Price,
                Stock = p.Stock,
                InStock = p.Stock > 0,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                MainImageUrl = p.MainImageUrl,
                IsActive = p.IsActive,
                AverageRating = p.Reviews.Where(r => r.IsApproved).Average(r => (double?)r.Rating) ?? 0d,
                ReviewCount = p.Reviews.Count(r => r.IsApproved)
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.AverageRating = Math.Round(item.AverageRating, 2);
        }

        return new PagedResult<ProductListItemDto>(items, total, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await QueryWithDetails().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Producto", id);

        return product.ToDto();
    }

    public async Task<ProductDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();

        var product = await QueryWithDetails().FirstOrDefaultAsync(p => p.Slug == normalized, ct)
            ?? throw new NotFoundException($"No se encontró el producto con slug {slug}.");

        return product.ToDto();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await _context.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            throw new ConflictException($"Ya existe un producto con el SKU {sku}.");
        }

        await EnsureCategoryExistsAsync(request.CategoryId, ct);
        ValidatePrices(request.Price, request.DiscountPrice);

        var product = new Product
        {
            Sku = sku,
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.Name, null, ct),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            Stock = request.Stock,
            CategoryId = request.CategoryId,
            MainImageUrl = string.IsNullOrWhiteSpace(request.MainImageUrl) ? null : request.MainImageUrl.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var image in request.Images)
        {
            product.Images.Add(new ProductImage
            {
                Url = image.Url.Trim(),
                AltText = image.AltText?.Trim(),
                DisplayOrder = image.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Producto", id);

        var sku = request.Sku.Trim().ToUpperInvariant();

        if (await _context.Products.AnyAsync(p => p.Sku == sku && p.Id != id, ct))
        {
            throw new ConflictException($"Ya existe otro producto con el SKU {sku}.");
        }

        await EnsureCategoryExistsAsync(request.CategoryId, ct);
        ValidatePrices(request.Price, request.DiscountPrice);

        var name = request.Name.Trim();

        if (!string.Equals(name, product.Name, StringComparison.Ordinal))
        {
            product.Slug = await GenerateUniqueSlugAsync(name, id, ct);
        }

        product.Sku = sku;
        product.Name = name;
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.Stock = request.Stock;
        product.CategoryId = request.CategoryId;
        product.MainImageUrl = string.IsNullOrWhiteSpace(request.MainImageUrl) ? null : request.MainImageUrl.Trim();
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        // La galería se reemplaza por completo con la lista enviada.
        _context.ProductImages.RemoveRange(product.Images);
        product.Images.Clear();

        foreach (var image in request.Images)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                Url = image.Url.Trim(),
                AltText = image.AltText?.Trim(),
                DisplayOrder = image.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductDto> UpdateStockAsync(int id, int stock, CancellationToken ct = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Producto", id);

        if (stock < 0)
        {
            throw new BusinessRuleException("El stock no puede ser negativo.");
        }

        product.Stock = stock;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(product.Id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Producto", id);

        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id, ct);

        if (hasOrders)
        {
            // Se conserva el histórico de ventas: el producto se desactiva en lugar de borrarse.
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return;
        }

        var cartItems = await _context.CartItems.Where(ci => ci.ProductId == id).ToListAsync(ct);
        _context.CartItems.RemoveRange(cartItems);
        _context.ProductImages.RemoveRange(product.Images);
        _context.Products.Remove(product);

        await _context.SaveChangesAsync(ct);
    }

    private IQueryable<Product> QueryWithDetails() =>
        _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews);

    private static void ValidatePrices(decimal price, decimal? discountPrice)
    {
        if (price <= 0m)
        {
            throw new BusinessRuleException("El precio debe ser mayor a cero.");
        }

        if (discountPrice.HasValue && discountPrice.Value >= price)
        {
            throw new BusinessRuleException("El precio con descuento debe ser menor al precio base.");
        }
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken ct)
    {
        var exists = await _context.Categories.AnyAsync(c => c.Id == categoryId, ct);

        if (!exists)
        {
            throw new NotFoundException("Categoría", categoryId);
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, int? excludeId, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.Generate(name);

        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = "producto";
        }

        var slug = baseSlug;
        var suffix = 1;

        while (await _context.Products.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), ct))
        {
            suffix++;
            slug = $"{baseSlug}-{suffix}";
        }

        return slug;
    }
}
