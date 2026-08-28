using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Carrito de compras. Existe como máximo uno activo por usuario.</summary>
public class Cart : BaseEntity
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public decimal SubTotal => Items.Sum(i => i.LineTotal);

    public int TotalItems => Items.Sum(i => i.Quantity);
}
