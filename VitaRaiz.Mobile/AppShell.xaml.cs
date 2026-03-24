using VitaRaiz.Mobile.Pages;

namespace VitaRaiz.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("=== Inicializando AppShell ===");
			InitializeComponent();
			
			// Cargar información del usuario en el header
			_ = LoadUserInfoAsync();
			
			System.Diagnostics.Debug.WriteLine("=== AppShell inicializado correctamente ===");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ERROR en AppShell constructor: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
			throw;
		}
	}

	private async Task LoadUserInfoAsync()
	{
		try
		{
			var username = await SecureStorage.GetAsync("username");
			var role = await SecureStorage.GetAsync("role");
			
			if (UsernameLabel != null)
			{
				UsernameLabel.Text = username ?? "Usuario";
			}
			
			if (RoleLabel != null)
			{
				RoleLabel.Text = role ?? "Cobrador";
			}
			
			System.Diagnostics.Debug.WriteLine($"Usuario cargado: {username} - {role}");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error cargando info de usuario: {ex.Message}");
		}
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine("=== Cerrando sesión ===");
			
			bool confirm = await DisplayAlert(
				"Cerrar Sesión",
				"¿Estás seguro que deseas cerrar sesión?",
				"Sí",
				"No");
			
			if (confirm)
			{
				// Limpiar tokens del SecureStorage
				SecureStorage.Remove("jwt_token");
				SecureStorage.Remove("user_id");
				SecureStorage.Remove("username");
				SecureStorage.Remove("role");
				
				System.Diagnostics.Debug.WriteLine("Tokens eliminados, navegando a LoginPage...");
				
				// Navegar al login
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					var window = Microsoft.Maui.Controls.Application.Current?.Windows[0];
					if (window != null)
					{
						window.Page = new LoginPage();
						System.Diagnostics.Debug.WriteLine("Navegación a LoginPage completada!");
					}
				});
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ERROR al cerrar sesión: {ex.Message}");
			await DisplayAlert("Error", $"Error al cerrar sesión: {ex.Message}", "OK");
		}
	}
}
