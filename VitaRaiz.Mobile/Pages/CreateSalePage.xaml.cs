using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class CreateSalePage : ContentPage
{
    private readonly ApiService _apiService;
    private List<ZoneItem> _zones = new();
    private List<ProductItem> _products = new();

    private string? _fotoFachadaPath;
    private string? _fotoClientePath;
    private string? _fotoContratoPath;
    private string? _fotoAdicionalPath;

    private double _gpsLat;
    private double _gpsLng;
    private bool _gpsObtained;

    public CreateSalePage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await Task.WhenAll(LoadCombosAsync(), GetGpsAsync());
        await CheckSupervisorAsync();
    }

    // ═══ Load combos from API ═══
    private async Task LoadCombosAsync()
    {
        try
        {
            var zonesTask = _apiService.GetAsync<List<ZoneItem>>("api/zones");
            var productsTask = _apiService.GetAsync<List<ProductItem>>("api/products?isActive=true");

            await Task.WhenAll(zonesTask, productsTask);

            var zones = zonesTask.Result;
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Zones loaded: {zones?.Count ?? 0}");
            if (zones != null && zones.Count > 0)
            {
                _zones = zones;
                MainThread.BeginInvokeOnMainThread(() => PickerZone.ItemsSource = _zones);
            }

            var products = productsTask.Result;
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Products loaded: {products?.Count ?? 0}");
            if (products != null && products.Count > 0)
            {
                _products = products;
                foreach (var p in _products.Take(3))
                    System.Diagnostics.Debug.WriteLine($"  Product: {p.ProductId} - {p.ProductName} - ${p.UnitPrice}");
                MainThread.BeginInvokeOnMainThread(() => PickerProduct.ItemsSource = _products);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[CreateSale] WARNING: No products returned from API!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Error loading combos: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Stack: {ex.StackTrace}");
        }
    }

    // ═══ GPS ═══
    private async Task GetGpsAsync()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            if (location != null)
            {
                _gpsLat = location.Latitude;
                _gpsLng = location.Longitude;
                _gpsObtained = true;
                EntryCoords.Text = $"{_gpsLat:F6}, {_gpsLng:F6}";
                LblGpsShort.Text = $"{_gpsLat:F4}, {_gpsLng:F4}";
            }
        }
        catch (Exception ex)
        {
            EntryCoords.Text = "GPS no disponible";
            System.Diagnostics.Debug.WriteLine($"[CreateSale] GPS error: {ex.Message}");
        }
    }

    // ═══ Supervisor check ═══
    private async Task CheckSupervisorAsync()
    {
        var role = await SecureStorage.GetAsync("role");
        if (role != null && (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase)
                          || role.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            SectionSupervisor.IsVisible = true;
        }
    }

    // ═══ Product selection ═══
    private void OnProductSelected(object? sender, EventArgs e)
    {
        if (PickerProduct.SelectedIndex >= 0 && PickerProduct.SelectedIndex < _products.Count)
        {
            var product = _products[PickerProduct.SelectedIndex];
            LblPrice.Text = $"Precio: ${product.UnitPrice:N2}";
        }
    }

    // ═══ Photos: Camera + Upload ═══
    private async void OnCameraFachada(object? s, EventArgs e) => await CapturePhoto("fachada", r => { _fotoFachadaPath = r; UpdatePhotoUI(LblFotoFachada, ImgFachada, r); });
    private async void OnUploadFachada(object? s, EventArgs e) => await PickPhoto(r => { _fotoFachadaPath = r; UpdatePhotoUI(LblFotoFachada, ImgFachada, r); });
    private async void OnCameraCliente(object? s, EventArgs e) => await CapturePhoto("cliente", r => { _fotoClientePath = r; UpdatePhotoUI(LblFotoCliente, ImgCliente, r); });
    private async void OnUploadCliente(object? s, EventArgs e) => await PickPhoto(r => { _fotoClientePath = r; UpdatePhotoUI(LblFotoCliente, ImgCliente, r); });
    private async void OnCameraContrato(object? s, EventArgs e) => await CapturePhoto("contrato", r => { _fotoContratoPath = r; UpdatePhotoUI(LblFotoContrato, ImgContrato, r); });
    private async void OnUploadContrato(object? s, EventArgs e) => await PickPhoto(r => { _fotoContratoPath = r; UpdatePhotoUI(LblFotoContrato, ImgContrato, r); });
    private async void OnCameraAdicional(object? s, EventArgs e) => await CapturePhoto("adicional", r => { _fotoAdicionalPath = r; UpdatePhotoUI(LblFotoAdicional, ImgAdicional, r); });
    private async void OnUploadAdicional(object? s, EventArgs e) => await PickPhoto(r => { _fotoAdicionalPath = r; UpdatePhotoUI(LblFotoAdicional, ImgAdicional, r); });

    private void UpdatePhotoUI(Label label, Image image, string? path)
    {
        if (path != null)
        {
            label.Text = "✓ Foto lista";
            label.TextColor = Color.FromArgb("#28A745");
            image.Source = ImageSource.FromFile(path);
            image.IsVisible = true;
            label.IsVisible = false;
        }
    }

    private async Task CapturePhoto(string name, Action<string?> callback)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Error", "La cámara no está disponible", "OK");
                return;
            }
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;
            var path = await SaveFileResult(photo, name);
            callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Camera error ({name}): {ex.Message}");
        }
    }

    private async Task PickPhoto(Action<string?> callback)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;
            var path = await SaveFileResult(photo, "upload");
            callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Upload error: {ex.Message}");
        }
    }

    private static async Task<string> SaveFileResult(FileResult file, string prefix)
    {
        var filePath = Path.Combine(FileSystem.AppDataDirectory, $"{prefix}_{file.FileName}");
        using var stream = await file.OpenReadAsync();
        using var fs = File.OpenWrite(filePath);
        await stream.CopyToAsync(fs);
        return filePath;
    }

    // ═══ Save ═══
    private async void OnSaveTapped(object? sender, EventArgs e)
    {
        var customerName = EntryNombre.Text?.Trim();
        var celular = EntryCelular.Text?.Trim();

        if (string.IsNullOrEmpty(customerName))
        {
            await DisplayAlert("Validación", "El nombre del cliente es requerido", "OK");
            return;
        }
        if (string.IsNullOrEmpty(celular))
        {
            await DisplayAlert("Validación", "El celular es requerido", "OK");
            return;
        }
        if (PickerZone.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona una zona", "OK");
            return;
        }
        if (PickerProduct.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona un producto", "OK");
            return;
        }
        if (PickerFormaPago.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona forma de pago", "OK");
            return;
        }
        if (PickerDiaCobro.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona día de cobro", "OK");
            return;
        }
        if (string.IsNullOrEmpty(_fotoFachadaPath))
        {
            await DisplayAlert("Validación", "La foto de fachada es requerida", "OK");
            return;
        }

        try
        {
            var zone = _zones[PickerZone.SelectedIndex];
            var product = _products[PickerProduct.SelectedIndex];
            var paymentTerm = PickerFormaPago.SelectedItem?.ToString() ?? "SEMANAL";
            var collectionDay = PickerDiaCobro.SelectedItem?.ToString() ?? "";
            var estatus = PickerEstatus.SelectedItem?.ToString() ?? "POR INICIAR";
            var calle = EntryCalle.Text?.Trim() ?? "";
            var entreCalle1 = EntryEntreCalle1.Text?.Trim() ?? "";
            var entreCalle2 = EntryEntreCalle2.Text?.Trim() ?? "";
            var referencia = EntryReferencia.Text?.Trim() ?? "";
            var notes = EditorNotas.Text?.Trim() ?? "";
            decimal.TryParse(EntryEnganche.Text, out decimal enganche);

            // Build address from fields
            var address = calle;
            if (!string.IsNullOrEmpty(entreCalle1) || !string.IsNullOrEmpty(entreCalle2))
                address += $", entre {entreCalle1} y {entreCalle2}";
            if (!string.IsNullOrEmpty(referencia))
                address += $", ref: {referencia}";

            var userIdStr = await SecureStorage.GetAsync("user_id");
            int.TryParse(userIdStr, out int userId);

            // 1. Create customer
            var customerPayload = new
            {
                customerName,
                phoneNumber = celular,
                address,
                zoneId = zone.ZoneId,
                gpsLatitude = _gpsObtained ? _gpsLat.ToString("F6") : (string?)null,
                gpsLongitude = _gpsObtained ? _gpsLng.ToString("F6") : (string?)null,
                isGoldCustomer = false,
                isBlacklisted = false
            };

            var customerResult = await _apiService.PostAsync<object, CreateCustomerResponse>("api/customers", customerPayload);
            if (customerResult == null || customerResult.CustomerId <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSale] Customer creation failed. Result is null: {customerResult == null}");
                await DisplayAlert("Error", "No se pudo registrar el cliente. Revisa la conexión con el servidor.", "OK");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Customer created OK. ID: {customerResult.CustomerId}");

            // 2. Create sale
            int paymentTermDays = paymentTerm switch
            {
                "SEMANAL" => 7,
                "QUINCENAL" => 15,
                "MENSUAL" => 30,
                "CONTADO" => 0,
                _ => 7
            };

            var salePayload = new
            {
                customerId = customerResult.CustomerId,
                sellerId = userId > 0 ? userId : 1,
                paymentTermDays,
                notes = $"Estatus: {estatus} | Cobro: {collectionDay} | FechaPrimerCobro: {DpPrimerCobro.Date:dd/MM/yyyy} | Enganche: {enganche:N2} | {notes}",
                details = new[]
                {
                    new
                    {
                        productId = product.ProductId,
                        quantity = 1,
                        unitPrice = product.UnitPrice,
                        subtotal = product.UnitPrice
                    }
                }
            };

            var saleResult = await _apiService.PostAsync<object, CreateSaleResponse>("api/sales", salePayload);
            if (saleResult != null && saleResult.SaleId > 0)
            {
                await DisplayAlert("✅ Venta Registrada",
                    $"Venta #{saleResult.SaleId}\nCliente: {customerName}\nProducto: {product.ProductName}\nTotal: ${product.UnitPrice:N2}",
                    "OK");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo crear la venta", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Save error: {ex.Message}");
            await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "OK");
        }
    }

    // ═══ Navigation ═══
    private async void OnBackTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private async void OnCancelTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}

// ═══ Local DTOs ═══
public class ZoneItem
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
}

public class ProductItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public class CreateCustomerResponse
{
    public int CustomerId { get; set; }
}

public class CreateSaleResponse
{
    public int SaleId { get; set; }
    public string? Message { get; set; }
}
