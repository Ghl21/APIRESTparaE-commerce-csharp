using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Línea de un pedido. Guarda una copia de los datos del producto vendido.</summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
