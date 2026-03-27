using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;
using NLog;

namespace VitaRaiz.Mobile.Pages;

public partial class PaymentPage : ContentPage
{
    private readonly Logger _logger = AppLogger.Get();
    private readonly ApiService _apiService;

    private string _gpsStatus = "Obteniendo ubicación...";
    private string _gpsCoordinates = string.Empty;
    private double _latitude;
    private double _longitude;
    private PaymentSaleItem? _selectedSale;
    private string _paymentAmount = string.Empty;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isSaving;
    private bool _hasError;
    private bool _hasSuccess;

    public ObservableCollection<PaymentSaleItem> Sales { get; set; } = new();
    public ObservableCollection<PaymentPhoto> Photos { get; set; } = new();

    public PaymentPage()
    {
        try
        {
            _logger.Info("═══════════════════════════════════════════════════════");
            _logger.Info("[Constructor] Inicializando PaymentPage...");
            InitializeComponent();
            
            _apiService = new ApiService();
            
            BindingContext = this;
            
            SelectAmountCommand = new Command<string>(OnSelectAmount);
            CustomAmountCommand = new Command(OnCustomAmount);
            TakePhotoCommand = new Command<string>(async (type) => await OnTakePhoto(type));
            SavePaymentCommand = new Command(async () => await OnSavePayment(), () => IsNotSaving);
            CancelCommand = new Command(OnCancel);
            
            _ = InitializeAsync();
            _logger.Info("[Constructor] PaymentPage inicializado correctamente");
            _logger.Info("═══════════════════════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error crítico en constructor PaymentPage");
            throw;
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

    public PaymentSaleItem? SelectedSale
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
            _logger.Info("[GetCurrentLocation] INICIO - Obteniendo ubicación GPS...");
            
            var location = await Geolocation.GetLastKnownLocationAsync();
            _logger.Debug("[GetCurrentLocation] LastKnownLocation: {IsNull}", location == null ? "NULL" : "OK");
            
            if (location == null)
            {
                _logger.Info("[GetCurrentLocation] No hay ubicación conocida. Solicitando ubicación actual...");
                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                location = await Geolocation.GetLocationAsync(request);
            }

            if (location != null)
            {
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                GpsStatus = "✓ Ubicación obtenida";
                GpsCoordinates = $"Lat: {_latitude:F6}, Lon: {_longitude:F6}";
                _logger.Info("[GetCurrentLocation] FIN - Ubicación obtenida: Lat={Lat}, Lon={Lon}", _latitude, _longitude);
            }
            else
            {
                GpsStatus = "⚠ No se pudo obtener ubicación";
                GpsCoordinates = "Verifica los permisos de ubicación";
                _logger.Warn("[GetCurrentLocation] No se pudo obtener ubicación - Location es NULL");
            }
        }
        catch (Exception ex)
        {
            GpsStatus = "⚠ Error al obtener ubicación";
            GpsCoordinates = ex.Message;
            _logger.LogException(ex, "Error al obtener ubicación GPS");
        }
    }

    private async Task LoadSales()
    {
        try
        {
            _logger.Info("[LoadSales] INICIO - Cargando ventas activas desde API...");
            
            // Cargar ventas activas desde la API
            var salesData = await _apiService.GetAsync<List<VitaRaiz.Mobile.Services.SaleDto>>("api/sales/active");

            _logger.Info("[LoadSales] API Response: Count={Count}, IsNull={IsNull}", 
                salesData?.Count ?? 0, salesData == null);

            if (salesData != null)
            {
                var items = salesData.Select(sale => new PaymentSaleItem
                {
                    SaleId = sale.SaleId,
                    CustomerName = sale.CustomerName,
                    TotalAmount = sale.TotalAmount,
                    PendingAmount = sale.Balance
                }).ToList();
                
                _logger.Debug("[LoadSales] {Count} items preparados para UI. Actualizando en MainThread...", items.Count);
                
                // CRITICAL: Modify ObservableCollection only on UI thread to prevent crash 0xc000027b
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        _logger.Debug("[LoadSales] En UI Thread. Limpiando Sales ObservableCollection...");
                        Sales.Clear();
                        
                        _logger.Debug("[LoadSales] Agregando {Count} ventas a Sales...", items.Count);
                        foreach (var item in items)
                            Sales.Add(item);
                        
                        _logger.Info("[LoadSales] FIN - {Count} ventas activas cargadas en UI", Sales.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex, "CRASH al actualizar ObservableCollection Sales en UI thread");
                        throw;
                    }
                });
            }
            else
            {
                _logger.Warn("[LoadSales] API devolvió null - No hay ventas activas");
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error crítico al cargar ventas desde API");
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
            _logger.Info("[OnTakePhoto] INICIO - Tipo: {PhotoType}", photoType);
            
            if (MediaPicker.Default.IsCaptureSupported)
            {
                _logger.Debug("[OnTakePhoto] Cámara soportada. Capturando foto...");
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                
                if (photo != null)
                {
                    _logger.Debug("[OnTakePhoto] Foto capturada: {FileName}", photo.FileName);
                    
                    // Guardar foto localmente
                    var localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                    _logger.Debug("[OnTakePhoto] Guardando en: {Path}", localFilePath);
                    
                    using Stream sourceStream = await photo.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);
                    
                    await sourceStream.CopyToAsync(localFileStream);
                    _logger.Info("[OnTakePhoto] Foto guardada exitosamente");
                    
                    var newPhoto = new PaymentPhoto
                    {
                        ImagePath = localFilePath,
                        PhotoType = photoType,
                        Latitude = _latitude,
                        Longitude = _longitude
                    };
                    
                    // CRITICAL: Modify ObservableCollection only on UI thread to prevent crash 0xc000027b
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            _logger.Debug("[OnTakePhoto] Agregando foto a Photos ObservableCollection...");
                            Photos.Add(newPhoto);
                            OnPropertyChanged(nameof(HasPhotos));
                            _logger.Info("[OnTakePhoto] FIN - Total fotos: {Count}", Photos.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogException(ex, "CRASH al agregar foto a ObservableCollection");
                            throw;
                        }
                    });
                }
                else
                {
                    _logger.Warn("[OnTakePhoto] Usuario canceló captura de foto");
                }
            }
            else
            {
                _logger.Error("[OnTakePhoto] Cámara NO soportada en este dispositivo");
                await DisplayAlert("Error", "La cámara no está disponible en este dispositivo", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error crítico al tomar foto");
            await DisplayAlert("Error", $"Error al tomar foto: {ex.Message}", "OK");
        }
    }

    private async Task OnSavePayment()
    {
        _logger.Info("════════════════════════════════════════");
        _logger.Info("[OnSavePayment] INICIO - Guardando pago...");
        
        // Validaciones
        if (SelectedSale == null)
        {
            _logger.Warn("[OnSavePayment] Validación fallida: No hay venta seleccionada");
            ErrorMessage = "Debes seleccionar una venta";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(PaymentAmount) || !decimal.TryParse(PaymentAmount, out var amount) || amount <= 0)
        {
            _logger.Warn("[OnSavePayment] Validación fallida: Monto inválido: '{Amount}'", PaymentAmount);
            ErrorMessage = "Ingresa un monto válido";
            HasError = true;
            return;
        }

        if (Photos.Count < 2)
        {
            _logger.Warn("[OnSavePayment] Validación fallida: Fotos insuficientes (Count={Count}, Requerido=2)", Photos.Count);
            ErrorMessage = "Debes tomar al menos 2 fotos (Fachada y Cliente)";
            HasError = true;
            return;
        }

        _logger.Info("[OnSavePayment] Validaciones OK: SaleId={SaleId}, Amount={Amount}, Photos={PhotoCount}",
            SelectedSale.SaleId, amount, Photos.Count);

        IsSaving = true;
        HasError = false;
        HasSuccess = false;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        try
        {
            var userId = await SecureStorage.GetAsync("user_id");
            _logger.Debug("[OnSavePayment] UserId desde SecureStorage: {UserId}", userId ?? "NULL");

            _logger.Debug("[OnSavePayment] Construyendo MultipartFormDataContent...");
            
            // Crear MultipartFormDataContent para enviar fotos
            var multipartContent = new MultipartFormDataContent();

            // Agregar datos del pago
            multipartContent.Add(new StringContent(SelectedSale.SaleId.ToString()), "saleId");
            multipartContent.Add(new StringContent(amount.ToString()), "amount");
            multipartContent.Add(new StringContent(userId ?? "0"), "collectorId");
            multipartContent.Add(new StringContent(_latitude.ToString()), "gpsLatitude");
            multipartContent.Add(new StringContent(_longitude.ToString()), "gpsLongitude");
            multipartContent.Add(new StringContent(Notes ?? ""), "notes");
            
            _logger.Debug("[OnSavePayment] Datos agregados. Procesando {Count} fotos...", Photos.Count);

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
                    
                    _logger.Debug("[OnSavePayment] Foto {Index}: Type={Type}, Size={Size} bytes",
                        photoIndex, photo.PhotoType, fileBytes.Length);
                    
                    photoIndex++;
                }
                else
                {
                    _logger.Warn("[OnSavePayment] Foto no existe en ruta: {Path}", photo.ImagePath);
                }
            }

            _logger.Info("[OnSavePayment] Enviando a API: POST /api/payments (con {Count} fotos)", photoIndex);
            
            // Enviar a la API (si hay fotos usar multipart, sino JSON simple)
            bool success;
            if (Photos.Count > 0)
            {
                success = await _apiService.PostMultipartAsync("api/payments", multipartContent);
            }
            else
            {
                _logger.Debug("[OnSavePayment] Fallback: Enviando sin fotos (JSON)");
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
                _logger.Info("[OnSavePayment] ✓ PAGO GUARDADO EXITOSAMENTE");
                SuccessMessage = "✓ Pago registrado exitosamente";
                HasSuccess = true;
                
                // Limpiar formulario después de 2 segundos
                await Task.Delay(2000);
                ClearForm();
                
                await DisplayAlert("Éxito", "Pago registrado correctamente", "OK");
            }
            else
            {
                _logger.Error("[OnSavePayment] API devolvió false - Error al registrar pago");
                ErrorMessage = "Error al registrar el pago. Intenta nuevamente.";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error CRÍTICO al guardar pago");
            ErrorMessage = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
            _logger.Info("[OnSavePayment] FIN");
            _logger.Info("════════════════════════════════════════");
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

    // ═══ Bottom Tab Navigation ═══
    private async void OnTabInicio(object? s, EventArgs e) => await Shell.Current.GoToAsync("//HomePage");
    private async void OnTabVentas(object? s, EventArgs e) => await Shell.Current.GoToAsync("//SalesPage");
    private async void OnTabClientes(object? s, EventArgs e) => await Shell.Current.GoToAsync("//CustomersPage");
}

// DTOs
public class PaymentSaleItem
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

