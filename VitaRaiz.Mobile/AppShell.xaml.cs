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
			
			// Register routes for Shell navigation
			Routing.RegisterRoute("SaleDetailPage", typeof(SaleDetailPage));
			Routing.RegisterRoute("CreateSalePage", typeof(CreateSalePage));
			Routing.RegisterRoute("PaymentDetailPage", typeof(PaymentDetailPage));
			
			System.Diagnostics.Debug.WriteLine("=== AppShell inicializado correctamente ===");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"ERROR en AppShell constructor: {ex.Message}");
			throw;
		}
	}
}
