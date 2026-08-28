using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Interfaces;

/// <summary>Registro, autenticación y gestión de sesión.</summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);

    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);

    Task<UserDto> GetProfileAsync(int userId, CancellationToken ct = default);
}

/// <summary>Administración de usuarios y roles.</summary>
public interface IUserService
{
    Task<PagedResult<UserDto>> GetAllAsync(QueryParameters parameters, CancellationToken ct = default);

    Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<UserDto> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);

    Task<UserDto> AssignRoleAsync(int id, string roleName, CancellationToken ct = default);

    Task<UserDto> RemoveRoleAsync(int id, string roleName, CancellationToken ct = default);
}

/// <summary>Gestión de categorías del catálogo.</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);

    Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);

    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>Gestión del catálogo de productos.</summary>
public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetAllAsync(ProductQueryParameters parameters, CancellationToken ct = default);

    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ProductDto> GetBySlugAsync(string slug, CancellationToken ct = default);

    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);

    Task<ProductDto> UpdateStockAsync(int id, int stock, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>Reseñas de productos.</summary>
public interface IReviewService
{
    Task<PagedResult<ReviewDto>> GetByProductAsync(int productId, QueryParameters parameters, CancellationToken ct = default);

    Task<ReviewDto> CreateOrUpdateAsync(int productId, int userId, CreateReviewRequest request, CancellationToken ct = default);

    Task DeleteAsync(int reviewId, int userId, bool isAdmin, CancellationToken ct = default);
}

/// <summary>Direcciones del usuario.</summary>
public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetMineAsync(int userId, CancellationToken ct = default);

    Task<AddressDto> GetByIdAsync(int id, int userId, CancellationToken ct = default);

    Task<AddressDto> CreateAsync(int userId, SaveAddressRequest request, CancellationToken ct = default);

    Task<AddressDto> UpdateAsync(int id, int userId, SaveAddressRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, int userId, CancellationToken ct = default);
}

/// <summary>Carrito de compras.</summary>
public interface ICartService
{
    Task<CartDto> GetMineAsync(int userId, CancellationToken ct = default);

    Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken ct = default);

    Task<CartDto> UpdateItemAsync(int userId, int itemId, UpdateCartItemRequest request, CancellationToken ct = default);

    Task<CartDto> RemoveItemAsync(int userId, int itemId, CancellationToken ct = default);

    Task ClearAsync(int userId, CancellationToken ct = default);
}

/// <summary>Pedidos.</summary>
public interface IOrderService
{
    Task<PagedResult<OrderListItemDto>> GetAllAsync(OrderQueryParameters parameters, int? userIdFilter, CancellationToken ct = default);

    Task<OrderDto> GetByIdAsync(int id, int userId, bool isAdmin, CancellationToken ct = default);

    Task<OrderDto> CreateFromCartAsync(int userId, CreateOrderRequest request, CancellationToken ct = default);

    Task<OrderDto> UpdateStatusAsync(int id, OrderStatus status, CancellationToken ct = default);

    Task<OrderDto> CancelAsync(int id, int userId, bool isAdmin, CancellationToken ct = default);
}

/// <summary>Pagos (pasarela simulada).</summary>
public interface IPaymentService
{
    Task<PaymentDto> ProcessAsync(int userId, bool isAdmin, CreatePaymentRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PaymentDto>> GetByOrderAsync(int orderId, int userId, bool isAdmin, CancellationToken ct = default);
}
