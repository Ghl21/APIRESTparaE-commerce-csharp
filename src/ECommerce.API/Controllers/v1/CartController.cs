using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Carrito de compras del usuario autenticado.</summary>
[Authorize]
public class CartController : BaseApiController
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>Devuelve el carrito actual con los totales estimados.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> GetMine() =>
        Ok(await _cartService.GetMineAsync(UserId, RequestAborted));

    /// <summary>Agrega un producto al carrito o incrementa su cantidad.</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request) =>
        Ok(await _cartService.AddItemAsync(UserId, request, RequestAborted));

    /// <summary>Cambia la cantidad de una línea del carrito.</summary>
    [HttpPut("items/{itemId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateItem(int itemId, [FromBody] UpdateCartItemRequest request) =>
        Ok(await _cartService.UpdateItemAsync(UserId, itemId, request, RequestAborted));

    /// <summary>Quita una línea del carrito.</summary>
    [HttpDelete("items/{itemId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveItem(int itemId) =>
        Ok(await _cartService.RemoveItemAsync(UserId, itemId, RequestAborted));

    /// <summary>Vacía el carrito por completo.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearAsync(UserId, RequestAborted);

        return NoContent();
    }
}
