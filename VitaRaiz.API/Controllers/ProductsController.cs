using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Products;
using VitaRaiz.Application.Queries.Products;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener todos los productos con filtros opcionales
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            Console.WriteLine($"[ProductsController] GetProducts called by {username}, Params: searchTerm={searchTerm}, isActive={isActive}");

            var query = new GetProductsQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive
            };

            var products = await _mediator.Send(query);
            Console.WriteLine($"[ProductsController] Returning {products?.Count ?? 0} products");
            return Ok(products);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProductsController] ERROR: {ex.Message}");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Obtener un producto por ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var query = new GetProductByIdQuery { ProductId = id };
        var product = await _mediator.Send(query);

        if (product == null)
            return NotFound(new { message = "Producto no encontrado" });

        return Ok(product);
    }

    /// <summary>
    /// Crear un nuevo producto
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "AdminFull,Admin,Supervisor")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var productId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProductById), new { id = productId }, new { productId });
    }

    /// <summary>
    /// Actualizar un producto existente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "AdminFull,Admin,Supervisor")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.ProductId)
            return BadRequest(new { message = "El ID del producto no coincide" });

        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Producto no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Eliminar un producto
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var command = new DeleteProductCommand { ProductId = id };
        var result = await _mediator.Send(command);

        if (!result)
            return NotFound(new { message = "Producto no encontrado" });

        return NoContent();
    }
}
