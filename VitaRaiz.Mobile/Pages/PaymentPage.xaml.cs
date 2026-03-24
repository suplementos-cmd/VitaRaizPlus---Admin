using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace VitaRaiz.Mobile.Pages;

public partial class PaymentPage : ContentPage, INotifyPropertyChanged
{
    private readonly HttpClient _httpClient;
    private const string API_BASE_URL = "https://localhost:7001/api"; // TODO: Cambiar a URL de producción

    private string _gpsStatus = "Obteniendo ubicación...";
    private string _gpsCoordinates = string.Empty;
    private double _latitude;
    private double _longitude;
    private SaleDto? _selectedSale;
    private string _paymentAmount = string.Empty;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isSaving;
    private bool _hasError;
    private bool _hasSuccess;

    public ObservableCollection<SaleDto> Sales { get; set; } = new();
    public ObservableCollection<PaymentPhoto> Photos { get; set; } = new();

    public PaymentPage()
    {
        InitializeComponent();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(API_BASE_URL)
        };
        
        BindingContext = this;
        
        SelectAmountCommand = new Command<string>(OnSelectAmount);
        CustomAmountCommand = new Command(OnCustomAmount);
        TakePhotoCommand = new Command<string>(async (type) => await OnTakePhoto(type));
        SavePaymentCommand = new Command(async () => await OnSavePayment(), () => IsNotSaving);
        CancelCommand = new Command(OnCancel);
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await GetCurrentLocation();
        await LoadSales();
    }

    // Properties
    public string GpsStatus
    {
        get => _gpsStatus;
        set
        {
            _gpsStatus = value;
            OnPropertyChanged();
        }
    }

    public string GpsCoordinates
    {
        get => _gpsCoordinates;
        set
        {
            _gpsCoordinates = value;
            OnPropertyChanged();
        }
    }

    public SaleDto? SelectedSale
    {
        get => _selectedSale;
        set
        {
            _selectedSale = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedSale));
        }
    }

    public bool HasSelectedSale => SelectedSale != null;

    public string PaymentAmount
    {
        get => _paymentAmount;
        set
        {
            _paymentAmount = value;
            OnPropertyChanged();
        }
    }

    public string Notes
    {
        get => _notes;
        set
        {
            _notes = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            _successMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            _isSaving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotSaving));
            ((Command)SavePaymentCommand).ChangeCanExecute();
        }
    }

    public bool IsNotSaving => !IsSaving;

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    public bool HasSuccess
    {
        get => _hasSuccess;
        set
        {
            _hasSuccess = value;
            OnPropertyChanged();
        }
    }

    public bool HasPhotos => Photos.Count > 0;

    // Commands
    public ICommand SelectAmountCommand { get; }
    public ICommand CustomAmountCommand { get; }
    public ICommand TakePhotoCommand { get; }
    public ICommand SavePaymentCommand { get; }
    public ICommand CancelCommand { get; }

    // Methods
    private async Task GetCurrentLocation()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            
            if (location == null)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                location = await Geolocation.GetLocationAsync(request);
            }

            if (location != null)
            {
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                GpsStatus = "✓ Ubicación obtenida";
                GpsCoordinates = $"Lat: {_latitude:F6}, Lon: {_longitude:F6}";
            }
            else
            {
                GpsStatus = "⚠ No se pudo obtener ubicación";
                GpsCoordinates = "Verifica los permisos de ubicación";
            }
        }
        catch (Exception ex)
        {
            GpsStatus = "⚠ Error al obtener ubicación";
            GpsCoordinates = ex.Message;
            Console.WriteLine($"GPS Error: {ex.Message}");
        }
    }

    private async Task LoadSales()
    {
        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "No hay sesión activa";
                HasError = true;
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("/sales/active");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var sales = JsonSerializer.Deserialize<List<SaleDto>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (sales != null)
                {
                    Sales.Clear();
                    foreach (var sale in sales)
                    {
                        Sales.Add(sale);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sales: {ex.Message}");
        }
    }

    private void OnSelectAmount(string amount)
    {
        PaymentAmount = amount;
    }

    private void OnCustomAmount()
    {
        AmountEntry.Focus();
    }

    private async Task OnTakePhoto(string photoType)
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                
                if (photo != null)
                {
                    // Guardar foto localmente
                    var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                    
                    using Stream sourceStream = await photo.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);
                    
                    await sourceStream.CopyToAsync(localFileStream);
                    
                    Photos.Add(new PaymentPhoto
                    {
                        ImagePath = localFilePath,
                        PhotoType = photoType,
                        Latitude = _latitude,
                        Longitude = _longitude
                    });
                    
                    OnPropertyChanged(nameof(HasPhotos));
                }
            }
            else
            {
                await DisplayAlert("Error", "La cámara no está disponible en este dispositivo", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al tomar foto: {ex.Message}", "OK");
        }
    }

    private async Task OnSavePayment()
    {
        // Validaciones
        if (SelectedSale == null)
        {
            ErrorMessage = "Debes seleccionar una venta";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(PaymentAmount) || !decimal.TryParse(PaymentAmount, out var amount) || amount <= 0)
        {
            Error Message = "Ingresa un monto válido";
            HasError = true;
            return;
        }

        if (Photos.Count < 2)
        {
            ErrorMessage = "Debes tomar al menos 2 fotos (Fachada y Cliente)";
            HasError = true;
            return;
        }

        IsSaving = true;
        HasError = false;
        HasSuccess = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            var userId = await SecureStorage.GetAsync("user_id");

            if (string.IsNullOrEmpty(token))
            {
                ErrorMessage = "No hay sesión activa";
                HasError = true;
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Preparar request
            var paymentRequest = new
            {
                saleId = SelectedSale.SaleId,
                amount = amount,
                collectedBy = int.Parse(userId ?? "0"),
                latitude = _latitude,
                longitude = _longitude,
                notes = Notes,
                photos = Photos.Select(p => new
                {
                    imagePath = p.ImagePath,
                    photoType = p.PhotoType,
                    latitude = p.Latitude,
                    longitude = p.Longitude
                }).ToList()
            };

            var jsonContent = JsonSerializer.Serialize(paymentRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Llamar API
            var response = await _httpClient.PostAsync("/payments", content);

            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = "✓ Pago registrado exitosamente";
                HasSuccess = true;
                
                // Limpiar formulario después de 2 segundos
                await Task.Delay(2000);
                ClearForm();
                
                await DisplayAlert("Éxito", "Pago registrado correctamente", "OK");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Error al registrar pago: {response.StatusCode}";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            HasError = true;
            Console.WriteLine($"Payment save error: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void OnCancel()
    {
        ClearForm();
    }

    private void ClearForm()
    {
        SelectedSale = null;
        PaymentAmount = string.Empty;
        Notes = string.Empty;
        Photos.Clear();
        OnPropertyChanged(nameof(HasPhotos));
        HasError = false;
        HasSuccess = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// DTOs
public class SaleDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string DisplayText => $"Venta #{SaleId} - {CustomerName} (Pendiente: ${PendingAmount:N2})";
}

public class PaymentPhoto
{
    public string ImagePath { get; set; } = string.Empty;
    public string PhotoType { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
