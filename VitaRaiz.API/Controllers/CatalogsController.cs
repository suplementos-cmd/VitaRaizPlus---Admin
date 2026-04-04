using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Domain.Entities;

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
    /// Obtiene todos los catálogos en una sola llamada.
    /// Si se llama con token Bearer válido, el tema retornado es el del rol del usuario.
    /// Si se llama anónimamente, retorna el tema global por defecto.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllCatalogs()
    {
        try
        {
            _logger.LogInformation("[CatalogsController] GetAllCatalogs called");

            var saleStatuses    = await _catalogRepository.GetSaleStatusesAsync();
            var paymentStatuses = await _catalogRepository.GetPaymentStatusesAsync();
            var riskStatuses    = await _catalogRepository.GetRiskStatusesAsync();
            var visitActions    = await _catalogRepository.GetVisitActionsAsync();
            var settings        = await _catalogRepository.GetAppSettingsAsync(null, true);

            // Tema contextual: si el token tiene roleId, usar el tema del perfil
            CatalogAppTheme? theme = null;
            int? roleId = ExtractRoleId();

            if (roleId.HasValue && roleId.Value > 0)
            {
                var profileTheme = await _catalogRepository.GetThemeByRoleAsync(roleId.Value);
                if (profileTheme != null)
                {
                    theme = new CatalogAppTheme
                    {
                        ThemeCode       = $"ROLE_{roleId}",
                        ThemeName       = profileTheme.ThemeName,
                        PrimaryColor    = profileTheme.PrimaryColor,
                        SecondaryColor  = profileTheme.SecondaryColor,
                        AccentColor     = profileTheme.AccentColor,
                        BackgroundColor = profileTheme.BackgroundColor,
                        TextColor       = profileTheme.TextColor
                    };
                    _logger.LogInformation("[CatalogsController] Role theme applied: {ThemeName} for roleId={RoleId}", theme.ThemeName, roleId);
                }
            }

            theme ??= await _catalogRepository.GetActiveThemeAsync();

            return Ok(new
            {
                saleStatuses,
                paymentStatuses,
                riskStatuses,
                theme,
                visitActions,
                settings = settings.ToDictionary(s => s.SettingKey, s => new
                {
                    value       = s.SettingValue,
                    type        = s.SettingType,
                    description = s.Description
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CatalogsController] Error loading catalogs");
            return StatusCode(500, new { error = "Error al cargar catálogos", details = ex.Message });
        }
    }

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
    /// Obtiene el tema específico configurado para un rol. Requiere autenticación.
    /// </summary>
    /// <summary>
    /// Guarda (insert o update) el tema de colores de un rol.
    /// </summary>
    [HttpPut("theme/role/{roleId:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> SaveThemeByRole(int roleId, [FromBody] ProfileThemeDto dto)
    {
        try
        {
            var theme = new VitaRaiz.Domain.Entities.ProfileTheme
            {
                RoleId          = roleId,
                ThemeName       = dto.ThemeName,
                PrimaryColor    = dto.PrimaryColor,
                SecondaryColor  = dto.SecondaryColor,
                AccentColor     = dto.AccentColor,
                BackgroundColor = dto.BackgroundColor,
                TextColor       = dto.TextColor,
                TitleTextColor  = dto.TitleTextColor,
                FormTextColor   = dto.FormTextColor,
                MenuTextColor   = dto.MenuTextColor,
                IconName        = dto.IconName
            };
            await _catalogRepository.SaveThemeByRoleAsync(theme);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CatalogsController] Error saving theme for role {RoleId}", roleId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("theme/role/{roleId:int}")]
    public async Task<IActionResult> GetThemeByRole(int roleId)
    {
        try
        {
            if (roleId <= 0)
                return BadRequest(new { error = "roleId debe ser un número positivo" });

            var theme = await _catalogRepository.GetThemeByRoleAsync(roleId);
            if (theme == null)
                return NotFound(new { message = $"No hay tema configurado para el rol {roleId}" });

            return Ok(new ProfileThemeDto
            {
                ThemeId         = theme.ThemeId,
                RoleId          = theme.RoleId,
                ThemeName       = theme.ThemeName,
                RoleName        = theme.RoleName,
                PrimaryColor    = theme.PrimaryColor,
                SecondaryColor  = theme.SecondaryColor,
                AccentColor     = theme.AccentColor,
                BackgroundColor = theme.BackgroundColor,
                TextColor       = theme.TextColor,
                TitleTextColor  = theme.TitleTextColor,
                FormTextColor   = theme.FormTextColor,
                MenuTextColor   = theme.MenuTextColor,
                IconName        = theme.IconName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

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

    [HttpGet("settings")]
    public async Task<IActionResult> GetAppSettings(
        [FromQuery] string? category = null,
        [FromQuery] bool publicOnly = true)
    {
        try
        {
            var settings = await _catalogRepository.GetAppSettingsAsync(category, publicOnly);
            var settingsDict = settings.ToDictionary(
                s => s.SettingKey,
                s => new
                {
                    value       = s.SettingValue,
                    type        = s.SettingType,
                    description = s.Description,
                    category    = s.Category
                }
            );
            return Ok(settingsDict);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

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

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Extrae roleId del claim del token JWT, si está presente.</summary>
    private int? ExtractRoleId()
    {
        var claim = User?.FindFirst("roleId")?.Value;
        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var rid) && rid > 0)
            return rid;
        return null;
    }
}
