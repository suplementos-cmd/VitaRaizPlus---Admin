using VitaRaiz.WebPortal.Models;
using NLog;

namespace VitaRaiz.WebPortal.Services;

public class ThemeService
{
    private readonly ApiService _api;
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();

    // Current applied theme
    public string PrimaryColor    { get; private set; } = "#607D8B";
    public string SecondaryColor  { get; private set; } = "#B0BEC5";
    public string AccentColor     { get; private set; } = "#ECEFF1";
    public string BackgroundColor { get; private set; } = "#FAFAFA";
    public string TextColor       { get; private set; } = "#333333";
    public string TitleTextColor  { get; private set; } = "#111111";
    public string FormTextColor   { get; private set; } = "#444444";
    public string MenuTextColor   { get; private set; } = "#FFFFFF";
    public string IconName        { get; private set; } = string.Empty;
    public string PrimaryDark     => DarkenColor(PrimaryColor, 20);

    public event Action? ThemeChanged;

    public ThemeService(ApiService api) => _api = api;

    public async Task LoadForRoleAsync(int roleId)
    {
        try
        {
            var theme = await _api.GetAsync<ProfileThemeDto>($"api/catalogs/theme/role/{roleId}");
            if (theme is not null)
                Apply(theme.PrimaryColor, theme.SecondaryColor, theme.AccentColor,
                      theme.BackgroundColor, theme.TextColor,
                      theme.TitleTextColor, theme.FormTextColor, theme.MenuTextColor,
                      theme.IconName);
        }
        catch (Exception ex)
        {
            _log.Warn(ex, "ThemeService: could not load theme for role {RoleId}", roleId);
        }
    }

    public void Apply(string primary, string? secondary = null, string? accent = null,
                       string? background = null, string? text = null,
                       string? titleText = null, string? formText = null,
                       string? menuText = null, string? icon = null)
    {
        PrimaryColor    = primary;
        SecondaryColor  = secondary   ?? DarkenColor(primary, -30);
        AccentColor     = accent      ?? DarkenColor(primary, -60);
        BackgroundColor = background  ?? "#FAFAFA";
        TextColor       = text        ?? "#333333";
        TitleTextColor  = titleText   ?? "#111111";
        FormTextColor   = formText    ?? "#444444";
        MenuTextColor   = menuText    ?? "#FFFFFF";
        IconName        = icon        ?? string.Empty;
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
            Primary          = PrimaryColor,
            PrimaryDarken    = PrimaryDark,
            Secondary        = SecondaryColor,
            Tertiary         = AccentColor,
            Background       = BackgroundColor,
            AppbarBackground = PrimaryColor,
            AppbarText       = MenuTextColor,
            DrawerBackground = PrimaryColor,
            DrawerText       = MenuTextColor,
            TextPrimary      = TextColor,
        },
        PaletteDark = new MudBlazor.PaletteDark
        {
            Primary   = PrimaryColor,
            Secondary = SecondaryColor,
        }
    };

    /// <summary>Genera un bloque &lt;style&gt; con CSS custom properties para usar en Razor con @@ThemeSvc.CssVars</summary>
    public string CssVars =>
        $":root{{" +
        $"--theme-primary:{PrimaryColor};" +
        $"--theme-secondary:{SecondaryColor};" +
        $"--theme-accent:{AccentColor};" +
        $"--theme-background:{BackgroundColor};" +
        $"--theme-text:{TextColor};" +
        $"--theme-title-text:{TitleTextColor};" +
        $"--theme-form-text:{FormTextColor};" +
        $"--theme-menu-text:{MenuTextColor};" +
        $"}}";
}
