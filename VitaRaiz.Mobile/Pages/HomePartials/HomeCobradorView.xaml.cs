namespace VitaRaiz.Mobile.Pages.HomePartials;

public partial class HomeCobradorView : ContentView
{
    private readonly HomeContext _ctx;

    public HomeCobradorView(HomeContext context)
    {
        _ctx = context;
        InitializeComponent();
        BindingContext = _ctx;
    }

    private async void OnSalesClicked(object? s, EventArgs e)           => await _ctx.GoToSales();
    private async void OnRegistrarCobroClicked(object? s, EventArgs e)  => await _ctx.GoToRegistroCobro();
}
