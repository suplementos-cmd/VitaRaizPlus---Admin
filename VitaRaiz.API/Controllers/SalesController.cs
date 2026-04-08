using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using VitaRaiz.Application.Commands.Sales;
using VitaRaiz.Application.Queries.Sales;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SalesController> _logger;
    private readonly ISalePhotoRepository _salePhotoRepository;
    private readonly IWebHostEnvironment _env;
    private readonly string _storageBasePath;
    private readonly string _salesFolder;

    // Mapeo de tipos "amigables" al valor que acepta CHK_PHOTO_TYPE en Oracle
    private static readonly Dictionary<string, string> _photoTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["FACHADA"]      = "FOTO_FACHADA",
            ["FOTO_FACHADA"] = "FOTO_FACHADA",
            ["CLIENTE"]      = "FOTO_CLIENTE",
            ["FOTO_CLIENTE"] = "FOTO_CLIENTE",
            ["CONTRATO"]     = "FOTO_CONTRATO",
            ["FOTO_CONTRATO"]= "FOTO_CONTRATO",
            ["ADICIONAL"]    = "FOTO_ADD_1",
            ["ADD_1"]        = "FOTO_ADD_1",
            ["FOTO_ADD_1"]   = "FOTO_ADD_1",
            ["ADD_2"]        = "FOTO_ADD_2",
            ["FOTO_ADD_2"]   = "FOTO_ADD_2",
        };

    public SalesController(IMediator mediator, ILogger<SalesController> logger,
        ISalePhotoRepository salePhotoRepository, IWebHostEnvironment env,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _salePhotoRepository = salePhotoRepository;
        _env = env;
        _storageBasePath = configuration["FileStorage:BasePath"] ?? Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        _salesFolder     = configuration["FileStorage:SalesFolder"] ?? "Ventas";
    }

    /// <summary>
    /// Obtener todas las ventas con filtros opcionales
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] int? customerId = null,
        [FromQuery] int? sellerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var username = User.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("[SalesController] GetSales called by {Username}, Auth: {Auth}, Params: customerId={CustomerId}, sellerId={SellerId}, status={Status}", 
                username, authHeader.Substring(0, Math.Min(20, authHeader.Length)), customerId, sellerId, status);

            var query = new GetSalesQuery
            {
                CustomerId = customerId,
                SellerId = sellerId,
                StartDate = startDate,
                EndDate = endDate,
                Status = status
            };

            var sales = await _mediator.Send(query);
            
            _logger.LogInformation("[SalesController] Returning {Count} sales", sales?.Count ?? 0);
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error in GetSales");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// DEBUG: Obtener conteo de ventas directamente de la tabla
    /// </summary>
    [HttpGet("debug/count")]
    public async Task<IActionResult> GetSalesCount()
    {
        try
        {
            var query = new GetSalesQuery();
            var sales = await _mediator.Send(query);
            return Ok(new { totalSales = sales?.Count ?? 0, sales = sales });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Obtener ventas activas (con saldo pendiente)
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSales([FromQuery] int? collectorId = null)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            _logger.LogInformation("[SalesController] GetActiveSales called by {Username}, collectorId={CollectorId}", username, collectorId);
            
            var query = new GetActiveSalesQuery { CollectorId = collectorId };
            var sales = await _mediator.Send(query);
            
            _logger.LogInformation("[SalesController] GetActiveSales returning {Count} sales", sales?.Count ?? 0);
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error in GetActiveSales");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Obtener una venta por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSaleById(int id)
    {
        var query = new GetSaleByIdQuery { SaleId = id };
        var sale = await _mediator.Send(query);

        if (sale == null)
            return NotFound(new { message = "Venta no encontrada" });

        return Ok(sale);
    }

    /// <summary>
    /// Obtener venta completa con items, pagos, cliente y métricas de riesgo.
    /// Endpoint pivote para la pantalla "Gestión de Cartera".
    /// </summary>
    [HttpGet("{id}/full")]
    public async Task<IActionResult> GetSaleFull(int id)
    {
        try
        {
            var query = new GetSaleFullQuery { SaleId = id };
            var sale = await _mediator.Send(query);

            if (sale == null)
                return NotFound(new { message = "Venta no encontrada" });

            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error in GetSaleFull for saleId={SaleId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener el balance pendiente de una venta
    /// </summary>
    [HttpGet("{saleId}/balance")]
    public async Task<ActionResult<decimal>> GetSaleBalance(int saleId)
    {
        try
        {
            var query = new GetSaleBalanceQuery(saleId);
            var balance = await _mediator.Send(query);
            return Ok(new { saleId, balance });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crear una nueva venta con detalles
    /// </summary>
    [HttpPost]
    [Authorize] // TEMPORAL: Permite cualquier usuario autenticado crear ventas
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleCommand command)
    {
        try
        {
            var saleId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetSaleById), new { id = saleId }, new { saleId, message = "Venta creada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar datos de una venta y su cliente
    /// </summary>
    [HttpPut("{id}")]
    [Authorize] // TEMPORAL: Permite cualquier usuario autenticado actualizar ventas
    public async Task<IActionResult> UpdateSale(int id, [FromBody] UpdateSaleCommand command)
    {
        command.SaleId = id; // route param is authoritative

        try
        {
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { message = "Venta no encontrada" });

            return Ok(new { message = "Venta actualizada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error updating sale {SaleId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancelar una venta
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Supervisor,AdminFull,Admin")]
    public async Task<IActionResult> CancelSale(int id, [FromBody] CancelSaleCommand command)
    {
        if (id != command.SaleId)
            return BadRequest(new { message = "El ID de la venta no coincide" });

        try
        {
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { message = "Venta no encontrada" });

            return Ok(new { message = "Venta cancelada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener las fotos de una venta
    /// </summary>
    [HttpGet("{saleId}/photos")]
    public async Task<IActionResult> GetSalePhotos(int saleId)
    {
        try
        {
            var photos = await _salePhotoRepository.GetSalePhotosAsync(saleId);
            return Ok(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error getting photos for sale {SaleId}", saleId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Subir archivo de foto de venta desde el portal web (multipart)
    /// </summary>
    [HttpPost("{saleId}/photos/upload")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadSalePhotoFile(int saleId,
        [FromForm] IFormFile file,
        [FromForm] string photoType)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo requerido" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest(new { message = "Solo jpg, png o webp" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "El archivo no debe superar 10 MB" });

        // Normalizar tipo al formato aceptado por CHK_PHOTO_TYPE de Oracle
        if (!_photoTypeMap.TryGetValue(photoType, out var normalizedType))
            return BadRequest(new { message = $"Tipo de foto no válido: {photoType}. Use FACHADA, CLIENTE, CONTRATO, ADICIONAL." });

        // Guardar en ruta centralizada: {BasePath}\Ventas\{saleId}\
        var dir = Path.Combine(_storageBasePath, _salesFolder, saleId.ToString());
        Directory.CreateDirectory(dir);

        var fileName = $"{normalizedType}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        var absolutePath = Path.Combine(dir, fileName);

        using (var stream = new FileStream(absolutePath, FileMode.Create))
            await file.CopyToAsync(stream);

        // Ruta relativa al BasePath (sin separador inicial)
        var relativeStoragePath = Path.Combine(_salesFolder, saleId.ToString(), fileName);

        var userId = User.FindFirst("user_id")?.Value;
        int.TryParse(userId, out int uploadedBy);

        var photoId = await _salePhotoRepository.AddSalePhotoAsync(
            saleId, normalizedType, relativeStoragePath,
            null, null, file.Length,
            uploadedBy > 0 ? uploadedBy : null);

        _logger.LogInformation("[SalesController] Photo {Type} uploaded for sale {Id}: {Path}", normalizedType, saleId, absolutePath);
        return Ok(new { photoId, filePath = relativeStoragePath });
    }

    /// <summary>
    /// Agregar una foto a una venta
    /// </summary>
    [HttpPost("{saleId}/photos")]
    [Authorize(Roles = "Vendedor,Supervisor,AdminFull,Admin")]
    public async Task<IActionResult> AddSalePhoto(int saleId, [FromBody] AddSalePhotoRequest request)
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            int.TryParse(userId, out int uploadedBy);

            var photoId = await _salePhotoRepository.AddSalePhotoAsync(
                saleId,
                request.PhotoType,
                request.FilePath,
                request.GpsLatitude,
                request.GpsLongitude,
                request.FileSize,
                uploadedBy > 0 ? uploadedBy : null
            );

            return Ok(new { photoId, message = "Foto agregada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error adding photo to sale {SaleId}", saleId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Eliminar una foto de venta
    /// </summary>
    [HttpDelete("{saleId}/photos/{photoId}")]
    [Authorize(Roles = "Vendedor,Supervisor,AdminFull,Admin")]
    public async Task<IActionResult> DeleteSalePhoto(int saleId, int photoId)
    {
        try
        {
            await _salePhotoRepository.DeleteSalePhotoAsync(photoId);
            return Ok(new { message = "Foto eliminada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error deleting photo {PhotoId}", photoId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Servir el archivo binario de una foto de venta directamente (para el portal web)
    /// </summary>
    [HttpGet("photos/{photoId}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSalePhotoFile(int photoId)
    {
        try
        {
            var photo = await _salePhotoRepository.GetPhotoByIdAsync(photoId);
            if (photo == null)
                return NotFound(new { message = "Foto no encontrada" });

            // Resolver ruta absoluta desde el BasePath centralizado.
            // Soporta rutas relativas nuevas (Ventas/10101/FACHADA_xxx.jpg)
            // y rutas legadas (/uploads/sales/... para compatibilidad).
            string absolutePath;
            var stored = photo.FilePath;

            if (stored.StartsWith("/") || stored.StartsWith("\\")
                || (stored.Length > 2 && stored[1] == ':'))
            {
                // Ruta absoluta (legado móvil: G:\AppData\... o /uploads/...)
                if (stored.StartsWith("/uploads/") || stored.StartsWith("uploads/"))
                {
                    var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                    var rel = stored.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    absolutePath = Path.GetFullPath(Path.Combine(webRoot, rel));
                    if (!absolutePath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new { message = "Ruta no válida" });
                }
                else
                {
                    // Ruta absoluta del dispositivo móvil — archivo no disponible en servidor
                    return NotFound(new { message = "Foto tomada desde dispositivo móvil, no disponible en servidor" });
                }
            }
            else
            {
                // Ruta relativa al BasePath centralizado (Ventas/{saleId}/archivo.jpg)
                var rel = stored.Replace('/', Path.DirectorySeparatorChar);
                absolutePath = Path.GetFullPath(Path.Combine(_storageBasePath, rel));
                if (!absolutePath.StartsWith(_storageBasePath, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Ruta no válida" });
            }

            if (!System.IO.File.Exists(absolutePath))
                return NotFound(new { message = "Archivo no encontrado en el servidor" });

            var ext = Path.GetExtension(photo.FilePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".gif"            => "image/gif",
                ".webp"           => "image/webp",
                _                 => "application/octet-stream"
            };

            var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SalesController] Error serving photo file {PhotoId}", photoId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class AddSalePhotoRequest
{
    public string PhotoType { get; set; } = string.Empty; // FACHADA, CLIENTE, CONTRATO, ADICIONAL
    public string FilePath { get; set; } = string.Empty;
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public long? FileSize { get; set; }
}
