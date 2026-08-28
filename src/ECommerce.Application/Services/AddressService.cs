using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Mapping;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

/// <summary>Libreta de direcciones del usuario autenticado.</summary>
public class AddressService : IAddressService
{
    private readonly IApplicationDbContext _context;

    public AddressService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AddressDto>> GetMineAsync(int userId, CancellationToken ct = default)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Alias)
            .ToListAsync(ct);

        return addresses.Select(a => a.ToDto()).ToList();
    }

    public async Task<AddressDto> GetByIdAsync(int id, int userId, CancellationToken ct = default)
    {
        var address = await _context.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Dirección", id);

        return address.ToDto();
    }

    public async Task<AddressDto> CreateAsync(int userId, SaveAddressRequest request, CancellationToken ct = default)
    {
        var isFirstAddress = !await _context.Addresses.AnyAsync(a => a.UserId == userId, ct);

        var address = new Address
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        Apply(request, address);

        // La primera dirección siempre queda como predeterminada.
        address.IsDefault = request.IsDefault || isFirstAddress;

        _context.Addresses.Add(address);

        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, 0, ct);
        }

        await _context.SaveChangesAsync(ct);

        return address.ToDto();
    }

    public async Task<AddressDto> UpdateAsync(
        int id,
        int userId,
        SaveAddressRequest request,
        CancellationToken ct = default)
    {
        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Dirección", id);

        Apply(request, address);
        address.IsDefault = request.IsDefault || address.IsDefault;
        address.UpdatedAt = DateTime.UtcNow;

        if (address.IsDefault)
        {
            await ClearOtherDefaultsAsync(userId, address.Id, ct);
        }

        await _context.SaveChangesAsync(ct);

        return address.ToDto();
    }

    public async Task DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException("Dirección", id);

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(ct);

        if (!address.IsDefault)
        {
            return;
        }

        // Si se elimina la predeterminada se promueve otra automáticamente.
        var replacement = await _context.Addresses
            .Where(a => a.UserId == userId && a.Id != id)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (replacement is not null)
        {
            replacement.IsDefault = true;
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task ClearOtherDefaultsAsync(int userId, int currentAddressId, CancellationToken ct)
    {
        var others = await _context.Addresses
            .Where(a => a.UserId == userId && a.IsDefault && a.Id != currentAddressId)
            .ToListAsync(ct);

        foreach (var other in others)
        {
            other.IsDefault = false;
        }
    }

    private static void Apply(SaveAddressRequest request, Address address)
    {
        address.Alias = request.Alias.Trim();
        address.RecipientName = request.RecipientName.Trim();
        address.Street = request.Street.Trim();
        address.Street2 = string.IsNullOrWhiteSpace(request.Street2) ? null : request.Street2.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country = request.Country.Trim();
        address.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
    }
}
