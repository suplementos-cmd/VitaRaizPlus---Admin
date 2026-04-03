namespace VitaRaiz.Mobile.Pages.HomePartials;

public partial class HomeAdminRHView : ContentView
{
    private readonly HomeContext _ctx;

    public HomeAdminRHView(HomeContext context)
    {
        _ctx = context;
        InitializeComponent();
        BindingContext = _ctx;
    }

    // These navigate to management pages that will be registered as Shell routes in the future.
    // For now they surface a friendly "coming soon" notice.
    private async void OnUsuariosClicked(object? s, EventArgs e)  =>
        await Shell.Current.DisplayAlert("Próximamente", "Módulo de gestión de usuarios", "OK");

    private async void OnZonasClicked(object? s, EventArgs e) =>
        await Shell.Current.DisplayAlert("Próximamente", "Módulo de zonas y colonias", "OK");

    private async void OnCatalogosClicked(object? s, EventArgs e) =>
        await Shell.Current.DisplayAlert("Próximamente", "Módulo de catálogos", "OK");
}
