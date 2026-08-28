using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Dirección de envío/facturación de un usuario.</summary>
public class Address : BaseEntity
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Alias { get; set; } = string.Empty;

    public string RecipientName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string? Street2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsDefault { get; set; }
}
