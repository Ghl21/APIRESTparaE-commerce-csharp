using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using ECommerce.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.Tests;

/// <summary>Pruebas de la pasarela de pago simulada.</summary>
public class PaymentServiceTests
{
    private static async Task<(ApplicationDbContext Context, int OrderId)> CreatePendingOrderAsync(string dbName)
    {
        var context = await TestDbContextFactory.CreateWithCatalogAsync(dbName);
        await new CartService(context).AddItemAsync(1, new AddCartItemRequest { ProductId = 2, Quantity = 1 });

        var order = await new OrderService(context, NullLogger<OrderService>.Instance)
            .CreateFromCartAsync(1, new CreateOrderRequest { ShippingAddressId = 1 });

        return (context, order.Id);
    }

    private static PaymentService CreateService(ApplicationDbContext context) =>
        new(context, NullLogger<PaymentService>.Instance);

    [Fact]
    public async Task Process_ApruebaElPagoYMarcaElPedidoComoPagado()
    {
        var (context, orderId) = await CreatePendingOrderAsync(nameof(Process_ApruebaElPagoYMarcaElPedidoComoPagado));
        await using var _ = context;

        var payment = await CreateService(context).ProcessAsync(1, false, new CreatePaymentRequest
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            CardNumber = "4111111111111111"
        });

        Assert.Equal(nameof(PaymentStatus.Approved), payment.Status);
        Assert.Equal("1111", payment.CardLastFourDigits);
        Assert.NotNull(payment.TransactionId);

        var order = await context.Orders.FindAsync(orderId);
        Assert.Equal(OrderStatus.Paid, order!.Status);
        Assert.NotNull(order.PaidAt);
    }

    [Fact]
    public async Task Process_RechazaLasTarjetasDePruebaTerminadasEnCeros()
    {
        var (context, orderId) = await CreatePendingOrderAsync(nameof(Process_RechazaLasTarjetasDePruebaTerminadasEnCeros));
        await using var _ = context;

        var payment = await CreateService(context).ProcessAsync(1, false, new CreatePaymentRequest
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            CardNumber = "4111111111110000"
        });

        Assert.Equal(nameof(PaymentStatus.Rejected), payment.Status);
        Assert.NotNull(payment.FailureReason);

        var order = await context.Orders.FindAsync(orderId);
        Assert.Equal(OrderStatus.Pending, order!.Status);
    }

    [Fact]
    public async Task Process_NoPermitePagarDosVecesElMismoPedido()
    {
        var (context, orderId) = await CreatePendingOrderAsync(nameof(Process_NoPermitePagarDosVecesElMismoPedido));
        await using var _ = context;

        var service = CreateService(context);
        var request = new CreatePaymentRequest
        {
            OrderId = orderId,
            Method = PaymentMethod.CreditCard,
            CardNumber = "4111111111111111"
        };

        await service.ProcessAsync(1, false, request);

        // El segundo intento falla porque el pedido ya no está en estado Pending.
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ProcessAsync(1, false, request));
    }

    [Fact]
    public async Task Process_ExigeNumeroDeTarjetaParaPagosConTarjeta()
    {
        var (context, orderId) = await CreatePendingOrderAsync(nameof(Process_ExigeNumeroDeTarjetaParaPagosConTarjeta));
        await using var _ = context;

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService(context).ProcessAsync(1, false,
            new CreatePaymentRequest { OrderId = orderId, Method = PaymentMethod.DebitCard }));
    }

    [Fact]
    public async Task Process_ImpideQueOtroUsuarioPagueElPedido()
    {
        var (context, orderId) = await CreatePendingOrderAsync(nameof(Process_ImpideQueOtroUsuarioPagueElPedido));
        await using var _ = context;

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService(context).ProcessAsync(99, false,
            new CreatePaymentRequest
            {
                OrderId = orderId,
                Method = PaymentMethod.CreditCard,
                CardNumber = "4111111111111111"
            }));
    }

    [Fact]
    public async Task Process_DejaElPagoPendienteEnContraEntrega()
    {
        var (context, orderId) = await CreatePendingOrderAsync(nameof(Process_DejaElPagoPendienteEnContraEntrega));
        await using var _ = context;

        var payment = await CreateService(context).ProcessAsync(1, false, new CreatePaymentRequest
        {
            OrderId = orderId,
            Method = PaymentMethod.CashOnDelivery
        });

        Assert.Equal(nameof(PaymentStatus.Pending), payment.Status);

        var order = await context.Orders.FindAsync(orderId);
        Assert.Equal(OrderStatus.Pending, order!.Status);
    }
}
