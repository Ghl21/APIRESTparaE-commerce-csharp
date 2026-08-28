using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ECommerce.API.Extensions;
using ECommerce.API.Middleware;
using ECommerce.API.Services;
using ECommerce.Application;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Seed;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// Servicios
// ----------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddValidationResponse();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddHealthChecks();

// Límite de peticiones por IP para mitigar abusos y ataques de fuerza bruta.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 120),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:WindowMinutes", 1)),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

var app = builder.Build();

// ----------------------------------------------------------------------------
// Pipeline
// ----------------------------------------------------------------------------
app.UseExceptionHandling();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API REST para E-commerce v1");
        options.DocumentTitle = "API REST para E-commerce";
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(ServiceCollectionExtensions.CorsPolicy);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// ----------------------------------------------------------------------------
// Siembra inicial (roles y administrador) controlada por configuración
// ----------------------------------------------------------------------------
if (app.Configuration.GetValue("Seed:Enabled", true))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        // La API debe poder arrancar aunque la base todavía no exista.
        logger.LogError(ex, "Falló la siembra inicial de datos.");
    }
}

app.Run();

/// <summary>Punto de entrada expuesto para las pruebas de integración.</summary>
public partial class Program
{
}
