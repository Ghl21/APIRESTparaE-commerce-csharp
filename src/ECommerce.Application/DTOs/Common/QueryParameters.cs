using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Common;

/// <summary>Parámetros base de paginación y ordenamiento.</summary>
public class QueryParameters
{
    private const int MaxPageSize = 100;

    private int _pageSize = 10;

    private int _pageNumber = 1;

    /// <summary>Número de página (base 1).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor o igual a 1.")]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>Cantidad de registros por página (máximo 100).</summary>
    [Range(1, MaxPageSize, ErrorMessage = "El tamaño de página debe estar entre 1 y 100.")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    /// <summary>Campo por el cual ordenar. Ej: "price", "name".</summary>
    public string? SortBy { get; set; }

    /// <summary>true para orden descendente.</summary>
    public bool SortDescending { get; set; }
}
