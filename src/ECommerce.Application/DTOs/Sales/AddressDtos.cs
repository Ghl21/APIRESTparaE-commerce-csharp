using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Sales;

/// <summary>Dirección del usuario.</summary>
public class AddressDto
{
    public int Id { get; set; }

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

/// <summary>Datos para crear o actualizar una dirección.</summary>
public class SaveAddressRequest
{
    [Required(ErrorMessage = "El alias es obligatorio.")]
    [StringLength(60)]
    public string Alias { get; set; } = string.Empty;

    [Required(ErrorMessage = "El destinatario es obligatorio.")]
    [StringLength(150)]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La calle es obligatoria.")]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Street2 { get; set; }

    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "El estado o provincia es obligatorio.")]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código postal es obligatorio.")]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "El país es obligatorio.")]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El formato del teléfono no es válido.")]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    public bool IsDefault { get; set; }
}
