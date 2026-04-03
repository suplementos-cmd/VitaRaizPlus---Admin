namespace VitaRaiz.Mobile.Pages.HomePartials;

public partial class HomeVendedorView : ContentView
{
    private readonly HomeContext _ctx;

    public HomeVendedorView(HomeContext context)
    {
        _ctx = context;
        InitializeComponent();
        BindingContext = _ctx;
    }

    private async void OnSalesClicked(object? s, EventArgs e)      => await _ctx.GoToSales();
    private async void OnCustomersClicked(object? s, EventArgs e)  => await _ctx.GoToCustomers();
    private async void OnNuevaVentaClicked(object? s, EventArgs e) => await _ctx.GoToCreateSale();
}
