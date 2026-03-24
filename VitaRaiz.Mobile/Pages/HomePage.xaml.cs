using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace VitaRaiz.Mobile.Pages;

public partial class HomePage : ContentPage, INotifyPropertyChanged
{
    private string _username = string.Empty;
    private string _role = string.Empty;
    private decimal _todayPayments;
    private int _todaySales;
    private decimal _pendingAmount;
    private int _todayVisits;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = this;
        
        GoToPaymentsCommand = new Command(async () => await Shell.Current.GoToAsync("//PaymentPage"));
        GoToSalesCommand = new Command(async () => await Shell.Current.GoToAsync("//SalesPage"));
        GoToCustomersCommand = new Command(async () => await Shell.Current.GoToAsync("//CustomersPage"));
        
        _ = LoadDataAsync();
    }

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }

    public string Role
    {
        get => _role;
        set
        {
            _role = value;
            OnPropertyChanged();
        }
    }

    public decimal TodayPayments
    {
        get => _todayPayments;
        set
        {
            _todayPayments = value;
            OnPropertyChanged();
        }
    }

    public int TodaySales
    {
        get => _todaySales;
        set
        {
            _todaySales = value;
            OnPropertyChanged();
        }
    }

    public decimal PendingAmount
    {
        get => _pendingAmount;
        set
        {
            _pendingAmount = value;
            OnPropertyChanged();
        }
    }

    public int TodayVisits
    {
        get => _todayVisits;
        set
        {
            _todayVisits = value;
            OnPropertyChanged();
        }
    }

    public ICommand GoToPaymentsCommand { get; }
    public ICommand GoToSalesCommand { get; }
    public ICommand GoToCustomersCommand { get; }

    private async Task LoadDataAsync()
    {
        try
        {
            // Cargar datos del usuario desde SecureStorage
            Username = await SecureStorage.GetAsync("username") ?? "Usuario";
            Role = await SecureStorage.GetAsync("role") ?? "Cobrador";
            
            // TODO: Cargar estadísticas reales desde la API
            TodayPayments = 12500.00m;
            TodaySales = 8;
            PendingAmount = 45000.00m;
            TodayVisits = 15;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
