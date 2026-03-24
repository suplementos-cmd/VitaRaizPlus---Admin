using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace VitaRaiz.Mobile.Pages;

public partial class CustomersPage : ContentPage, INotifyPropertyChanged
{
    private string _searchText = string.Empty;

    public CustomersPage()
    {
        InitializeComponent();
        BindingContext = this;
        
        SearchCommand = new Command(OnSearch);
        ViewCustomerDetailCommand = new Command(OnViewCustomerDetail);
        
        _ = LoadCustomersAsync();
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
            // TODO: Cargar clientes reales desde la API
            Customers.Clear();
            Customers.Add(new CustomerItemDto
            {
                CustomerId = 1,
                CustomerName = "Juan Pérez",
                PhoneNumber = "809-555-1234",
                ZoneName = "Zona Norte",
                PendingBalance = 2500.00m,
                IsGoldCustomer = true,
                AvatarColor = "#9C27B0"
            });
            Customers.Add(new CustomerItemDto
            {
                CustomerId = 2,
                CustomerName = "María García",
                PhoneNumber = "809-555-5678",
                ZoneName = "Zona Sur",
                PendingBalance = 1800.00m,
                IsGoldCustomer = false,
                AvatarColor = "#2196F3"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading customers: {ex.Message}");
        }
    }

    private void OnSearch()
    {
        // TODO: Implementar búsqueda
    }

    private void OnViewCustomerDetail()
    {
        // TODO: Navegar a detalle de cliente
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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
