using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

/// <summary>Pedido generado a partir del carrito de un usuario.</summary>
public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Total { get; set; }

    public string? Notes { get; set; }

    // Copia (snapshot) de la dirección de envío en el momento de la compra.
    public string ShippingRecipientName { get; set; } = string.Empty;

    public string ShippingStreet { get; set; } = string.Empty;

    public string ShippingCity { get; set; } = string.Empty;

    public string ShippingState { get; set; } = string.Empty;

    public string ShippingPostalCode { get; set; } = string.Empty;

    public string ShippingCountry { get; set; } = string.Empty;

    public string? ShippingPhoneNumber { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
