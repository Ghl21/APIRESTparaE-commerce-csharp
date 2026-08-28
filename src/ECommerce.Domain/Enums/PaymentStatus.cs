namespace ECommerce.Domain.Enums;

/// <summary>Estados posibles de un pago.</summary>
public enum PaymentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Refunded = 4
}
