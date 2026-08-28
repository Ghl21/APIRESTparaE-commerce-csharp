using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Catalog;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.v1;

/// <summary>Categorías del catálogo. La consulta es pública y la escritura requiere rol Admin.</summary>
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Lista las categorías. Sólo un administrador puede incluir las inactivas.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var include = includeInactive && IsAdmin;

        return Ok(await _categoryService.GetAllAsync(include, RequestAborted));
    }

    /// <summary>Obtiene una categoría por su identificador.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(int id) =>
        Ok(await _categoryService.GetByIdAsync(id, RequestAborted));

    /// <summary>Crea una categoría.</summary>
    [HttpPost]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request)
    {
        var category = await _categoryService.CreateAsync(request, RequestAborted);

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>Actualiza una categoría existente.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] UpdateCategoryRequest request) =>
        Ok(await _categoryService.UpdateAsync(id, request, RequestAborted));

    /// <summary>Elimina una categoría sin productos ni subcategorías asociadas.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ServiceCollectionExtensions.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoryService.DeleteAsync(id, RequestAborted);

        return NoContent();
    }
}
