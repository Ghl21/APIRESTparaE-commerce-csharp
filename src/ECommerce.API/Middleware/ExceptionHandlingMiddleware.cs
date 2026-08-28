using System.Net;
using System.Text.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Middleware;

/// <summary>
/// Traduce las excepciones de dominio a códigos HTTP y devuelve siempre el mismo
/// formato de error, sin exponer detalles internos al cliente.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Recurso no encontrado"),
            ConflictException => (HttpStatusCode.Conflict, "Conflicto"),
            BusinessRuleException => (HttpStatusCode.BadRequest, "Regla de negocio"),
            AuthenticationException => (HttpStatusCode.Unauthorized, "No autenticado"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Acceso denegado"),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Conflicto de concurrencia"),
            DbUpdateException => (HttpStatusCode.BadRequest, "Error al guardar los datos"),
            OperationCanceledException => ((HttpStatusCode)499, "Petición cancelada"),
            _ => (HttpStatusCode.InternalServerError, "Error interno del servidor")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("{Title}: {Message}", title, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        var response = new ApiErrorResponse
        {
            StatusCode = (int)status,
            Title = title,
            TraceId = context.TraceIdentifier,
            Message = status == HttpStatusCode.InternalServerError && !_environment.IsDevelopment()
                ? "Ocurrió un error inesperado. Contacte al administrador e indique el TraceId."
                : exception.Message
        };

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(payload);
    }
}

/// <summary>Extensión para registrar el middleware en el pipeline.</summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
