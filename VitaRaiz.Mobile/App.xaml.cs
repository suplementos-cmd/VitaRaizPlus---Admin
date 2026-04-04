using MauiApp = Microsoft.Maui.Controls.Application;
using VitaRaiz.Mobile.Pages;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile;

public partial class App : MauiApp
{
	// Colores de tema globales accesibles desde cualquier página (se sobreescriben tras login)
	public static string ThemeColor      { get; set; } = "#607D8B";
	public static string ThemeColorLight { get; set; } = "#B0BEC5";
	public static string ThemeColorLighter { get; set; } = "#ECEFF1";
	public static string ThemeTitleTextColor { get; set; } = "#111111";
	public static string ThemeFormTextColor  { get; set; } = "#444444";
	public static string ThemeMenuTextColor  { get; set; } = "#FFFFFF";

	public App()
	{
		InitializeComponent();
		// Los colores de tema iniciales ya están definidos en App.xaml como recursos estáticos.
		// Se actualizan una sola vez en LoginPage tras LoadAndApplyAsync (tema confirmado del perfil).
	}

	/// <summary>
	/// Actualiza los colores del tema globalmente y notifica a todas las páginas
	/// </summary>
	public static void UpdateThemeColors(string primary, string light, string lighter,
		string? titleText = null, string? formText = null, string? menuText = null)
	{
		ThemeColor           = primary;
		ThemeColorLight      = light;
		ThemeColorLighter    = lighter;
		ThemeTitleTextColor  = titleText ?? "#111111";
		ThemeFormTextColor   = formText  ?? "#444444";
		ThemeMenuTextColor   = menuText  ?? "#FFFFFF";

		// Actualizar recursos globales para que todas las páginas los usen
		if (Current?.Resources != null)
		{
			var darkHex = DarkenColor(primary, 0.25);
			Current.Resources["ThemeColor"]           = Color.FromArgb(primary);
			Current.Resources["ThemeColorLight"]      = Color.FromArgb(light);
			Current.Resources["ThemeColorLighter"]    = Color.FromArgb(lighter);
			Current.Resources["ThemeColorDark"]       = Color.FromArgb(darkHex);
			Current.Resources["ThemeColorHex"]        = primary;
			Current.Resources["ThemeColorLightHex"]   = light;
			Current.Resources["ThemeColorLighterHex"] = lighter;
			Current.Resources["ThemeTitleTextColor"]  = Color.FromArgb(ThemeTitleTextColor);
			Current.Resources["ThemeFormTextColor"]   = Color.FromArgb(ThemeFormTextColor);
			Current.Resources["ThemeMenuTextColor"]   = Color.FromArgb(ThemeMenuTextColor);

			// Update Shell tab bar title color when navigation is already active
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					if (Current?.Windows.Count > 0 && Current.Windows[0].Page is Shell shell)
						shell.SetValue(Shell.TabBarTitleColorProperty, Color.FromArgb(primary));
				}
				catch { /* non-fatal */ }
			});

			System.Diagnostics.Debug.WriteLine($"[App] Theme updated globally: {primary}");
		}
	}

	/// <summary>
	/// Resetea el tema al color por defecto (para usar al cerrar sesión)
	/// </summary>
	public static void ResetThemeToDefault()
	{
		// Gris neutro — el tema real se aplica tras el próximo login
		const string neutralColor = "#607D8B";
		string lightColor = LightenColor(neutralColor, 0.7);
		string lighterColor = LightenColor(neutralColor, 0.85);
		UpdateThemeColors(neutralColor, lightColor, lighterColor);
		System.Diagnostics.Debug.WriteLine("[App] Theme reset to neutral (pending login)");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// La app siempre arranca en LoginPage — el usuario debe autenticarse en cada sesión.
		// El tema se aplica una sola vez en LoginPage tras el login exitoso (via LoadAndApplyAsync),
		// eliminando cualquier flash de color durante el arranque.
		return new Window(new LoginPage());
	}

	private async Task<bool> CheckForSavedToken()
	{
		try
		{
			var token = await SecureStorage.GetAsync("auth_token").ConfigureAwait(false);
			return !string.IsNullOrEmpty(token);
		}
		catch
		{
			return false;
		}
	}
	
	/// <summary>
	/// Aclara un color hexadecimal mezclándolo con blanco
	/// </summary>
	private static string LightenColor(string hexColor, double factor)
	{
		try
		{
			var color = Color.FromArgb(hexColor);
			var r = (int)(color.Red * 255 + (255 - color.Red * 255) * factor);
			var g = (int)(color.Green * 255 + (255 - color.Green * 255) * factor);
			var b = (int)(color.Blue * 255 + (255 - color.Blue * 255) * factor);
			return $"#{r:X2}{g:X2}{b:X2}";
		}
		catch
		{
			return hexColor;
		}
	}

	private static string DarkenColor(string hexColor, double factor)
	{
		try
		{
			var color = Color.FromArgb(hexColor);
			var r = Math.Clamp((int)(color.Red   * 255 * (1.0 - factor)), 0, 255);
			var g = Math.Clamp((int)(color.Green * 255 * (1.0 - factor)), 0, 255);
			var b = Math.Clamp((int)(color.Blue  * 255 * (1.0 - factor)), 0, 255);
			return $"#{r:X2}{g:X2}{b:X2}";
		}
		catch
		{
			return hexColor;
		}
	}
}
