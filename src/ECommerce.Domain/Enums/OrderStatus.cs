namespace ECommerce.Domain.Enums;

/// <summary>Estados posibles del ciclo de vida de un pedido.</summary>
public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7
}
