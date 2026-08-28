using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Pasarela de pago simulada. No se comunica con ningún proveedor real ni almacena
/// datos sensibles de tarjeta: sólo persiste los últimos cuatro dígitos.
/// Regla de prueba: las tarjetas terminadas en 0000 se rechazan, el resto se aprueban.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IApplicationDbContext context, ILogger<PaymentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentDto> ProcessAsync(
        int userId,
        bool isAdmin,
        CreatePaymentRequest request,
        CancellationToken ct = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Pedido", request.OrderId);

        if (order.UserId != userId && !isAdmin)
        {
            throw new ForbiddenException("No tiene permiso para pagar este pedido.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new BusinessRuleException(
                $"Sólo se pueden pagar pedidos en estado Pending. Estado actual: {order.Status}.");
        }

        if (order.Payments.Any(p => p.Status == PaymentStatus.Approved))
        {
            throw new ConflictException("El pedido ya cuenta con un pago aprobado.");
        }

        var requiresCard = request.Method is PaymentMethod.CreditCard or PaymentMethod.DebitCard;

        if (requiresCard && string.IsNullOrWhiteSpace(request.CardNumber))
        {
            throw new BusinessRuleException("Debe indicar el número de tarjeta para el medio de pago seleccionado.");
        }

        var digits = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        var lastFour = digits.Length >= 4 ? digits[^4..] : null;

        var payment = new Payment
        {
            OrderId = order.Id,
            Method = request.Method,
            Amount = order.Total,
            CardLastFourDigits = lastFour,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Method == PaymentMethod.CashOnDelivery)
        {
            // El cobro se realiza al entregar: el pago queda pendiente y el pedido no cambia de estado.
            payment.Status = PaymentStatus.Pending;
        }
        else if (requiresCard && lastFour == "0000")
        {
            payment.Status = PaymentStatus.Rejected;
            payment.FailureReason = "La tarjeta fue rechazada por el emisor.";
            payment.ProcessedAt = DateTime.UtcNow;
        }
        else
        {
            payment.Status = PaymentStatus.Approved;
            payment.TransactionId = $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
            payment.ProcessedAt = DateTime.UtcNow;

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Pago {PaymentId} del pedido {OrderNumber} finalizó con estado {Status}",
            payment.Id,
            order.OrderNumber,
            payment.Status);

        payment.Order = order;

        return payment.ToDto();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetByOrderAsync(
        int orderId,
        int userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException("Pedido", orderId);

        if (order.UserId != userId && !isAdmin)
        {
            throw new ForbiddenException("No tiene permiso para consultar los pagos de este pedido.");
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Include(p => p.Order)
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return payments.Select(p => p.ToDto()).ToList();
    }
}
