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

			// Apply the current role theme to the tab bar
			try { SetValue(Shell.TabBarTitleColorProperty, Color.FromArgb(App.ThemeColor)); }
			catch { /* non-fatal */ }
			
			// Register routes for Shell navigation
			Routing.RegisterRoute("SaleDetailPage", typeof(SaleDetailPage));
			Routing.RegisterRoute("CreateSalePage", typeof(CreateSalePage));
			Routing.RegisterRoute("PaymentDetailPage", typeof(PaymentDetailPage));
			Routing.RegisterRoute("RegistrarCobroPage", typeof(RegistrarCobroPage));
			
			System.Diagnostics.Debug.WriteLine("=== AppShell inicializado correctamente ===");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ERROR en AppShell constructor: {ex.Message}");
			throw;
		}
	}
}
