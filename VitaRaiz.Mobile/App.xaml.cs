using MauiApp = Microsoft.Maui.Controls.Application;

namespace VitaRaiz.Mobile;

public partial class App : MauiApp
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}