using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Tests;

/// <summary>Pruebas del flujo de checkout, cambio de estado y cancelación de pedidos.</summary>
public class OrderServiceTests
{
    private static OrderService CreateService(ApplicationDbContext context) =>
        new(context, NullLogger<OrderService>.Instance);

    private static CartService CreateCartService(ApplicationDbContext context) => new(context);

    [Fact]
    public async Task CreateFromCart_GeneraElPedidoYDescuentaElStock()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(CreateFromCart_GeneraElPedidoYDescuentaElStock));
        await CreateCartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 2, Quantity = 2 });

        var order = await CreateService(context).CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 });

        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.Equal(nameof(OrderStatus.Pending), order.Status);
        Assert.Equal(39.98m, order.SubTotal);
        Assert.Equal(6.40m, order.TaxAmount);
        Assert.Equal(99.00m, order.ShippingCost);
        Assert.Equal(145.38m, order.Total);

        var producto = await context.Products.FindAsync(2);
        Assert.Equal(1, producto!.Stock);

        // El carrito queda vacío después de confirmar la compra.
        Assert.Empty(await context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task CreateFromCart_FallaSiElCarritoEstaVacio()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(CreateFromCart_FallaSiElCarritoEstaVacio));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(context).CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 }));

        Assert.Contains("carrito", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFromCart_FallaSiLaDireccionNoPerteneceAlUsuario()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(CreateFromCart_FallaSiLaDireccionNoPerteneceAlUsuario));
        await CreateCartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(context).CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 999 }));
    }

    [Fact]
    public async Task Cancel_DevuelveLasUnidadesAlInventario()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(Cancel_DevuelveLasUnidadesAlInventario));
        await CreateCartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 2, Quantity = 2 });

        var service = CreateService(context);
        var order = await service.CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 });

        var cancelled = await service.CancelAsync(order.Id, 1, false);

        Assert.Equal(nameof(OrderStatus.Cancelled), cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);

        var producto = await context.Products.FindAsync(2);
        Assert.Equal(3, producto!.Stock);
    }

    [Fact]
    public async Task Cancel_FallaSiElPedidoEsDeOtroUsuario()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(Cancel_FallaSiElPedidoEsDeOtroUsuario));
        await CreateCartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });

        var service = CreateService(context);
        var order = await service.CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelAsync(order.Id, 99, false));
    }

    [Fact]
    public async Task UpdateStatus_RechazaTransicionesNoPermitidas()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(UpdateStatus_RechazaTransicionesNoPermitidas));
        await CreateCartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });

        var service = CreateService(context);
        var order = await service.CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 });

        // Pending sólo puede pasar a Paid o Cancelled.
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.UpdateStatusAsync(order.Id, OrderStatus.Delivered));

        Assert.Contains("No se permite pasar del estado", exception.Message);
    }

    [Fact]
    public async Task UpdateStatus_PermiteElFlujoNormalDeVenta()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(UpdateStatus_PermiteElFlujoNormalDeVenta));
        await CreateCartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });

        var service = CreateService(context);
        var order = await service.CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 });

        await service.UpdateStatusAsync(order.Id, OrderStatus.Paid);
        await service.UpdateStatusAsync(order.Id, OrderStatus.Processing);
        await service.UpdateStatusAsync(order.Id, OrderStatus.Shipped);
        var delivered = await service.UpdateStatusAsync(order.Id, OrderStatus.Delivered);

        Assert.Equal(nameof(OrderStatus.Delivered), delivered.Status);
        Assert.NotNull(delivered.PaidAt);
        Assert.NotNull(delivered.ShippedAt);
        Assert.NotNull(delivered.DeliveredAt);
    }
}
