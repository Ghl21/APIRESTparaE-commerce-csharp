using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>Reseña de un producto realizada por un usuario.</summary>
public class Review : BaseEntity
{
    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public bool IsApproved { get; set; } = true;
}
