using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Commands.Products;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Application.Queries.Products;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IProductRepository _productRepository;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger,
        IWebHostEnvironment env, IProductRepository productRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _env = env;
        _productRepository = productRepository;
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
            _logger.LogInformation("[ProductsController] GetProducts called with searchTerm={SearchTerm}, isActive={IsActive}", searchTerm, isActive);
            
            var username = User.Identity?.Name ?? "Anonymous";
            // Already logged above

            var query = new GetProductsQuery
            {
                SearchTerm = searchTerm,
                IsActive = isActive
            };

            var products = await _mediator.Send(query);
            _logger.LogInformation("[ProductsController] Returning {Count} products", products?.Count ?? 0);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ProductsController] Error getting products");
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

    /// <summary>
    /// Subir/actualizar foto de producto
    /// </summary>
    [HttpPost("{id}/photo")]
    [Authorize(Roles = "AdminFull,Admin,Supervisor")]
    public async Task<IActionResult> UploadProductPhoto(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo requerido" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest(new { message = "Solo se permiten imágenes jpg, png o webp" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no debe superar 5 MB" });

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{id}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var relativeUrl = $"/uploads/products/{fileName}";
        await _productRepository.UpdateProductPhotoAsync(id, relativeUrl);

        _logger.LogInformation("[ProductsController] Photo updated for product {Id}: {Url}", id, relativeUrl);
        return Ok(new { photoUrl = relativeUrl });
    }
}
