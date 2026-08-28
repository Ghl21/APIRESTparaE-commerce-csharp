using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Services;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Tests;

/// <summary>Pruebas del carrito de compras sobre una base en memoria.</summary>
public class CartServiceTests
{
    [Fact]
    public async Task AddItem_AgregaElProductoConSuPrecioEfectivo()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(AddItem_AgregaElProductoConSuPrecioEfectivo));
        var service = new CartService(context);

        var cart = await service.AddItemAsync(1, new AddCartItemRequest { ProductId = 2, Quantity = 2 });

        var item = Assert.Single(cart.Items);
        Assert.Equal(2, item.ProductId);
        Assert.Equal(2, item.Quantity);
        // El producto 2 tiene precio 25 con descuento a 19.99.
        Assert.Equal(19.99m, item.UnitPrice);
        Assert.Equal(39.98m, cart.SubTotal);
    }

    [Fact]
    public async Task AddItem_AcumulaLaCantidadSiElProductoYaEstaEnElCarrito()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(AddItem_AcumulaLaCantidadSiElProductoYaEstaEnElCarrito));
        var service = new CartService(context);

        await service.AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });
        var cart = await service.AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 2 });

        var item = Assert.Single(cart.Items);
        Assert.Equal(3, item.Quantity);
    }

    [Fact]
    public async Task AddItem_FallaCuandoNoHayStockSuficiente()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(AddItem_FallaCuandoNoHayStockSuficiente));
        var service = new CartService(context);

        // El producto 2 sólo tiene 3 unidades disponibles.
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.AddItemAsync(1, new AddCartItemRequest { ProductId = 2, Quantity = 4 }));

        Assert.Contains("Stock insuficiente", exception.Message);
    }

    [Fact]
    public async Task AddItem_FallaSiElProductoNoExiste()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(AddItem_FallaSiElProductoNoExiste));
        var service = new CartService(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AddItemAsync(1, new AddCartItemRequest { ProductId = 999, Quantity = 1 }));
    }

    [Fact]
    public async Task GetMine_CalculaImpuestoYEnvioEstimados()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(GetMine_CalculaImpuestoYEnvioEstimados));
        var service = new CartService(context);

        await service.AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });
        var cart = await service.GetMineAsync(1);

        Assert.Equal(1200m, cart.SubTotal);
        Assert.Equal(192m, cart.EstimatedTax);
        // 1200 no alcanza el umbral de 1500, por lo que se cobra el envío estándar.
        Assert.Equal(99m, cart.EstimatedShipping);
        Assert.Equal(1491m, cart.EstimatedTotal);
    }

    [Fact]
    public async Task GetMine_NoCobraEnvioAlSuperarElUmbral()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(GetMine_NoCobraEnvioAlSuperarElUmbral));
        var service = new CartService(context);

        await service.AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 2 });
        var cart = await service.GetMineAsync(1);

        Assert.Equal(2400m, cart.SubTotal);
        Assert.Equal(0m, cart.EstimatedShipping);
        Assert.Equal(2784m, cart.EstimatedTotal);
    }

    [Fact]
    public async Task Clear_DejaElCarritoVacio()
    {
        await using var context = await TestDbContextFactory.CreateWithCatalogAsync(nameof(Clear_DejaElCarritoVacio));
        var service = new CartService(context);

        await service.AddItemAsync(1, new AddCartItemRequest { ProductId = 1, Quantity = 1 });
        await service.ClearAsync(1);

        Assert.Empty(await context.CartItems.ToListAsync());
    }
}
