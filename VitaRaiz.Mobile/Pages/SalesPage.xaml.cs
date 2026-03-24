using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace VitaRaiz.Mobile.Pages;

public partial class SalesPage : ContentPage, INotifyPropertyChanged
{
    private ObservableCollection<string> _statusFilters = new() { "Todas", "Activas", "Completadas", "Vencidas" };
    private string _selectedStatus = "Todas";

    public SalesPage()
    {
        InitializeComponent();
        BindingContext = this;
        
        SearchCommand = new Command(OnSearch);
        ViewSaleDetailCommand = new Command(OnViewSaleDetail);
        
        _ = LoadSalesAsync();
    }

    public ObservableCollection<string> StatusFilters
    {
        get => _statusFilters;
        set
        {
            _statusFilters = value;
            OnPropertyChanged();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            _selectedStatus = value;
            OnPropertyChanged();
            OnSearch();
        }
    }

    public ObservableCollection<SaleItemDto> Sales { get; set; } = new();

    public ICommand SearchCommand { get; }
    public ICommand ViewSaleDetailCommand { get; }

    private async Task LoadSalesAsync()
    {
        try
        {
            // TODO: Cargar ventas reales desde la API
            Sales.Clear();
            Sales.Add(new SaleItemDto
            {
                SaleId = 1,
                CustomerName = "Juan Pérez",
                SaleDate = DateTime.Now.AddDays(-5),
                TotalAmount = 5000.00m,
                PendingAmount = 2000.00m,
                Status = "Activa",
                StatusColor = "#28A745"
            });
            Sales.Add(new SaleItemDto
            {
                SaleId = 2,
                CustomerName = "María García",
                SaleDate = DateTime.Now.AddDays(-3),
                TotalAmount = 3500.00m,
                PendingAmount = 1500.00m,
                Status = "Activa",
                StatusColor = "#28A745"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sales: {ex.Message}");
        }
    }

    private void OnSearch()
    {
        // TODO: Implementar filtrado por estado
    }

    private void OnViewSaleDetail()
    {
        // TODO: Navegar a detalle de venta
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SaleItemDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#999";
    public double PaymentProgress => TotalAmount > 0 ? (double)((TotalAmount - PendingAmount) / TotalAmount) : 0;
}
