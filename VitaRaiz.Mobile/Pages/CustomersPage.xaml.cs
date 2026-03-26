using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class CustomersPage : ContentPage
{
    private readonly ApiService _apiService;
    private string _searchText = string.Empty;
    private List<CustomerDto> _allCustomers = new();

    public CustomersPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Inicializando CustomersPage ===");
            InitializeComponent();
            BindingContext = this;
            
            _apiService = new ApiService();
            
            SearchCommand = new Command(OnSearch);
            ViewCustomerDetailCommand = new Command(OnViewCustomerDetail);
            
            _ = LoadCustomersAsync();
            System.Diagnostics.Debug.WriteLine("=== CustomersPage inicializado correctamente ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en CustomersPage constructor: {ex.Message}");
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<CustomerItemDto> Customers { get; set; } = new();

    public ICommand SearchCommand { get; }
    public ICommand ViewCustomerDetailCommand { get; }

    private async Task LoadCustomersAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("CustomersPage: Cargando clientes desde API...");
            // Cargar clientes desde la API
            var customersData = await _apiService.GetAsync<List<CustomerDto>>("api/customers");
            
            System.Diagnostics.Debug.WriteLine($"CustomersPage: Recibidos {customersData?.Count ?? 0} clientes");
            
            if (customersData != null)
            {
                _allCustomers = customersData;
                DisplayCustomers(_allCustomers);
                System.Diagnostics.Debug.WriteLine($"CustomersPage: Mostrando {Customers.Count} clientes");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("CustomersPage: No se recibieron datos (null)");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CustomersPage: Error loading customers: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    private void DisplayCustomers(List<CustomerDto> customers)
    {
        Customers.Clear();
        
        var colors = new[] { "#9C27B0", "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#00BCD4" };
        int colorIndex = 0;
        
        foreach (var customer in customers)
        {
            Customers.Add(new CustomerItemDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                PhoneNumber = customer.PhoneNumber ?? "Sin teléfono",
                ZoneName = customer.ZoneName ?? "Sin zona",
                PendingBalance = 0, // TODO: Obtener balance pendiente del cliente
                IsGoldCustomer = customer.IsGoldCustomer,
                IsBlacklisted = customer.IsBlacklisted,
                AvatarColor = colors[colorIndex % colors.Length]
            });
            colorIndex++;
        }
    }

    private void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            DisplayCustomers(_allCustomers);
        }
        else
        {
            var filtered = _allCustomers.Where(c => 
                c.CustomerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(SearchText))
            ).ToList();
            DisplayCustomers(filtered);
        }
    }

    private void OnViewCustomerDetail()
    {
        // TODO: Navegar a detalle de cliente
    }

    // ═══ Bottom Tab Navigation ═══
    private async void OnTabInicio(object? s, EventArgs e) => await Shell.Current.GoToAsync("//HomePage");
    private async void OnTabVentas(object? s, EventArgs e) => await Shell.Current.GoToAsync("//SalesPage");
    private async void OnTabCobranza(object? s, EventArgs e) => await Shell.Current.GoToAsync("//PaymentPage");
}

public class CustomerItemDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public decimal PendingBalance { get; set; }
    public bool IsGoldCustomer { get; set; }
    public bool IsBlacklisted { get; set; }
    public string AvatarColor { get; set; } = "#999";
    public string Initials => CustomerName.Length >= 2 ? CustomerName.Substring(0, 2).ToUpper() : "XX";
    public string StatusIcon => IsBlacklisted ? "⛔" : IsGoldCustomer ? "⭐" : "👤";
}
