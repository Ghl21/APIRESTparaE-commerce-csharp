namespace ECommerce.Application.DTOs.Common;

/// <summary>Formato uniforme de error devuelto por la API.</summary>
public class ApiErrorResponse
{
    public int StatusCode { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? TraceId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public IDictionary<string, string[]>? Errors { get; set; }
}
