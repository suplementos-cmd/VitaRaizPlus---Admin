namespace VitaRaiz.Mobile.Pages.HomePartials;

public partial class HomeAdminFullView : ContentView
{
    private readonly HomeContext _ctx;

    public HomeAdminFullView(HomeContext context)
    {
        _ctx = context;
        InitializeComponent();
        BindingContext = _ctx;
    }

    private async void OnSalesClicked(object? s, EventArgs e)          => await _ctx.GoToSales();
    private async void OnCustomersClicked(object? s, EventArgs e)      => await _ctx.GoToCustomers();
    private async void OnNuevaVentaClicked(object? s, EventArgs e)     => await _ctx.GoToCreateSale();
    private async void OnRegistrarCobroClicked(object? s, EventArgs e) => await _ctx.GoToRegistroCobro();
}
