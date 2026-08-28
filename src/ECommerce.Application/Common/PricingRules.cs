namespace ECommerce.Application.Common;

/// <summary>
/// Reglas de negocio de precios usadas en el carrito y en el checkout.
/// Centralizadas aquí para que el cálculo estimado y el definitivo nunca difieran.
/// </summary>
public static class PricingRules
{
    /// <summary>Impuesto aplicado sobre el subtotal (16%).</summary>
    public const decimal TaxRate = 0.16m;

    /// <summary>Costo fijo de envío.</summary>
    public const decimal StandardShippingCost = 99.00m;

    /// <summary>A partir de este subtotal el envío es gratuito.</summary>
    public const decimal FreeShippingThreshold = 1500.00m;

    public static decimal CalculateTax(decimal subTotal) => Round(subTotal * TaxRate);

    public static decimal CalculateShipping(decimal subTotal) =>
        subTotal <= 0m || subTotal >= FreeShippingThreshold ? 0m : StandardShippingCost;

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
