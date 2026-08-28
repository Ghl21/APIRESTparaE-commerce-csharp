using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Pedidos: creación desde el carrito y seguimiento.</summary>
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;

    public OrdersController(IOrderService orderService, IPaymentService paymentService)
    {
        _orderService = orderService;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Lista pedidos. Un cliente sólo ve los suyos; un administrador ve todos
    /// y puede filtrar por usuario con el parámetro userId.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> GetAll([FromQuery] OrderQueryParameters parameters)
    {
        int? userFilter = IsAdmin ? null : UserId;

        return Ok(await _orderService.GetAllAsync(parameters, userFilter, RequestAborted));
    }

    /// <summary>Obtiene el detalle de un pedido.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id) =>
        Ok(await _orderService.GetByIdAsync(id, UserId, IsAdmin, RequestAborted));

    /// <summary>
    /// Confirma la compra: convierte el carrito en un pedido, descuenta el stock
    /// y deja el pedido en estado Pending a la espera del pago.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateFromCartAsync(UserId, request, RequestAborted);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Cambia el estado de un pedido respetando las transiciones permitidas.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request) =>
        Ok(await _orderService.UpdateStatusAsync(id, request.Status, RequestAborted));

    /// <summary>Cancela un pedido y devuelve las unidades al inventario.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Cancel(int id) =>
        Ok(await _orderService.CancelAsync(id, UserId, IsAdmin, RequestAborted));

    /// <summary>Lista los pagos registrados sobre un pedido.</summary>
    [HttpGet("{id:int}/payments")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetPayments(int id) =>
        Ok(await _paymentService.GetByOrderAsync(id, UserId, IsAdmin, RequestAborted));
}
