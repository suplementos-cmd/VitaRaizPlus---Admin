using MauiApp = Microsoft.Maui.Controls.Application;
using VitaRaiz.Mobile.Pages;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile;

public partial class App : MauiApp
{
	// Colores de tema globales accesibles desde cualquier página
	public static string ThemeColor { get; set; } = "#28A745";
	public static string ThemeColorLight { get; set; } = "#C8E6C9";
	public static string ThemeColorLighter { get; set; } = "#E8F5E9";

	public App()
	{
		InitializeComponent();
		
		// Establecer colores de tema iniciales en Resources
		UpdateThemeColors(ThemeColor, ThemeColorLight, ThemeColorLighter);
		
		// TEMPORAL: Limpiar tokens anteriores para testing
		// Comenta o elimina estas líneas después de la primera ejecución
		SecureStorage.Remove("auth_token");
		SecureStorage.Remove("user_id");
		SecureStorage.Remove("username");
		SecureStorage.Remove("role");
		System.Diagnostics.Debug.WriteLine("=== Tokens limpiados para testing ===");
	}

	/// <summary>
	/// Actualiza los colores del tema globalmente y notifica a todas las páginas
	/// </summary>
	public static void UpdateThemeColors(string primary, string light, string lighter)
	{
		ThemeColor = primary;
		ThemeColorLight = light;
		ThemeColorLighter = lighter;

		// Actualizar recursos globales para que todas las páginas los usen
		if (Current?.Resources != null)
		{
			Current.Resources["ThemeColor"] = Color.FromArgb(primary);
			Current.Resources["ThemeColorLight"] = Color.FromArgb(light);
			Current.Resources["ThemeColorLighter"] = Color.FromArgb(lighter);
			Current.Resources["ThemeColorHex"] = primary;
			Current.Resources["ThemeColorLightHex"] = light;
			Current.Resources["ThemeColorLighterHex"] = lighter;

			System.Diagnostics.Debug.WriteLine($"[App] Theme updated globally: {primary}");
		}
	}

	/// <summary>
	/// Resetea el tema al color por defecto (para usar al cerrar sesión)
	/// </summary>
	public static void ResetThemeToDefault()
	{
		const string defaultColor = "#28A745"; // Verde por defecto
		string lightColor = LightenColor(defaultColor, 0.7);
		string lighterColor = LightenColor(defaultColor, 0.85);
		UpdateThemeColors(defaultColor, lightColor, lighterColor);
		System.Diagnostics.Debug.WriteLine("[App] Theme reset to default");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("=== CreateWindow START ===");
			
			// Verificar si existe un token guardado
			var hasToken = CheckForSavedToken().GetAwaiter().GetResult();
			
			System.Diagnostics.Debug.WriteLine($"HasToken: {hasToken}");
			
			// Cargar tema del usuario antes de crear la página principal
			if (hasToken)
			{
				LoadInitialTheme().GetAwaiter().GetResult();
			}
			
			Page mainPage = hasToken ? new AppShell() : new LoginPage();
			
			System.Diagnostics.Debug.WriteLine($"Página inicial: {mainPage.GetType().Name}");
			
			return new Window(mainPage);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ERROR en CreateWindow: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
			// En caso de error, siempre mostrar LoginPage
			return new Window(new LoginPage());
		}
	}

	private async Task<bool> CheckForSavedToken()
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("=== CheckForSavedToken ===");
			var token = await SecureStorage.GetAsync("auth_token");
			System.Diagnostics.Debug.WriteLine($"Token encontrado: {!string.IsNullOrEmpty(token)}");
			if (!string.IsNullOrEmpty(token))
			{
				System.Diagnostics.Debug.WriteLine($"Token: {token.Substring(0, Math.Min(20, token.Length))}...");
			}
			return !string.IsNullOrEmpty(token);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error al verificar token: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Carga el tema inicial basado en el rol guardado en SecureStorage
	/// </summary>
	private async Task LoadInitialTheme()
	{
		try
		{
			var role = await SecureStorage.GetAsync("role") ?? "Cobrador";
			System.Diagnostics.Debug.WriteLine($"[App] Loading initial theme for role: {role}");
			
			// Aplicar color por defecto según el rol
			string themeColor = role switch
			{
				"Admin" or "Administrador" => "#E91E63", // Rosa/Magenta
				"Supervisor" => "#FF9800", // Naranja
				"Vendedor" => "#2196F3", // Azul
				"Cobrador" or _ => "#28A745" // Verde (default)
			};
			
			// Calcular colores claros
			string lightColor = LightenColor(themeColor, 0.7);
			string lighterColor = LightenColor(themeColor, 0.85);
			
			UpdateThemeColors(themeColor, lightColor, lighterColor);
			System.Diagnostics.Debug.WriteLine($"[App] Initial theme loaded: {themeColor}");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[App] Error loading initial theme: {ex.Message}");
			// Mantener el tema por defecto (verde) si hay error
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
}
