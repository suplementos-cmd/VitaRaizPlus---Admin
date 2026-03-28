using VitaRaiz.Mobile.Models;

namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Manages application theming based on user role and API configuration
/// </summary>
public class ThemeService
{
    private readonly CatalogService _catalogService;
    private string _themeColor = "#28A745"; // Default green
    private string _themeColorLight = "#C8E6C9";
    private string _themeColorLighter = "#E8F5E9";
    private string _role = string.Empty;

    public ThemeService(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public string ThemeColor => _themeColor;
    public string ThemeColorLight => _themeColorLight;
    public string ThemeColorLighter => _themeColorLighter;

    /// <summary>
    /// Load theme from API based on user role
    /// </summary>
    public async Task LoadThemeAsync(string role, int userId = 0)
    {
        _role = role;
        
        // Ensure catalogs are loaded
        if (!_catalogService.IsLoaded)
        {
            await _catalogService.LoadAsync();
        }

        // Check if there's a theme from the API
        if (_catalogService.Theme != null)
        {
            _themeColor = _catalogService.Theme.PrimaryColor;
            _themeColorLight = _catalogService.Theme.SecondaryColor ?? LightenColor(_themeColor, 0.3);
            _themeColorLighter = _catalogService.Theme.AccentColor ?? LightenColor(_themeColor, 0.6);
            
            System.Diagnostics.Debug.WriteLine($"[ThemeService] Theme loaded from API: {_themeColor}");
        }
        else
        {
            // Tema por defecto según rol
            _themeColor = role.ToLower() switch
            {
                var r when r.Contains("admin") => "#E91E63", // Rosa para admin
                var r when r.Contains("supervisor") => "#FF9800", // Naranja para supervisor
                var r when r.Contains("vendedor") => "#2196F3", // Azul para vendedor
                var r when r.Contains("cobrador") => "#28A745", // Verde para cobrador
                _ => "#28A745" // Verde por defecto
            };
            _themeColorLight = LightenColor(_themeColor, 0.3);
            _themeColorLighter = LightenColor(_themeColor, 0.6);
            
            System.Diagnostics.Debug.WriteLine($"[ThemeService] Theme by role: {_themeColor}");
        }
    }

    /// <summary>
    /// Aclara un color hex agregando transparencia o mezclando con blanco
    /// </summary>
    private string LightenColor(string hexColor, double factor)
    {
        try
        {
            hexColor = hexColor.TrimStart('#');
            
            int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            
            r = (int)(r + (255 - r) * factor);
            g = (int)(g + (255 - g) * factor);
            b = (int)(b + (255 - b) * factor);
            
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return hexColor;
        }
    }
}
