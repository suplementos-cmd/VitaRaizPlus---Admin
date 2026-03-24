using MauiApp = Microsoft.Maui.Controls.Application;
using VitaRaiz.Mobile.Pages;

namespace VitaRaiz.Mobile;

public partial class App : MauiApp
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Verificar si existe un token guardado
		var hasToken = CheckForSavedToken().GetAwaiter().GetResult();
		
		var mainPage = hasToken ? new MainPage() : new LoginPage();
		
		return new Window(mainPage);
	}

	private async Task<bool> CheckForSavedToken()
	{
		try
		{
			var token = await SecureStorage.GetAsync("jwt_token");
			return !string.IsNullOrEmpty(token);
		}
		catch
		{
			return false;
		}
	}
}