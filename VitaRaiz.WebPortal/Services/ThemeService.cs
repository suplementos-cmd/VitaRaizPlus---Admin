using VitaRaiz.WebPortal.Models;
using NLog;

namespace VitaRaiz.WebPortal.Services;

public class ThemeService
{
    private readonly ApiService _api;
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    // Current applied theme
    public string PrimaryColor   { get; private set; } = "#607D8B";
    public string SecondaryColor { get; private set; } = "#B0BEC5";
    public string AccentColor    { get; private set; } = "#ECEFF1";
    public string PrimaryDark    => DarkenColor(PrimaryColor, 20);

    public event Action? ThemeChanged;

    public ThemeService(ApiService api) => _api = api;

    public async Task LoadForRoleAsync(int roleId)
    {
        try
        {
            var theme = await _api.GetAsync<ProfileThemeDto>($"api/catalogs/theme/role/{roleId}");
            if (theme is not null) Apply(theme.PrimaryColor, theme.SecondaryColor, theme.AccentColor);
        }
        catch (Exception ex)
        {
            _log.Warn(ex, "ThemeService: could not load theme for role {RoleId}", roleId);
        }
    }

    public void Apply(string primary, string? secondary = null, string? accent = null)
    {
        PrimaryColor   = primary;
        SecondaryColor = secondary ?? DarkenColor(primary, -30);
        AccentColor    = accent    ?? DarkenColor(primary, -60);
        ThemeChanged?.Invoke();
    }

    // Naive hex darkening (for CSS var computation)
    private static string DarkenColor(string hex, int amount)
    {
        try
        {
            hex = hex.TrimStart('#');
            var r = Math.Clamp(Convert.ToInt32(hex[..2], 16) - amount, 0, 255);
            var g = Math.Clamp(Convert.ToInt32(hex[2..4], 16) - amount, 0, 255);
            var b = Math.Clamp(Convert.ToInt32(hex[4..6], 16) - amount, 0, 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch { return hex; }
    }

    public MudBlazor.MudTheme BuildMudTheme() => new()
    {
        PaletteLight = new MudBlazor.PaletteLight
        {
            Primary      = PrimaryColor,
            PrimaryDarken  = PrimaryDark,
            Secondary    = SecondaryColor,
            AppbarBackground = PrimaryColor,
        },
        PaletteDark = new MudBlazor.PaletteDark
        {
            Primary   = PrimaryColor,
            Secondary = SecondaryColor,
        }
    };
}
