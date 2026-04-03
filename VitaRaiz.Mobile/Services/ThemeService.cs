using VitaRaiz.Mobile.Models;
using NLog;

namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Gestiona el tema visual de la aplicación basado en el perfil del usuario.
/// Prioridad: 1) Tema del rol desde API  2) Tema global desde API  3) Color por rol (hardcode fallback).
/// Los colores aplicados se persisten en SecureStorage para el arranque en frío.
/// </summary>
public class ThemeService
{
    // ── SecureStorage keys ────────────────────────────────────────────────
    private const string KeyThemePrimary  = "theme_primary_color";
    private const string KeyThemeLight    = "theme_light_color";
    private const string KeyThemeLighter  = "theme_lighter_color";
    private const string KeyThemeRole     = "theme_role";

    // ── Role-based fallback colors (aligned with PROFILE_THEMES seed data) ─
    private static readonly Dictionary<string, string> RoleFallbackColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "vendedor",         "#FF1744" },
        { "vendedora",        "#FF1744" },
        { "supervisor",       "#C62828" },
        { "supervisorajunior","#C62828" },
        { "cobrador",         "#28A745" },
        { "cobradora",        "#28A745" },
        { "admin",            "#E91E63" },
        { "administrador",    "#E91E63" },
    };

    private readonly CatalogService _catalogService;
    private readonly Logger _logger = AppLogger.Get();

    private string _themeColor    = "#28A745";
    private string _themeLight    = "#C8E6C9";
    private string _themeLighter  = "#E8F5E9";
    private string _currentRole   = string.Empty;

    public ThemeService(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public string ThemeColor   => _themeColor;
    public string ThemeLight   => _themeLight;
    public string ThemeLighter => _themeLighter;

    // ── Called after successful login ──────────────────────────────────────

    /// <summary>
    /// Carga y aplica el tema según el rol. Llama a la API (token ya guardado).
    /// Persiste los colores en SecureStorage para el arranque en frío.
    /// </summary>
    public async Task LoadAndApplyAsync(string role)
    {
        _currentRole = role;
        try
        {
            if (!_catalogService.IsLoaded)
                await _catalogService.LoadAsync();

            if (_catalogService.Theme != null)
            {
                // API returned a role-specific or global theme
                _themeColor   = _catalogService.Theme.PrimaryColor;
                _themeLight   = _catalogService.Theme.SecondaryColor  ?? LightenColor(_themeColor, 0.7);
                _themeLighter = _catalogService.Theme.AccentColor     ?? LightenColor(_themeColor, 0.85);
                _logger.Info("[ThemeService] Theme from API: {Color} ({Name})",
                    _themeColor, _catalogService.Theme.ThemeName);
            }
            else
            {
                ApplyRoleFallback(role);
                _logger.Info("[ThemeService] Fallback theme for role '{Role}': {Color}", role, _themeColor);
            }

            await PersistAsync();
            App.UpdateThemeColors(_themeColor, _themeLight, _themeLighter);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[ThemeService] Error loading theme, applying role fallback");
            ApplyRoleFallback(role);
            App.UpdateThemeColors(_themeColor, _themeLight, _themeLighter);
        }
    }

    // ── Called on cold start (no API call needed) ─────────────────────────

    /// <summary>
    /// Restaura el tema guardado en SecureStorage. Si no existe, aplica el fallback por rol.
    /// </summary>
    public static async Task RestoreCachedThemeAsync(string role = "")
    {
        try
        {
            var cachedRole = await SecureStorage.GetAsync(KeyThemeRole) ?? string.Empty;
            var primary    = await SecureStorage.GetAsync(KeyThemePrimary);

            // Only use cached colors if they belong to the same role (avoid stale cache from a different user)
            bool roleMatches = !string.IsNullOrEmpty(cachedRole)
                               && !string.IsNullOrEmpty(role)
                               && cachedRole.Equals(role, StringComparison.OrdinalIgnoreCase);

            if (roleMatches && !string.IsNullOrEmpty(primary))
            {
                var light   = await SecureStorage.GetAsync(KeyThemeLight)   ?? LightenColor(primary, 0.7);
                var lighter = await SecureStorage.GetAsync(KeyThemeLighter) ?? LightenColor(primary, 0.85);
                App.UpdateThemeColors(primary, light, lighter);
                return;
            }
        }
        catch { /* non-fatal */ }

        // No valid cached theme for this role → apply role-based fallback immediately
        if (!string.IsNullOrEmpty(role))
        {
            var color   = GetRoleFallbackColor(role);
            var light   = LightenColor(color, 0.7);
            var lighter = LightenColor(color, 0.85);
            App.UpdateThemeColors(color, light, lighter);
        }
    }

    /// <summary>
    /// Limpia los colores persistidos (llamar al hacer logout).
    /// </summary>
    public static void ClearCachedTheme()
    {
        SecureStorage.Remove(KeyThemePrimary);
        SecureStorage.Remove(KeyThemeLight);
        SecureStorage.Remove(KeyThemeLighter);
        SecureStorage.Remove(KeyThemeRole);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void ApplyRoleFallback(string role)
    {
        _themeColor   = GetRoleFallbackColor(role);
        _themeLight   = LightenColor(_themeColor, 0.7);
        _themeLighter = LightenColor(_themeColor, 0.85);
    }

    private static string GetRoleFallbackColor(string role)
    {
        foreach (var kv in RoleFallbackColors)
        {
            if (role.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return "#28A745"; // default green
    }

    /// <summary>
    /// Applies the role fallback color synchronously (no async/SecureStorage).
    /// Safe to call from CreateWindow on a ThreadPool thread.
    /// </summary>
    public static void ApplyRoleColorSync(string role)
    {
        var color   = GetRoleFallbackColor(role);
        var light   = LightenColor(color, 0.7);
        var lighter = LightenColor(color, 0.85);
        App.UpdateThemeColors(color, light, lighter);
    }

    private async Task PersistAsync()
    {
        await SecureStorage.SetAsync(KeyThemePrimary,  _themeColor);
        await SecureStorage.SetAsync(KeyThemeLight,    _themeLight);
        await SecureStorage.SetAsync(KeyThemeLighter,  _themeLighter);
        if (!string.IsNullOrEmpty(_currentRole))
            await SecureStorage.SetAsync(KeyThemeRole, _currentRole);
    }

    private static string LightenColor(string hexColor, double factor)
    {
        try
        {
            hexColor = hexColor.TrimStart('#');
            int r = Convert.ToInt32(hexColor[..2], 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            r = (int)(r + (255 - r) * factor);
            g = (int)(g + (255 - g) * factor);
            b = (int)(b + (255 - b) * factor);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return $"#{hexColor}";
        }
    }
}
