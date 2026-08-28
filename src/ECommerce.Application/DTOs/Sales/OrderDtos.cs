using System.ComponentModel.DataAnnotations;
using ECommerce.Application.DTOs.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Sales;

/// <summary>Pedido en listados.</summary>
public class OrderListItemDto
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int TotalItems { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Detalle de un pedido.</summary>
public class OrderDto : OrderListItemDto
{
    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal DiscountAmount { get; set; }

    public string? Notes { get; set; }

    public AddressSnapshotDto ShippingAddress { get; set; } = new();

    public DateTime? PaidAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public IReadOnlyList<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();

    public IReadOnlyList<PaymentDto> Payments { get; set; } = Array.Empty<PaymentDto>();
}

/// <summary>Copia de la dirección de envío guardada junto con el pedido.</summary>
public class AddressSnapshotDto
{
    public string RecipientName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
}

/// <summary>Línea de un pedido.</summary>
public class OrderItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}

/// <summary>Datos de confirmación de compra (checkout).</summary>
public class CreateOrderRequest
{
    /// <summary>Dirección registrada del usuario que se usará para el envío.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una dirección de envío válida.")]
    public int ShippingAddressId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>Cambio de estado de un pedido (uso administrativo).</summary>
public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [EnumDataType(typeof(OrderStatus), ErrorMessage = "El estado indicado no es válido.")]
    public OrderStatus Status { get; set; }
}

/// <summary>Filtros de búsqueda de pedidos.</summary>
public class OrderQueryParameters : QueryParameters
{
    public OrderStatus? Status { get; set; }

    public string? OrderNumber { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    /// <summary>Sólo aplica para administradores.</summary>
    public int? UserId { get; set; }
}
