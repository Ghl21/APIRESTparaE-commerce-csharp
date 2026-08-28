using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Mapping;

/// <summary>
/// Conversión manual de entidades a DTOs. Se prefiere sobre un mapeador automático
/// para mantener explícito qué datos se exponen en la API.
/// </summary>
public static class MappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        Roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role.Name)
            .OrderBy(name => name)
            .ToList()
    };

    public static CategoryDto ToDto(this Category category, int productCount = 0) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = category.Slug,
        Description = category.Description,
        ParentCategoryId = category.ParentCategoryId,
        ParentCategoryName = category.ParentCategory?.Name,
        IsActive = category.IsActive,
        ProductCount = productCount,
        CreatedAt = category.CreatedAt
    };

    public static ProductDto ToDto(this Product product)
    {
        var approvedReviews = product.Reviews.Where(r => r.IsApproved).ToList();

        return new ProductDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            EffectivePrice = product.EffectivePrice,
            Stock = product.Stock,
            InStock = product.Stock > 0,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            MainImageUrl = product.MainImageUrl,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            AverageRating = approvedReviews.Count == 0
                ? 0d
                : Math.Round(approvedReviews.Average(r => r.Rating), 2),
            ReviewCount = approvedReviews.Count,
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ToDto())
                .ToList()
        };
    }

    public static ProductImageDto ToDto(this ProductImage image) => new()
    {
        Id = image.Id,
        Url = image.Url,
        AltText = image.AltText,
        DisplayOrder = image.DisplayOrder
    };

    public static ReviewDto ToDto(this Review review) => new()
    {
        Id = review.Id,
        ProductId = review.ProductId,
        ProductName = review.Product?.Name ?? string.Empty,
        UserId = review.UserId,
        UserName = review.User?.FullName ?? string.Empty,
        Rating = review.Rating,
        Title = review.Title,
        Comment = review.Comment,
        IsApproved = review.IsApproved,
        CreatedAt = review.CreatedAt
    };

    public static AddressDto ToDto(this Address address) => new()
    {
        Id = address.Id,
        Alias = address.Alias,
        RecipientName = address.RecipientName,
        Street = address.Street,
        Street2 = address.Street2,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
        Country = address.Country,
        PhoneNumber = address.PhoneNumber,
        IsDefault = address.IsDefault
    };

    public static CartItemDto ToDto(this CartItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.Product?.Name ?? string.Empty,
        ProductSku = item.Product?.Sku ?? string.Empty,
        ImageUrl = item.Product?.MainImageUrl,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        LineTotal = item.LineTotal,
        AvailableStock = item.Product?.Stock ?? 0
    };

    public static CartDto ToDto(this Cart cart)
    {
        var items = cart.Items.OrderBy(i => i.Id).Select(i => i.ToDto()).ToList();
        var subTotal = PricingRules.Round(items.Sum(i => i.LineTotal));
        var tax = PricingRules.CalculateTax(subTotal);
        var shipping = PricingRules.CalculateShipping(subTotal);

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items,
            TotalItems = items.Sum(i => i.Quantity),
            SubTotal = subTotal,
            EstimatedTax = tax,
            EstimatedShipping = shipping,
            EstimatedTotal = PricingRules.Round(subTotal + tax + shipping),
            UpdatedAt = cart.UpdatedAt
        };
    }

    public static OrderItemDto ToDto(this OrderItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        ProductSku = item.ProductSku,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        LineTotal = item.LineTotal
    };

    public static PaymentDto ToDto(this Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        OrderNumber = payment.Order?.OrderNumber ?? string.Empty,
        Method = payment.Method.ToString(),
        Status = payment.Status.ToString(),
        Amount = payment.Amount,
        TransactionId = payment.TransactionId,
        CardLastFourDigits = payment.CardLastFourDigits,
        FailureReason = payment.FailureReason,
        ProcessedAt = payment.ProcessedAt,
        CreatedAt = payment.CreatedAt
    };

    public static OrderDto ToDto(this Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        UserId = order.UserId,
        CustomerName = order.User?.FullName ?? string.Empty,
        Status = order.Status.ToString(),
        SubTotal = order.SubTotal,
        TaxAmount = order.TaxAmount,
        ShippingCost = order.ShippingCost,
        DiscountAmount = order.DiscountAmount,
        Total = order.Total,
        Notes = order.Notes,
        TotalItems = order.Items.Sum(i => i.Quantity),
        CreatedAt = order.CreatedAt,
        PaidAt = order.PaidAt,
        ShippedAt = order.ShippedAt,
        DeliveredAt = order.DeliveredAt,
        CancelledAt = order.CancelledAt,
        ShippingAddress = new AddressSnapshotDto
        {
            RecipientName = order.ShippingRecipientName,
            Street = order.ShippingStreet,
            City = order.ShippingCity,
            State = order.ShippingState,
            PostalCode = order.ShippingPostalCode,
            Country = order.ShippingCountry,
            PhoneNumber = order.ShippingPhoneNumber
        },
        Items = order.Items.OrderBy(i => i.Id).Select(i => i.ToDto()).ToList(),
        Payments = order.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.ToDto()).ToList()
    };
}
