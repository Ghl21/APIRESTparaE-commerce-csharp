using System.ComponentModel.DataAnnotations;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Sales;

/// <summary>Pago registrado sobre un pedido.</summary>
public class PaymentDto
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public string? CardLastFourDigits { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Solicitud de cobro de un pedido (pasarela simulada).</summary>
public class CreatePaymentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un pedido válido.")]
    public int OrderId { get; set; }

    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "El medio de pago no es válido.")]
    public PaymentMethod Method { get; set; }

    /// <summary>Número de tarjeta de prueba. Sólo se persisten los últimos cuatro dígitos.</summary>
    [CreditCard(ErrorMessage = "El número de tarjeta no es válido.")]
    public string? CardNumber { get; set; }

    [StringLength(150)]
    public string? CardHolderName { get; set; }

    [RegularExpression("^(0[1-9]|1[0-2])/[0-9]{2}$", ErrorMessage = "La fecha de expiración debe tener el formato MM/YY.")]
    public string? ExpirationDate { get; set; }

    [RegularExpression("^[0-9]{3,4}$", ErrorMessage = "El CVV debe tener 3 o 4 dígitos.")]
    public string? Cvv { get; set; }
}
