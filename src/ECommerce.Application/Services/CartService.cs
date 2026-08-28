using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

/// <summary>Carrito de compras del usuario autenticado.</summary>
public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;

    public CartService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto> GetMineAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);
        return cart.ToDto();
    }

    public async Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Producto", request.ProductId);

        if (!product.IsActive)
        {
            throw new BusinessRuleException($"El producto {product.Name} no está disponible.");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == product.Id);
        var requestedQuantity = (item?.Quantity ?? 0) + request.Quantity;

        if (product.Stock < requestedQuantity)
        {
            throw new BusinessRuleException(
                $"Stock insuficiente para {product.Name}. Disponible: {product.Stock}, solicitado: {requestedQuantity}.");
        }

        if (item is null)
        {
            item = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Product = product,
                Quantity = request.Quantity,
                UnitPrice = product.EffectivePrice,
                CreatedAt = DateTime.UtcNow
            };

            cart.Items.Add(item);
            _context.CartItems.Add(item);
        }
        else
        {
            item.Quantity = requestedQuantity;
            // Se refresca el precio por si cambió desde que se agregó al carrito.
            item.UnitPrice = product.EffectivePrice;
            item.UpdatedAt = DateTime.UtcNow;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return (await GetOrCreateCartAsync(userId, ct)).ToDto();
    }

    public async Task<CartDto> UpdateItemAsync(
        int userId,
        int itemId,
        UpdateCartItemRequest request,
        CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Línea del carrito", itemId);

        if (item.Product.Stock < request.Quantity)
        {
            throw new BusinessRuleException(
                $"Stock insuficiente para {item.Product.Name}. Disponible: {item.Product.Stock}.");
        }

        item.Quantity = request.Quantity;
        item.UnitPrice = item.Product.EffectivePrice;
        item.UpdatedAt = DateTime.UtcNow;
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return cart.ToDto();
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int itemId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Línea del carrito", itemId);

        cart.Items.Remove(item);
        _context.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return cart.ToDto();
    }

    public async Task ClearAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, ct);

        if (cart.Items.Count == 0)
        {
            return;
        }

        _context.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>Devuelve el carrito del usuario y lo crea la primera vez que se solicita.</summary>
    private async Task<Cart> GetOrCreateCartAsync(int userId, CancellationToken ct)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is not null)
        {
            return cart;
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId, ct);

        if (!userExists)
        {
            throw new NotFoundException("Usuario", userId);
        }

        cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync(ct);

        return cart;
    }
}
