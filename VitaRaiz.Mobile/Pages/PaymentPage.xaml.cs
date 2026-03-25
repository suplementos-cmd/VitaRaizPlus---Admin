using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class PaymentPage : ContentPage
{
    private readonly ApiService _apiService;

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
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Inicializando PaymentPage ===");
            InitializeComponent();
            
            _apiService = new ApiService();
            
            BindingContext = this;
            
            SelectAmountCommand = new Command<string>(OnSelectAmount);
            CustomAmountCommand = new Command(OnCustomAmount);
            TakePhotoCommand = new Command<string>(async (type) => await OnTakePhoto(type));
            SavePaymentCommand = new Command(async () => await OnSavePayment(), () => IsNotSaving);
            CancelCommand = new Command(OnCancel);
            
            _ = InitializeAsync();
            System.Diagnostics.Debug.WriteLine("=== PaymentPage inicializado correctamente ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en PaymentPage constructor: {ex.Message}");
        }
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
            System.Diagnostics.Debug.WriteLine("PaymentPage: Cargando ventas activas desde API...");
            // Cargar ventas activas desde la API
            var salesData = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("api/sales/active");

            System.Diagnostics.Debug.WriteLine($"PaymentPage: Recibidas {salesData?.Count ?? 0} ventas activas");

            if (salesData != null)
            {
                Sales.Clear();
                foreach (var sale in salesData)
                {
                    Sales.Add(new SaleDto
                    {
                        SaleId = sale.SaleId,
                        CustomerName = sale.CustomerName,
                        TotalAmount = sale.TotalAmount,
                        PendingAmount = sale.Balance
                    });
                }
                System.Diagnostics.Debug.WriteLine($"PaymentPage: Mostrando {Sales.Count} ventas");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PaymentPage: No se recibieron datos (null)");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentPage: Error loading sales: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            ErrorMessage = "Error al cargar ventas";
            HasError = true;
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
            ErrorMessage = "Ingresa un monto válido";
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
            var userId = await SecureStorage.GetAsync("user_id");

            // Crear MultipartFormDataContent para enviar fotos
            var multipartContent = new MultipartFormDataContent();

            // Agregar datos del pago
            multipartContent.Add(new StringContent(SelectedSale.SaleId.ToString()), "saleId");
            multipartContent.Add(new StringContent(amount.ToString()), "amount");
            multipartContent.Add(new StringContent(userId ?? "0"), "collectorId");
            multipartContent.Add(new StringContent(_latitude.ToString()), "gpsLatitude");
            multipartContent.Add(new StringContent(_longitude.ToString()), "gpsLongitude");
            multipartContent.Add(new StringContent(Notes ?? ""), "notes");

            // Agregar fotos
            int photoIndex = 0;
            foreach (var photo in Photos)
            {
                if (File.Exists(photo.ImagePath))
                {
                    var fileBytes = await File.ReadAllBytesAsync(photo.ImagePath);
                    var imageContent = new ByteArrayContent(fileBytes);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    
                    multipartContent.Add(imageContent, $"photos", $"photo_{photoIndex}_{photo.PhotoType}.jpg");
                    multipartContent.Add(new StringContent(photo.PhotoType), $"photoTypes[{photoIndex}]");
                    multipartContent.Add(new StringContent(photo.Latitude.ToString()), $"photoLatitudes[{photoIndex}]");
                    multipartContent.Add(new StringContent(photo.Longitude.ToString()), $"photoLongitudes[{photoIndex}]");
                    
                    photoIndex++;
                }
            }

            // Enviar a la API (si hay fotos usar multipart, sino JSON simple)
            bool success;
            if (Photos.Count > 0)
            {
                success = await _apiService.PostMultipartAsync("api/payments", multipartContent);
            }
            else
            {
                // Fallback sin fotos
                var paymentData = new
                {
                    saleId = SelectedSale.SaleId,
                    amount = amount,
                    collectorId = int.Parse(userId ?? "0"),
                    gpsLatitude = _latitude,
                    gpsLongitude = _longitude,
                    notes = Notes
                };
                success = await _apiService.PostAsync("api/payments", paymentData);
            }

            if (success)
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
                ErrorMessage = "Error al registrar el pago. Intenta nuevamente.";
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

