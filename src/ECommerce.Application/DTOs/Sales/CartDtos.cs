using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Sales;

/// <summary>Carrito de compras del usuario autenticado.</summary>
public class CartDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public IReadOnlyList<CartItemDto> Items { get; set; } = Array.Empty<CartItemDto>();

    public int TotalItems { get; set; }

    public decimal SubTotal { get; set; }

    public decimal EstimatedTax { get; set; }

    public decimal EstimatedShipping { get; set; }

    public decimal EstimatedTotal { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Linea del carrito.</summary>
public class CartItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public int AvailableStock { get; set; }
}

/// <summary>Alta de un producto en el carrito.</summary>
public class AddCartItemRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un producto valido.")]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
    public int Quantity { get; set; } = 1;
}

/// <summary>Cambio de cantidad de una linea del carrito.</summary>
public class UpdateCartItemRequest
{
    [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
    public int Quantity { get; set; }
}
