using ECommerce.Application.DTOs.Common;
using ECommerce.Application.DTOs.Sales;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>
/// Pagos de pedidos contra una pasarela simulada.
/// Nunca se almacenan datos completos de tarjeta: sólo los últimos cuatro dígitos.
/// </summary>
[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Procesa el pago de un pedido en estado Pending. Si se aprueba, el pedido pasa a Paid.
    /// Para pruebas: las tarjetas terminadas en 0000 se rechazan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentDto>> Process([FromBody] CreatePaymentRequest request)
    {
        var payment = await _paymentService.ProcessAsync(UserId, IsAdmin, request, RequestAborted);

        return StatusCode(StatusCodes.Status201Created, payment);
    }

    /// <summary>Lista los pagos de un pedido.</summary>
    [HttpGet("order/{orderId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetByOrder(int orderId) =>
        Ok(await _paymentService.GetByOrderAsync(orderId, UserId, IsAdmin, RequestAborted));
}
