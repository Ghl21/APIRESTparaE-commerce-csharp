using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>Implementa el registro, inicio de sesión y rotación de tokens.</summary>
public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService,
        ICurrentUserService currentUser,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _context.Users.AnyAsync(u => u.Email == email, ct);
        if (emailTaken)
        {
            throw new ConflictException($"El correo {email} ya se encuentra registrado.");
        }

        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Role.Customer, ct)
            ?? throw new BusinessRuleException(
                "El rol Customer no existe en la base de datos. Ejecute los scripts de inicialización.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole { Role = customerRole, AssignedAt = DateTime.UtcNow });
        user.Cart = new Cart { CreatedAt = DateTime.UtcNow };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Usuario registrado con Id {UserId}", user.Id);

        return await BuildAuthResponseAsync(user, new[] { Role.Customer }, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        // Se usa el mismo mensaje para usuario inexistente y contraseña incorrecta
        // para no revelar qué correos están registrados.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Credenciales inválidas.");
        }

        if (!user.IsActive)
        {
            throw new AuthenticationException("La cuenta se encuentra desactivada. Contacte al administrador.");
        }

        user.LastLoginAt = DateTime.UtcNow;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var response = await BuildAuthResponseAsync(user, roles, ct);

        _logger.LogInformation("Inicio de sesión correcto para el usuario {UserId}", user.Id);

        return response;
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct)
            ?? throw new AuthenticationException("El refresh token no es válido.");

        if (!stored.IsActive)
        {
            throw new AuthenticationException("El refresh token expiró o fue revocado.");
        }

        if (!stored.User.IsActive)
        {
            throw new AuthenticationException("La cuenta se encuentra desactivada.");
        }

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Rotación: el token usado queda revocado y apunta al que lo reemplaza.
        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByToken = newRefreshToken;

        var roles = stored.User.UserRoles.Select(ur => ur.Role.Name).ToList();
        var (accessToken, expiresAt, expiresIn) = _tokenService.GenerateAccessToken(stored.User, roles);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = stored.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
            CreatedByIp = _currentUser.IpAddress,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        var userDto = stored.User.ToDto();
        userDto.Roles = roles.OrderBy(r => r).ToList();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            ExpiresInSeconds = expiresIn,
            User = userDto
        };
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken, ct)
            ?? throw new NotFoundException("El refresh token indicado no existe.");

        if (stored.IsRevoked)
        {
            return;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Usuario", userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthenticationException("La contraseña actual no es correcta.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Al cambiar la contraseña se invalidan todas las sesiones abiertas.
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("Usuario", userId);

        return user.ToDto();
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, IEnumerable<string> roles, CancellationToken ct)
    {
        var roleList = roles.ToList();
        var (accessToken, expiresAt, expiresIn) = _tokenService.GenerateAccessToken(user, roleList);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_tokenService.RefreshTokenExpirationDays),
            CreatedByIp = _currentUser.IpAddress,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        var dto = user.ToDto();
        dto.Roles = roleList.OrderBy(r => r).ToList();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            ExpiresInSeconds = expiresIn,
            User = dto
        };
    }
}
