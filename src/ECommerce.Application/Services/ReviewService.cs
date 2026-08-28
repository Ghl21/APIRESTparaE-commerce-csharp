using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

/// <summary>Reseñas de productos. Cada usuario puede dejar una sola reseña por producto.</summary>
public class ReviewService : IReviewService
{
    private readonly IApplicationDbContext _context;

    public ReviewService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ReviewDto>> GetByProductAsync(
        int productId,
        QueryParameters parameters,
        CancellationToken ct = default)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId, ct);

        if (!productExists)
        {
            throw new NotFoundException("Producto", productId);
        }

        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId && r.IsApproved);

        query = parameters.SortBy?.ToLowerInvariant() switch
        {
            "rating" => parameters.SortDescending
                ? query.OrderByDescending(r => r.Rating)
                : query.OrderBy(r => r.Rating),
            _ => parameters.SortDescending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var reviews = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(
            reviews.Select(r => r.ToDto()).ToList(),
            total,
            parameters.PageNumber,
            parameters.PageSize);
    }

    public async Task<ReviewDto> CreateOrUpdateAsync(
        int productId,
        int userId,
        CreateReviewRequest request,
        CancellationToken ct = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("Producto", productId);

        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId, ct);

        if (review is null)
        {
            review = new Review
            {
                ProductId = product.Id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
        }
        else
        {
            review.UpdatedAt = DateTime.UtcNow;
        }

        review.Rating = request.Rating;
        review.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        review.IsApproved = true;

        await _context.SaveChangesAsync(ct);

        var saved = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstAsync(r => r.Id == review.Id, ct);

        return saved.ToDto();
    }

    public async Task DeleteAsync(int reviewId, int userId, bool isAdmin, CancellationToken ct = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new NotFoundException("Reseña", reviewId);

        if (review.UserId != userId && !isAdmin)
        {
            throw new ForbiddenException("Sólo puede eliminar sus propias reseñas.");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(ct);
    }
}
