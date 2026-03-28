using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CatalogsController : ControllerBase
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly ILogger<CatalogsController> _logger;

    public CatalogsController(ICatalogRepository catalogRepository, ILogger<CatalogsController> logger)
    {
        _catalogRepository = catalogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los catálogos en una sola llamada
    /// Útil para la inicialización de la app móvil
    /// </summary>
    [AllowAnonymous]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllCatalogs()
    {
        try
        {
            _logger.LogInformation("[CatalogsController] GetAllCatalogs called");
            var saleStatuses = await _catalogRepository.GetSaleStatusesAsync();
            var paymentStatuses = await _catalogRepository.GetPaymentStatusesAsync();
            var riskStatuses = await _catalogRepository.GetRiskStatusesAsync();
            var theme = await _catalogRepository.GetActiveThemeAsync();
            var visitActions = await _catalogRepository.GetVisitActionsAsync();
            var settings = await _catalogRepository.GetAppSettingsAsync(null, true);

            return Ok(new
            {
                saleStatuses,
                paymentStatuses,
                riskStatuses,
                theme,
                visitActions,
                settings = settings.ToDictionary(s => s.SettingKey, s => new 
                { 
                    value = s.SettingValue, 
                    type = s.SettingType,
                    description = s.Description 
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error al cargar catálogos", details = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el catálogo de estados de venta
    /// </summary>
    [AllowAnonymous]
    [HttpGet("sale-statuses")]
    public async Task<IActionResult> GetSaleStatuses()
    {
        try
        {
            var statuses = await _catalogRepository.GetSaleStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el catálogo de estados de pago
    /// </summary>
    [AllowAnonymous]
    [HttpGet("payment-statuses")]
    public async Task<IActionResult> GetPaymentStatuses()
    {
        try
        {
            var statuses = await _catalogRepository.GetPaymentStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el catálogo de estados de riesgo
    /// </summary>
    [AllowAnonymous]
    [HttpGet("risk-statuses")]
    public async Task<IActionResult> GetRiskStatuses()
    {
        try
        {
            var statuses = await _catalogRepository.GetRiskStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el tema activo de la aplicación
    /// </summary>
    [AllowAnonymous]
    [HttpGet("theme")]
    public async Task<IActionResult> GetActiveTheme()
    {
        try
        {
            var theme = await _catalogRepository.GetActiveThemeAsync();
            
            if (theme == null)
                return NotFound(new { message = "No hay tema activo configurado" });

            return Ok(theme);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las plantillas de notificaciones
    /// </summary>
    [HttpGet("notification-templates")]
    public async Task<IActionResult> GetNotificationTemplates([FromQuery] string? templateType = null)
    {
        try
        {
            var templates = await _catalogRepository.GetNotificationTemplatesAsync(templateType);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las configuraciones de la aplicación
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetAppSettings(
        [FromQuery] string? category = null,
        [FromQuery] bool publicOnly = true)
    {
        try
        {
            var settings = await _catalogRepository.GetAppSettingsAsync(category, publicOnly);
            
            // Convertir a diccionario para facilitar el uso en la app
            var settingsDict = settings.ToDictionary(
                s => s.SettingKey,
                s => new 
                { 
                    value = s.SettingValue,
                    type = s.SettingType,
                    description = s.Description,
                    category = s.Category
                }
            );
            
            return Ok(settingsDict);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene un setting específico por su key
    /// </summary>
    [HttpGet("settings/{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        try
        {
            var value = await _catalogRepository.GetSettingValueAsync(key);
            
            if (value == null)
                return NotFound(new { message = $"Setting '{key}' no encontrado" });

            return Ok(new { key, value });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las acciones de visita disponibles
    /// </summary>
    [HttpGet("visit-actions")]
    public async Task<IActionResult> GetVisitActions()
    {
        try
        {
            var actions = await _catalogRepository.GetVisitActionsAsync();
            return Ok(actions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
