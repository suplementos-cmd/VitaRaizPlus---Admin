using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;
using System.Text.Json;

namespace VitaRaiz.Mobile.Pages;

[QueryProperty(nameof(EditSaleId), "saleId")]
[QueryProperty(nameof(SaleDataJson), "saleData")]
public partial class CreateSalePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private List<ZoneItem> _zones = new();
    private List<ProductItem> _products = new();

    private string? _fotoFachadaPath;
    private string? _fotoClientePath;
    private string? _fotoContratoPath;
    private string? _fotoAdicionalPath;

    private double _gpsLat;
    private double _gpsLng;
    private bool _gpsObtained;

    private int _editSaleId;
    private bool _isEditMode;
    private SaleFullDetail? _existingSaleData; // NEW: Data passed directly from DetailPage

    public int EditSaleId
    {
        get => _editSaleId;
        set { _editSaleId = value; _isEditMode = value > 0; }
    }

    // NEW: Receive JSON data to avoid API call
    private string? _saleDataJson;
    public string? SaleDataJson
    {
        get => _saleDataJson;
        set
        {
            _saleDataJson = value;
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    _existingSaleData = JsonSerializer.Deserialize<SaleFullDetail>(value);
                    System.Diagnostics.Debug.WriteLine("[CreateSale] ✅ Sale data received directly (no API call needed)");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateSale] Error deserializing sale data: {ex.Message}");
                }
            }
        }
    }

    private bool _initialized;

    public CreateSalePage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _catalogService = new CatalogService(_apiService);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;
        _initialized = true;
        await InitAsync();
    }

    private async Task InitAsync()
    {
        try
        {
            // Update page title based on mode
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LblPageTitle.Text = _isEditMode ? "  ✏ Editar Venta" : "  ✏ Alta Venta";
            });
            
            // Load catalog service cache first
            await _catalogService.LoadAsync();
            
            await Task.WhenAll(LoadCombosAsync(), GetGpsAsync());
            await CheckSupervisorAsync();
            
            if (_isEditMode)
            {
                // ⚡ OPTIMIZATION: Use direct data if available, avoid API call
                if (_existingSaleData != null)
                {
                    System.Diagnostics.Debug.WriteLine("[CreateSale] ⚡ Using existing data (FAST PATH)");
                    await PopulateSaleData(_existingSaleData);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CreateSale] ⚠️ Loading from API (SLOW PATH)");
                    await LoadSaleForEdit();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSalePage] Init error: {ex.Message}");
        }
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
    private async void OnCameraFachada(object? s, EventArgs e) => await CapturePhoto("fachada", r => { _fotoFachadaPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoFachadaPath set to: {r}"); UpdatePhotoUI(LblFotoFachada, ImgFachada, r); });
    private async void OnUploadFachada(object? s, EventArgs e) => await PickPhoto(r => { _fotoFachadaPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoFachadaPath set to: {r}"); UpdatePhotoUI(LblFotoFachada, ImgFachada, r); });
    private async void OnCameraCliente(object? s, EventArgs e) => await CapturePhoto("cliente", r => { _fotoClientePath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoClientePath set to: {r}"); UpdatePhotoUI(LblFotoCliente, ImgCliente, r); });
    private async void OnUploadCliente(object? s, EventArgs e) => await PickPhoto(r => { _fotoClientePath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoClientePath set to: {r}"); UpdatePhotoUI(LblFotoCliente, ImgCliente, r); });
    private async void OnCameraContrato(object? s, EventArgs e) => await CapturePhoto("contrato", r => { _fotoContratoPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoContratoPath set to: {r}"); UpdatePhotoUI(LblFotoContrato, ImgContrato, r); });
    private async void OnUploadContrato(object? s, EventArgs e) => await PickPhoto(r => { _fotoContratoPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoContratoPath set to: {r}"); UpdatePhotoUI(LblFotoContrato, ImgContrato, r); });
    private async void OnCameraAdicional(object? s, EventArgs e) => await CapturePhoto("adicional", r => { _fotoAdicionalPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoAdicionalPath set to: {r}"); UpdatePhotoUI(LblFotoAdicional, ImgAdicional, r); });
    private async void OnUploadAdicional(object? s, EventArgs e) => await PickPhoto(r => { _fotoAdicionalPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoAdicionalPath set to: {r}"); UpdatePhotoUI(LblFotoAdicional, ImgAdicional, r); });

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
            System.Diagnostics.Debug.WriteLine($"[CreateSale] CapturePhoto START for {name}");
            
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Error", "La cámara no está disponible", "OK");
                System.Diagnostics.Debug.WriteLine($"[CreateSale] Camera not supported");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Launching camera...");
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            
            if (photo == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSale] User cancelled camera");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Photo captured: {photo.FileName}");
            var path = await SaveFileResult(photo, name);
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Photo saved to: {path}");
            
            callback(path);
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Callback executed for {name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Camera error ({name}): {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CreateSale] StackTrace: {ex.StackTrace}");
        }
    }

    private async Task PickPhoto(Action<string?> callback)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] PickPhoto START");
            var photo = await MediaPicker.Default.PickPhotoAsync();
            
            if (photo == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSale] User cancelled pick");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Photo picked: {photo.FileName}");
            var path = await SaveFileResult(photo, "upload");
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Photo saved to: {path}");
            
            callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Upload error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CreateSale] StackTrace: {ex.StackTrace}");
        }
    }

    private static async Task<string> SaveFileResult(FileResult file, string prefix)
    {
        System.Diagnostics.Debug.WriteLine($"[CreateSale] SaveFileResult START - prefix={prefix}, fileName={file.FileName}");
        
        var appDataDir = FileSystem.AppDataDirectory;
        System.Diagnostics.Debug.WriteLine($"[CreateSale] AppDataDirectory: {appDataDir}");
        
        var filePath = Path.Combine(appDataDir, $"{prefix}_{file.FileName}");
        System.Diagnostics.Debug.WriteLine($"[CreateSale] Target file path: {filePath}");
        
        using var stream = await file.OpenReadAsync();
        System.Diagnostics.Debug.WriteLine($"[CreateSale] Opened read stream, length: {stream.Length} bytes");
        
        using var fs = File.OpenWrite(filePath);
        await stream.CopyToAsync(fs);
        
        System.Diagnostics.Debug.WriteLine($"[CreateSale] File saved successfully: {filePath}");
        System.Diagnostics.Debug.WriteLine($"[CreateSale] File.Exists check: {File.Exists(filePath)}");
        
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
                // Save photos to API and locally
                await SaveSalePhotosAsync(saleResult.SaleId);
                await UploadSalePhotosToServerAsync(saleResult.SaleId);

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

    // ═══ Save photos to local DB ═══
    private async Task SaveSalePhotosAsync(int saleId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] SaveSalePhotosAsync START for saleId={saleId}");
            
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
            System.Diagnostics.Debug.WriteLine($"[CreateSale] DB path: {dbPath}");
            
            // Verificar que el archivo de DB existe o se puede crear
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir!);
                System.Diagnostics.Debug.WriteLine($"[CreateSale] Created directory: {dbDir}");
            }

            var db = new Data.LocalDatabase(dbPath);
            System.Diagnostics.Debug.WriteLine($"[CreateSale] LocalDatabase created");

            var photoPairs = new[]
            {
                ("Fachada", _fotoFachadaPath),
                ("Cliente", _fotoClientePath),
                ("Contrato", _fotoContratoPath),
                ("Adicional", _fotoAdicionalPath)
            };

            int savedCount = 0;
            foreach (var (photoType, path) in photoPairs)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSale] Checking {photoType}: path={path ?? "null"}");
                
                if (!string.IsNullOrEmpty(path))
                {
                    bool exists = File.Exists(path);
                    System.Diagnostics.Debug.WriteLine($"[CreateSale] File.Exists({photoType}): {exists}");
                    
                    if (exists)
                    {
                        var photo = new Data.LocalSalePhoto
                        {
                            SaleId = saleId,
                            PhotoType = photoType,
                            LocalPath = path,
                            CreatedAt = DateTime.Now
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"[CreateSale] Saving {photoType} to DB...");
                        await db.SaveSalePhotoAsync(photo);
                        savedCount++;
                        System.Diagnostics.Debug.WriteLine($"[CreateSale] ✓ Saved {photoType}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CreateSale] SaveSalePhotosAsync END - Saved {savedCount} photos");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] ERROR in SaveSalePhotosAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[CreateSale] StackTrace: {ex.StackTrace}");
        }
    }

    // ═══ Upload photos to server ═══
    private async Task UploadSalePhotosToServerAsync(int saleId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] UploadSalePhotosToServerAsync START for saleId={saleId}");

            var photoPairs = new[]
            {
                ("FACHADA", _fotoFachadaPath),
                ("CLIENTE", _fotoClientePath),
                ("CONTRATO", _fotoContratoPath),
                ("ADICIONAL", _fotoAdicionalPath)
            };

            // Get GPS if available
            double? gpsLat = _gpsObtained ? _gpsLat : null;
            double? gpsLng = _gpsObtained ? _gpsLng : null;

            int uploadedCount = 0;
            foreach (var (photoType, path) in photoPairs)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        // Read file size
                        var fileInfo = new FileInfo(path);
                        var fileSize = fileInfo.Length;

                        // For now, we just store the local path
                        // In production, you would upload the actual file to a file server/cloud storage
                        // and store the URL here
                        var photoPayload = new
                        {
                            photoType,
                            filePath = path, // In production: uploaded URL
                            gpsLatitude = gpsLat,
                            gpsLongitude = gpsLng,
                            fileSize
                        };

                        var result = await _apiService.PostAsync<object>($"api/sales/{saleId}/photos", photoPayload);
                        if (result)
                        {
                            uploadedCount++;
                            System.Diagnostics.Debug.WriteLine($"[CreateSale] ✓ Uploaded {photoType} to server");
                        }
                    }
                    catch (Exception photoEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateSale] Error uploading {photoType}: {photoEx.Message}");
                        // Continue with other photos even if one fails
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CreateSale] UploadSalePhotosToServerAsync END - Uploaded {uploadedCount} photos");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] ERROR in UploadSalePhotosToServerAsync: {ex.Message}");
        }
    }

    // ═══ Edit mode: load existing sale data ═══
    private async Task LoadSaleForEdit()
    {
        try
        {
            var sale = await _apiService.GetAsync<SaleFullDetail>($"api/sales/{_editSaleId}/full");
            if (sale == null) return;
            await PopulateSaleData(sale);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Edit load error: {ex.Message}");
        }
    }

    // ═══ NEW: Populate form with sale data (used by both direct data and API loading) ═══
    private Task PopulateSaleData(SaleFullDetail sale)
    {
        return Task.Run(() =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                EntryNombre.Text = sale.CustomerName;
                EntryCelular.Text = sale.CustomerPhone;
                EditorNotas.Text = sale.Notes;

                // Parse address parts
                var addr = sale.CustomerAddress ?? "";
                if (addr.Contains(", entre "))
                {
                    var mainParts = addr.Split(", entre ");
                    EntryCalle.Text = mainParts[0];
                    var rest = mainParts.Length > 1 ? mainParts[1] : "";
                    if (rest.Contains(", ref: "))
                    {
                        var refParts = rest.Split(", ref: ");
                        var calles = refParts[0].Split(" y ");
                        EntryEntreCalle1.Text = calles.Length > 0 ? calles[0].Trim() : "";
                        EntryEntreCalle2.Text = calles.Length > 1 ? calles[1].Trim() : "";
                        EntryReferencia.Text = refParts.Length > 1 ? refParts[1] : "";
                    }
                    else
                    {
                        var calles = rest.Split(" y ");
                        EntryEntreCalle1.Text = calles.Length > 0 ? calles[0].Trim() : "";
                        EntryEntreCalle2.Text = calles.Length > 1 ? calles[1].Trim() : "";
                    }
                }
                else if (addr.Contains(", ref: "))
                {
                    var refParts = addr.Split(", ref: ");
                    EntryCalle.Text = refParts[0];
                    EntryReferencia.Text = refParts.Length > 1 ? refParts[1] : "";
                }
                else
                {
                    EntryCalle.Text = addr;
                }

                DpFechaVenta.Date = sale.SaleDate;

                // Select zone
                if (sale.ZoneName != null)
                {
                    var zoneIdx = _zones.FindIndex(z => z.ZoneName.Equals(sale.ZoneName, StringComparison.OrdinalIgnoreCase));
                    if (zoneIdx >= 0) PickerZone.SelectedIndex = zoneIdx;
                }

                // Select payment term
                var terms = sale.PaymentTerms ?? "";
                if (terms.Contains("7")) PickerFormaPago.SelectedIndex = 0;       // SEMANAL
                else if (terms.Contains("15")) PickerFormaPago.SelectedIndex = 1;  // QUINCENAL
                else if (terms.Contains("30")) PickerFormaPago.SelectedIndex = 2;  // MENSUAL
                else if (terms.Contains("0")) PickerFormaPago.SelectedIndex = 3;   // CONTADO

                // Select product (first item)
                if (sale.Items.Count > 0)
                {
                    var firstItem = sale.Items[0];
                    var prodIdx = _products.FindIndex(p => p.ProductId == firstItem.ProductId);
                    if (prodIdx >= 0)
                    {
                        PickerProduct.SelectedIndex = prodIdx;
                        LblPrice.Text = $"Precio: ${firstItem.UnitPrice:N2}";
                    }
                }

                // Parse collection day and status from notes
                var notes = sale.Notes ?? "";
                var notesParts = notes.Split('|');
                foreach (var part in notesParts)
                {
                    var p = part.Trim();
                    if (p.StartsWith("Cobro:"))
                    {
                        var day = p.Replace("Cobro:", "").Trim();
                        for (int i = 0; i < PickerDiaCobro.Items.Count; i++)
                            if (PickerDiaCobro.Items[i].Equals(day, StringComparison.OrdinalIgnoreCase))
                            { PickerDiaCobro.SelectedIndex = i; break; }
                    }
                    else if (p.StartsWith("Estatus:"))
                    {
                        var st = p.Replace("Estatus:", "").Trim();
                        for (int i = 0; i < PickerEstatus.Items.Count; i++)
                            if (PickerEstatus.Items[i].Equals(st, StringComparison.OrdinalIgnoreCase))
                            { PickerEstatus.SelectedIndex = i; break; }
                    }
                    else if (p.StartsWith("Enganche:"))
                    {
                        var eng = p.Replace("Enganche:", "").Trim();
                        EntryEnganche.Text = eng;
                    }
                }

                if (sale.CustomerGpsLatitude.HasValue && sale.CustomerGpsLongitude.HasValue)
                {
                    _gpsLat = (double)sale.CustomerGpsLatitude.Value;
                    _gpsLng = (double)sale.CustomerGpsLongitude.Value;
                    _gpsObtained = true;
                    EntryCoords.Text = $"{_gpsLat:F6}, {_gpsLng:F6}";
                    LblGpsShort.Text = $"{_gpsLat:F4}, {_gpsLng:F4}";
                }
            });
        });
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
