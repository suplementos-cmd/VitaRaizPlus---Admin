using VitaRaiz.Mobile.Models;
using NLog;

namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Gestiona el tema visual de la aplicación basado en el perfil del usuario.
/// Prioridad: 1) GET api/Catalogs/theme/role/{roleId}  2) Caché (SecureStorage)  3) Neutro hasta login.
/// </summary>
public class ThemeService
{
    // ── SecureStorage keys ───────────────────────────────────────────
    private const string KeyThemePrimary  = "theme_primary_color";
    private const string KeyThemeLight    = "theme_light_color";
    private const string KeyThemeLighter  = "theme_lighter_color";
    private const string KeyThemeRole     = "theme_role_id";

    private const string NeutralColor   = "#607D8B";
    private const string NeutralLight   = "#B0BEC5";
    private const string NeutralLighter = "#ECEFF1";

    private readonly ApiService _apiService;
    private readonly Logger _logger = AppLogger.Get();

    private string _themeColor    = NeutralColor;
    private string _themeLight    = NeutralLight;
    private string _themeLighter  = NeutralLighter;

    public ThemeService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public string ThemeColor   => _themeColor;
    public string ThemeLight   => _themeLight;
    public string ThemeLighter => _themeLighter;

    // ── Called after successful login ───────────────────────────────────

    /// <summary>
    /// Carga y aplica el tema del perfil del usuario desde el endpoint dedicado.
    /// GET api/Catalogs/theme/role/{roleId} — requiere token ya guardado.
    /// Persiste los colores en SecureStorage para el arranque en frío.
    /// </summary>
    public async Task LoadAndApplyAsync(int roleId)
    {
        try
        {
            var profileTheme = await _apiService.GetProfileThemeAsync(roleId);

            if (profileTheme != null)
            {
                _themeColor   = profileTheme.PrimaryColor;
                _themeLight   = profileTheme.SecondaryColor  ?? LightenColor(_themeColor, 0.7);
                _themeLighter = profileTheme.AccentColor      ?? LightenColor(_themeColor, 0.85);
                _logger.Info("[ThemeService] Theme from API: {Color} ({Name}) for roleId={RoleId}",
                    _themeColor, profileTheme.ThemeName, roleId);
            }
            else
            {
                _logger.Warn("[ThemeService] API returned no theme for roleId={RoleId}, keeping neutral", roleId);
            }

            await PersistAsync(roleId);
            App.UpdateThemeColors(_themeColor, _themeLight, _themeLighter);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[ThemeService] Error loading theme from API, keeping neutral");
            App.UpdateThemeColors(_themeColor, _themeLight, _themeLighter);
        }
    }

    // ── Called on cold start (no API call needed) ─────────────────────

    /// <summary>
    /// Restaura el tema guardado en SecureStorage.
    /// Si no existe o el roleId no coincide, muestra neutro hasta el próximo login.
    /// </summary>
    public static async Task RestoreCachedThemeAsync(int roleId = 0)
    {
        try
        {
            var cachedRoleIdStr = await SecureStorage.GetAsync(KeyThemeRole) ?? string.Empty;
            var primary         = await SecureStorage.GetAsync(KeyThemePrimary);

            bool roleMatches = roleId > 0
                               && int.TryParse(cachedRoleIdStr, out int cachedRoleId)
                               && cachedRoleId == roleId;

            if (roleMatches && !string.IsNullOrEmpty(primary))
            {
                var light   = await SecureStorage.GetAsync(KeyThemeLight)   ?? LightenColor(primary, 0.7);
                var lighter = await SecureStorage.GetAsync(KeyThemeLighter) ?? LightenColor(primary, 0.85);
                App.UpdateThemeColors(primary, light, lighter);
                return;
            }
        }
        catch { /* non-fatal */ }

        // Sin caché válida → neutro hasta que el usuario haga login y la API responda
        App.UpdateThemeColors(NeutralColor, NeutralLight, NeutralLighter);
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

    private async Task PersistAsync(int roleId)
    {
        await SecureStorage.SetAsync(KeyThemePrimary,  _themeColor);
        await SecureStorage.SetAsync(KeyThemeLight,    _themeLight);
        await SecureStorage.SetAsync(KeyThemeLighter,  _themeLighter);
        await SecureStorage.SetAsync(KeyThemeRole,     roleId.ToString());
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
