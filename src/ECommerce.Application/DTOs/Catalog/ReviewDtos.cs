using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Catalog;

/// <summary>Reseña de producto.</summary>
public class ReviewDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Datos para crear o actualizar una reseña.</summary>
public class CreateReviewRequest
{
    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    public int Rating { get; set; }

    [StringLength(150)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
