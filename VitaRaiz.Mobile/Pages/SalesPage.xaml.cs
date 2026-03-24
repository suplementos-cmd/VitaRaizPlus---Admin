using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class SalesPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<string> _statusFilters = new() { "Todas", "Activas", "Completadas", "Vencidas" };
    private string _selectedStatus = "Todas";

    public SalesPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Inicializando SalesPage ===");
            InitializeComponent();
            BindingContext = this;
            
            _apiService = new ApiService();
            
            SearchCommand = new Command(OnSearch);
            ViewSaleDetailCommand = new Command(OnViewSaleDetail);
            
            _ = LoadSalesAsync();
            System.Diagnostics.Debug.WriteLine("=== SalesPage inicializado correctamente ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en SalesPage constructor: {ex.Message}");
        }
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
            // Obtener userId del usuario actual
            var userIdStr = await SecureStorage.GetAsync("user_id");
            int userId = int.TryParse(userIdStr, out var id) ? id : 0;
            
            // Cargar ventas activas desde la API
            var queryParams = new Dictionary<string, string?>();
            
            if (_selectedStatus == "Activas")
            {
                queryParams.Add("status", "active");
            }
            else if (_selectedStatus == "Completadas")
            {
                queryParams.Add("status", "completed");
            }
            else if (_selectedStatus == "Vencidas")
            {
                // Filtrar ventas vencidas (TODO: implementar lógica en API)
                queryParams.Add("status", "active");
            }
            
            var salesData = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("api/sales", queryParams);
            
            Sales.Clear();
            if (salesData != null)
            {
                foreach (var sale in salesData)
                {
                    Sales.Add(new SaleItemDto
                    {
                        SaleId = sale.SaleId,
                        CustomerName = sale.CustomerName,
                        SaleDate = sale.SaleDate,
                        TotalAmount = sale.TotalAmount,
                        PendingAmount = sale.Balance,
                        Status = sale.Status switch
                        {
                            "active" => "Activa",
                            "completed" => "Completada",
                            "cancelled" => "Cancelada",
                            _ => sale.Status
                        },
                        StatusColor = sale.Status switch
                        {
                            "active" => "#28A745",
                            "completed" => "#17A2B8",
                            "cancelled" => "#DC3545",
                            _ => "#999"
                        }
                    });
                }
            }
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
