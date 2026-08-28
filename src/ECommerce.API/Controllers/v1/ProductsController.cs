using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Catálogo de productos: búsqueda pública y mantenimiento para administradores.</summary>
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;

    public ProductsController(IProductService productService, IReviewService reviewService)
    {
        _productService = productService;
        _reviewService = reviewService;
    }

    /// <summary>
    /// Lista productos con paginación, filtros y ordenamiento.
    /// Ejemplo: /api/v1/products?search=laptop&amp;categoryId=2&amp;minPrice=100&amp;sortBy=price&amp;sortDescending=true
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetAll(
        [FromQuery] ProductQueryParameters parameters)
    {
        // Sólo un administrador puede consultar productos desactivados.
        if (!IsAdmin)
        {
            parameters.IsActive = true;
        }

        return Ok(await _productService.GetAllAsync(parameters, RequestAborted));
    }

    /// <summary>Obtiene el detalle de un producto por su identificador.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id) =>
        Ok(await _productService.GetByIdAsync(id, RequestAborted));

    /// <summary>Obtiene el detalle de un producto por su slug.</summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetBySlug(string slug) =>
        Ok(await _productService.GetBySlugAsync(slug, RequestAborted));

    /// <summary>Crea un producto.</summary>
    [HttpPost]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request, RequestAborted);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Actualiza un producto existente, incluida su galería de imágenes.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request) =>
        Ok(await _productService.UpdateAsync(id, request, RequestAborted));

    /// <summary>Ajusta las existencias de un producto.</summary>
    [HttpPatch("{id:int}/stock")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateStock(int id, [FromBody] UpdateStockRequest request) =>
        Ok(await _productService.UpdateStockAsync(id, request.Stock, RequestAborted));

    /// <summary>
    /// Elimina un producto. Si ya fue vendido se desactiva en lugar de borrarse
    /// para conservar el histórico de pedidos.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id, RequestAborted);

        return NoContent();
    }

    /// <summary>Lista las reseñas aprobadas de un producto.</summary>
    [HttpGet("{id:int}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetReviews(
        int id,
        [FromQuery] QueryParameters parameters) =>
        Ok(await _reviewService.GetByProductAsync(id, parameters, RequestAborted));

    /// <summary>Crea o actualiza la reseña del usuario autenticado sobre un producto.</summary>
    [HttpPost("{id:int}/reviews")]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> UpsertReview(int id, [FromBody] CreateReviewRequest request) =>
        Ok(await _reviewService.CreateOrUpdateAsync(id, UserId, request, RequestAborted));
}
