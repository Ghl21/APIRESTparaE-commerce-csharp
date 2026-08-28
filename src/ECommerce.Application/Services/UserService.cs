using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

/// <summary>Operaciones administrativas sobre usuarios y sus roles.</summary>
public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;

    public UserService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        query = parameters.SortBy?.ToLowerInvariant() switch
        {
            "email" => parameters.SortDescending
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),
            "name" => parameters.SortDescending
                ? query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
                : query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
            _ => parameters.SortDescending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var users = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserDto>(
            users.Select(u => u.ToDto()).ToList(),
            total,
            parameters.PageNumber,
            parameters.PageSize);
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario", id);

        return user.ToDto();
    }

    public async Task<UserDto> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario", id);

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!isActive)
        {
            // Un usuario desactivado no debe poder renovar su sesión.
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && rt.RevokedAt == null)
                .ToListAsync(ct);

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task<UserDto> AssignRoleAsync(int id, string roleName, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario", id);

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct)
            ?? throw new NotFoundException($"No existe el rol {roleName}.");

        if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
        {
            throw new ConflictException($"El usuario ya tiene asignado el rol {roleName}.");
        }

        user.UserRoles.Add(new Domain.Entities.UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role,
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task<UserDto> RemoveRoleAsync(int id, string roleName, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario", id);

        var userRole = user.UserRoles.FirstOrDefault(ur => ur.Role.Name == roleName)
            ?? throw new NotFoundException($"El usuario no tiene asignado el rol {roleName}.");

        if (user.UserRoles.Count == 1)
        {
            throw new BusinessRuleException("El usuario debe conservar al menos un rol.");
        }

        _context.UserRoles.Remove(userRole);
        user.UserRoles.Remove(userRole);

        await _context.SaveChangesAsync(ct);

        return user.ToDto();
    }
}
