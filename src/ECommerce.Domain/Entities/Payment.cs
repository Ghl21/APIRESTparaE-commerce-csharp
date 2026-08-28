using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

/// <summary>Pago asociado a un pedido.</summary>
public class Payment : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public string? CardLastFourDigits { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
