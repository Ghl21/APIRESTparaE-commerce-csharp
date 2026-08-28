using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ECommerce.Application.DTOs.Common;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace ECommerce.API.Extensions;

/// <summary>Configuración de autenticación, documentación y CORS de la API.</summary>
public static class ServiceCollectionExtensions
{
    public const string AdminPolicy = "AdminOnly";
    public const string CustomerPolicy = "CustomerOnly";
    public const string CorsPolicy = "DefaultCorsPolicy";

    /// <summary>Configura la autenticación por JWT Bearer y las políticas de autorización.</summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");

        if (string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "La clave 'Jwt:SecretKey' es obligatoria y debe tener al menos 32 caracteres.");
        }

        // Se desactiva el mapeo automático de claims para trabajar con los nombres estándar (sub, role).
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(settings.ClockSkewSeconds),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = JwtTokenService.RoleClaimType
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Respuesta uniforme también para los errores de autenticación.
                        context.HandleResponse();

                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json; charset=utf-8";

                        await context.Response.WriteAsync(Serialize(new ApiErrorResponse
                        {
                            StatusCode = StatusCodes.Status401Unauthorized,
                            Title = "No autenticado",
                            Message = "Debe iniciar sesión y enviar un token válido en la cabecera Authorization."
                        }));
                    },
                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json; charset=utf-8";

                        await context.Response.WriteAsync(Serialize(new ApiErrorResponse
                        {
                            StatusCode = StatusCodes.Status403Forbidden,
                            Title = "Acceso denegado",
                            Message = "No cuenta con los permisos necesarios para realizar esta operación."
                        }));
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy => policy.RequireRole(Role.Admin));
            options.AddPolicy(CustomerPolicy, policy => policy.RequireRole(Role.Customer, Role.Admin));
        });

        return services;
    }

    /// <summary>Configura Swagger con soporte para autenticación Bearer.</summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API REST para E-commerce",
                Version = "v1",
                Description =
                    "API REST de comercio electrónico construida con ASP.NET Core 8, Entity Framework Core, " +
                    "autenticación JWT y SQL Server. Incluye catálogo, carrito, pedidos y pagos.",
                Contact = new OpenApiContact { Name = "Equipo de desarrollo" }
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Autenticación JWT. Escriba únicamente el token, sin el prefijo Bearer.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [jwtScheme] = Array.Empty<string>()
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>Configura CORS a partir de los orígenes declarados en appsettings.</summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, policy =>
            {
                if (origins.Length == 0 || origins.Contains("*"))
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
            });
        });

        return services;
    }

    /// <summary>Unifica el formato de los errores de validación del modelo.</summary>
    public static IServiceCollection AddValidationResponse(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var response = new ApiErrorResponse
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Title = "Error de validación",
                    Message = "Uno o más campos enviados no son válidos.",
                    TraceId = context.HttpContext.TraceIdentifier,
                    Errors = errors
                };

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }

    private static string Serialize(ApiErrorResponse response) =>
        JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
}
