using MauiApp = Microsoft.Maui.Controls.Application;
using VitaRaiz.Mobile.Pages;

namespace VitaRaiz.Mobile;

public partial class App : MauiApp
{
	public App()
	{
		InitializeComponent();
		
		// TEMPORAL: Limpiar tokens anteriores para testing
		// Comenta o elimina estas líneas después de la primera ejecución
		SecureStorage.Remove("jwt_token");
		SecureStorage.Remove("user_id");
		SecureStorage.Remove("username");
		SecureStorage.Remove("role");
		System.Diagnostics.Debug.WriteLine("=== Tokens limpiados para testing ===");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("=== CreateWindow START ===");
			
			// Verificar si existe un token guardado
			var hasToken = CheckForSavedToken().GetAwaiter().GetResult();
			
			System.Diagnostics.Debug.WriteLine($"HasToken: {hasToken}");
			
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
			var token = await SecureStorage.GetAsync("jwt_token");
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
}
