namespace ECommerce.Domain.Enums;

/// <summary>Medios de pago soportados por la API.</summary>
public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    PayPal = 3,
    BankTransfer = 4,
    CashOnDelivery = 5
}
