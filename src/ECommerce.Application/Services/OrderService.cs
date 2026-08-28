using ECommerce.Application.Common;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>Creación y seguimiento de pedidos.</summary>
public class OrderService : IOrderService
{
    /// <summary>Transiciones de estado permitidas.</summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
        [OrderStatus.Paid] = new[] { OrderStatus.Processing, OrderStatus.Cancelled, OrderStatus.Refunded },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = new[] { OrderStatus.Refunded },
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
        [OrderStatus.Refunded] = Array.Empty<OrderStatus>()
    };

    private readonly IApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IApplicationDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<OrderListItemDto>> GetAllAsync(
        OrderQueryParameters parameters,
        int? userIdFilter,
        CancellationToken ct = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .AsQueryable();

        // userIdFilter viene informado cuando el solicitante no es administrador.
        if (userIdFilter.HasValue)
        {
            query = query.Where(o => o.UserId == userIdFilter.Value);
        }
        else if (parameters.UserId.HasValue)
        {
            query = query.Where(o => o.UserId == parameters.UserId.Value);
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(o => o.Status == parameters.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.OrderNumber))
        {
            var number = parameters.OrderNumber.Trim();
            query = query.Where(o => o.OrderNumber.Contains(number));
        }

        if (parameters.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= parameters.FromDate.Value);
        }

        if (parameters.ToDate.HasValue)
        {
            var to = parameters.ToDate.Value.Date.AddDays(1);
            query = query.Where(o => o.CreatedAt < to);
        }

        query = parameters.SortBy?.ToLowerInvariant() switch
        {
            "total" => parameters.SortDescending
                ? query.OrderByDescending(o => o.Total)
                : query.OrderBy(o => o.Total),
            "status" => parameters.SortDescending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            _ => parameters.SortDescending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var orders = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                CustomerName = o.User.FirstName + " " + o.User.LastName,
                Status = o.Status.ToString(),
                Total = o.Total,
                TotalItems = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<OrderListItemDto>(orders, total, parameters.PageNumber, parameters.PageSize);
    }

    public async Task<OrderDto> GetByIdAsync(int id, int userId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await QueryWithDetails().FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Pedido", id);

        if (order.UserId != userId && !isAdmin)
        {
            throw new ForbiddenException("No tiene permiso para consultar este pedido.");
        }

        return order.ToDto();
    }

    public async Task<OrderDto> CreateFromCartAsync(
        int userId,
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new BusinessRuleException("El carrito está vacío.");
        }

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId && a.UserId == userId, ct)
            ?? throw new NotFoundException("Dirección de envío", request.ShippingAddressId);

        // Se revalida disponibilidad y precio contra el catálogo antes de cobrar.
        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
            {
                throw new BusinessRuleException($"El producto {item.Product.Name} ya no está disponible.");
            }

            if (item.Product.Stock < item.Quantity)
            {
                throw new BusinessRuleException(
                    $"Stock insuficiente para {item.Product.Name}. Disponible: {item.Product.Stock}, solicitado: {item.Quantity}.");
            }
        }

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(ct),
            UserId = userId,
            Status = OrderStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ShippingRecipientName = address.RecipientName,
            ShippingStreet = string.IsNullOrWhiteSpace(address.Street2)
                ? address.Street
                : $"{address.Street}, {address.Street2}",
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPostalCode = address.PostalCode,
            ShippingCountry = address.Country,
            ShippingPhoneNumber = address.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            var unitPrice = item.Product.EffectivePrice;

            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductSku = item.Product.Sku,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                LineTotal = PricingRules.Round(unitPrice * item.Quantity),
                CreatedAt = DateTime.UtcNow
            });

            // El stock se descuenta al confirmar el pedido para reservar la mercancía.
            item.Product.Stock -= item.Quantity;
            item.Product.UpdatedAt = DateTime.UtcNow;
        }

        order.SubTotal = PricingRules.Round(order.Items.Sum(i => i.LineTotal));
        order.TaxAmount = PricingRules.CalculateTax(order.SubTotal);
        order.ShippingCost = PricingRules.CalculateShipping(order.SubTotal);
        order.DiscountAmount = 0m;
        order.Total = PricingRules.Round(order.SubTotal + order.TaxAmount + order.ShippingCost - order.DiscountAmount);

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;

        try
        {
            if (_context.Database.IsRelational())
            {
                // El proveedor está configurado con reintentos, por lo que la transacción
                // debe ejecutarse completa dentro de la estrategia de ejecución.
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                    try
                    {
                        await _context.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(ct);
                        throw;
                    }
                });
            }
            else
            {
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflicto de concurrencia al crear el pedido del usuario {UserId}", userId);

            throw new ConflictException(
                "El stock de alguno de los productos cambió mientras se procesaba el pedido. Intente nuevamente.");
        }

        _logger.LogInformation("Pedido {OrderNumber} creado para el usuario {UserId}", order.OrderNumber, userId);

        return await GetByIdAsync(order.Id, userId, true, ct);
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, OrderStatus status, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Pedido", id);

        if (order.Status == status)
        {
            return await GetByIdAsync(order.Id, order.UserId, true, ct);
        }

        EnsureTransitionAllowed(order.Status, status);

        if (status == OrderStatus.Cancelled || status == OrderStatus.Refunded)
        {
            await RestoreStockAsync(order, ct);
        }

        ApplyStatus(order, status);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(order.Id, order.UserId, true, ct);
    }

    public async Task<OrderDto> CancelAsync(int id, int userId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("Pedido", id);

        if (order.UserId != userId && !isAdmin)
        {
            throw new ForbiddenException("No tiene permiso para cancelar este pedido.");
        }

        if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
        {
            throw new BusinessRuleException("Un pedido enviado o entregado no puede cancelarse.");
        }

        EnsureTransitionAllowed(order.Status, OrderStatus.Cancelled);

        await RestoreStockAsync(order, ct);
        ApplyStatus(order, OrderStatus.Cancelled);

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(order.Id, userId, isAdmin, ct);
    }

    private IQueryable<Order> QueryWithDetails() =>
        _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.Payments);

    private static void EnsureTransitionAllowed(OrderStatus current, OrderStatus target)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
        {
            throw new BusinessRuleException($"No se permite pasar del estado {current} a {target}.");
        }
    }

    private static void ApplyStatus(Order order, OrderStatus status)
    {
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        switch (status)
        {
            case OrderStatus.Paid:
                order.PaidAt = DateTime.UtcNow;
                break;
            case OrderStatus.Shipped:
                order.ShippedAt = DateTime.UtcNow;
                break;
            case OrderStatus.Delivered:
                order.DeliveredAt = DateTime.UtcNow;
                break;
            case OrderStatus.Cancelled:
                order.CancelledAt = DateTime.UtcNow;
                break;
        }
    }

    /// <summary>Devuelve al catálogo las unidades reservadas por un pedido cancelado o reembolsado.</summary>
    private async Task RestoreStockAsync(Order order, CancellationToken ct)
    {
        var productIds = order.Items.Select(i => i.ProductId).ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);

            if (product is null)
            {
                continue;
            }

            product.Stock += item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Genera un número de pedido legible y único: ORD-AAAAMMDD-XXXXXX.</summary>
    private async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

            if (!await _context.Orders.AnyAsync(o => o.OrderNumber == candidate, ct))
            {
                return candidate;
            }
        }

        throw new ConflictException("No fue posible generar un número de pedido único. Intente nuevamente.");
    }
}
