using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VitaRaiz.Mobile.Pages;

[QueryProperty(nameof(EditSaleId), "saleId")]
public partial class CreateSalePage : ContentPage, INotifyPropertyChanged
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
    private Models.SaleFullDetail? _cachedOriginalSale;
    
    // Catálogos de API
    public System.Collections.ObjectModel.ObservableCollection<Models.CatalogSaleStatus> SaleStatuses { get; } = new();
    
    private Models.CatalogSaleStatus? _selectedSaleStatus;
    public Models.CatalogSaleStatus? SelectedSaleStatus 
    {
        get => _selectedSaleStatus;
        set { _selectedSaleStatus = value; OnPropertyChanged(); }
    }

    public int EditSaleId
    {
        get => _editSaleId;
        set { _editSaleId = value; _isEditMode = value > 0; }
    }

    private bool _initialized;

    // Day-chip references — populated after InitializeComponent
    private Border[] _dayChipBorders = Array.Empty<Border>();
    private Label[]  _dayChipLabels  = Array.Empty<Label>();

    // Payment-term chip references
    private Border[] _payChipBorders = Array.Empty<Border>();
    private Label[]  _payChipLabels  = Array.Empty<Label>();

    public CreateSalePage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _catalogService = new CatalogService(_apiService);
        BindingContext = this;
        InitDayChips();
        InitPayChips();
    }

    private void InitDayChips()
    {
        _dayChipBorders = [ChipLun, ChipMar, ChipMie, ChipJue, ChipVie];
        _dayChipLabels  = [LblChipLun, LblChipMar, LblChipMie, LblChipJue, LblChipVie];
    }

    private void InitPayChips()
    {
        _payChipBorders = [ChipSemanal, ChipContado];
        _payChipLabels  = [LblChipSemanal, LblChipContado];
    }

    private void OnFormaPago_SelectedIndexChanged(object sender, EventArgs e)
    {
        SyncPayChipVisuals(PickerFormaPago.SelectedIndex);
        if (PickerFormaPago.SelectedIndex >= 0) ErrFormaPago.IsVisible = false;
    }

    private void OnFormaPagoChipTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string s && int.TryParse(s, out int idx))
            PickerFormaPago.SelectedIndex = idx;
    }

    private void SyncPayChipVisuals(int selectedIndex)
    {
        object? selBgObj = null;
        Application.Current?.Resources.TryGetValue("ThemeColor", out selBgObj);
        var selBg     = selBgObj is Color s ? s : Color.FromArgb("#32B864");
        var unselBg   = Colors.Transparent;
        var unselBorder = Color.FromArgb("#C8D4CC");
        var unselText = Color.FromArgb("#6E7D75");

        for (int i = 0; i < _payChipBorders.Length; i++)
        {
            bool active = i == selectedIndex;
            _payChipBorders[i].BackgroundColor = active ? selBg : unselBg;
            _payChipBorders[i].Stroke          = active ? new SolidColorBrush(selBg) : new SolidColorBrush(unselBorder);
            _payChipBorders[i].StrokeThickness = 1.5;
            _payChipBorders[i].Shadow = active
                ? new Shadow { Brush = new SolidColorBrush(selBg), Offset = new Point(0, 3), Radius = 8, Opacity = 0.35f }
                : null;
            _payChipLabels[i].TextColor      = active ? Colors.White : unselText;
            _payChipLabels[i].FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    // Fired when hidden PickerDiaCobro selection changes — keeps chips in sync
    private void OnDiaCobro_SelectedIndexChanged(object sender, EventArgs e)
    {
        SyncDayChipVisuals(PickerDiaCobro.SelectedIndex);
        if (PickerDiaCobro.SelectedIndex >= 0) ErrDiaCobro.IsVisible = false;
    }

    // Chip tap: update the hidden picker, which in turn fires SelectedIndexChanged
    private void OnDayChipTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string s && int.TryParse(s, out int idx))
            PickerDiaCobro.SelectedIndex = idx;
    }

    private void SyncDayChipVisuals(int selectedIndex)
    {
        object? selBgObj = null;
        Application.Current?.Resources.TryGetValue("ThemeColor", out selBgObj);
        var selBg       = selBgObj is Color s ? s : Color.FromArgb("#32B864");
        var unselBg     = Colors.Transparent;
        var unselBorder = Color.FromArgb("#C8D4CC");
        var unselText   = Color.FromArgb("#6E7D75");

        for (int i = 0; i < _dayChipBorders.Length; i++)
        {
            bool active = i == selectedIndex;
            _dayChipBorders[i].BackgroundColor = active ? selBg : unselBg;
            _dayChipBorders[i].Stroke          = active ? new SolidColorBrush(selBg) : new SolidColorBrush(unselBorder);
            _dayChipBorders[i].StrokeThickness = 1.5;
            _dayChipBorders[i].Shadow = active
                ? new Shadow { Brush = new SolidColorBrush(selBg), Offset = new Point(0, 3), Radius = 8, Opacity = 0.35f }
                : null;
            _dayChipLabels[i].TextColor      = active ? Colors.White : unselText;
            _dayChipLabels[i].FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        }
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
                PageHeader.Title = _isEditMode ? "  ✏ Editar Venta" : "  ✏ Alta Venta";
            });
            
            // Load catalog service cache first
            await _catalogService.LoadAsync();
            
            // Populate SaleStatuses from catalog
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SaleStatuses.Clear();
                foreach (var status in _catalogService.SaleStatuses)
                    SaleStatuses.Add(status);
                // Default: prefer "pending", fall back to first available status
                SelectedSaleStatus = SaleStatuses.FirstOrDefault(s =>
                    s.StatusCode.Equals("pending", StringComparison.OrdinalIgnoreCase))
                    ?? SaleStatuses.FirstOrDefault();
            });
            
            await Task.WhenAll(LoadCombosAsync(), _isEditMode ? Task.CompletedTask : GetGpsAsync());
            await CheckSupervisorAsync();
            
            if (_isEditMode)
            {
                // ⚡ FAST PATH: consume in-memory cache passed from SaleDetailPage
                var cached = Services.PageDataCache.PendingEditSale;
                if (cached != null && cached.SaleId == _editSaleId)
                {
                    _cachedOriginalSale = cached;            // keep reference for OnSaveTapped
                    Services.PageDataCache.PendingEditSale = null;
                    System.Diagnostics.Debug.WriteLine("[CreateSale] ⚡ Using PageDataCache (zero serialization)");
                    await PopulateSaleData(cached);
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
    private void OnZoneSelected(object? sender, EventArgs e)
    {
        if (PickerZone.SelectedIndex >= 0) ErrZona.IsVisible = false;
    }

    private void OnProductSelected(object? sender, EventArgs e)
    {
        if (PickerProduct.SelectedIndex >= 0 && PickerProduct.SelectedIndex < _products.Count)
        {
            var product = _products[PickerProduct.SelectedIndex];
            LblPrice.Text = $"Precio: ${product.UnitPrice:N2}";
            ErrProducto.IsVisible = false;
        }
    }

    // ═══ Photos: Camera + Upload ═══
    private async void OnCameraFachada(object? s, EventArgs e) => await CapturePhoto("fachada", r => { _fotoFachadaPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoFachadaPath set to: {r}"); UpdatePhotoUI(LblFotoFachada, ImgFachada, BtnClearFachada, r); });
    private async void OnUploadFachada(object? s, EventArgs e) => await PickPhoto(r => { _fotoFachadaPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoFachadaPath set to: {r}"); UpdatePhotoUI(LblFotoFachada, ImgFachada, BtnClearFachada, r); });
    private async void OnCameraCliente(object? s, EventArgs e) => await CapturePhoto("cliente", r => { _fotoClientePath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoClientePath set to: {r}"); UpdatePhotoUI(LblFotoCliente, ImgCliente, BtnClearCliente, r); });
    private async void OnUploadCliente(object? s, EventArgs e) => await PickPhoto(r => { _fotoClientePath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoClientePath set to: {r}"); UpdatePhotoUI(LblFotoCliente, ImgCliente, BtnClearCliente, r); });
    private async void OnCameraContrato(object? s, EventArgs e) => await CapturePhoto("contrato", r => { _fotoContratoPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoContratoPath set to: {r}"); UpdatePhotoUI(LblFotoContrato, ImgContrato, BtnClearContrato, r); });
    private async void OnUploadContrato(object? s, EventArgs e) => await PickPhoto(r => { _fotoContratoPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoContratoPath set to: {r}"); UpdatePhotoUI(LblFotoContrato, ImgContrato, BtnClearContrato, r); });
    private async void OnCameraAdicional(object? s, EventArgs e) => await CapturePhoto("adicional", r => { _fotoAdicionalPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoAdicionalPath set to: {r}"); UpdatePhotoUI(LblFotoAdicional, ImgAdicional, BtnClearAdicional, r); });
    private async void OnUploadAdicional(object? s, EventArgs e) => await PickPhoto(r => { _fotoAdicionalPath = r; System.Diagnostics.Debug.WriteLine($"[CreateSale] _fotoAdicionalPath set to: {r}"); UpdatePhotoUI(LblFotoAdicional, ImgAdicional, BtnClearAdicional, r); });

    private void UpdatePhotoUI(Label label, Image image, Border clearBtn, string? path)
    {
        if (path != null)
        {
            label.Text = "✓ Foto lista";
            label.TextColor = Color.FromArgb("#28A745");
            image.Source = ImageSource.FromFile(path);
            image.IsVisible = true;
            label.IsVisible = false;
            clearBtn.IsVisible = true;
        }
    }

    private void ClearPhotoUI(Label label, Image image, Border clearBtn)
    {
        image.Source = null;
        image.IsVisible = false;
        label.Text = "Sin foto";
        label.TextColor = Color.FromArgb("#9CACA4");
        label.IsVisible = true;
        clearBtn.IsVisible = false;
    }

    private void OnClearFoto(object sender, TappedEventArgs e)
    {
        switch (e.Parameter as string)
        {
            case "fachada":   _fotoFachadaPath  = null; ClearPhotoUI(LblFotoFachada,  ImgFachada,  BtnClearFachada);  break;
            case "cliente":   _fotoClientePath   = null; ClearPhotoUI(LblFotoCliente,  ImgCliente,  BtnClearCliente);  break;
            case "contrato":  _fotoContratoPath  = null; ClearPhotoUI(LblFotoContrato, ImgContrato, BtnClearContrato); break;
            case "adicional": _fotoAdicionalPath = null; ClearPhotoUI(LblFotoAdicional,ImgAdicional,BtnClearAdicional); break;
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

    private void OnNombreTextChanged(object sender, TextChangedEventArgs e)
    {
        var upper = e.NewTextValue?.ToUpperInvariant();
        if (upper != null && upper != e.NewTextValue)
        {
            EntryNombre.Text = upper;
            EntryNombre.CursorPosition = upper.Length;
        }
        if (!string.IsNullOrEmpty(e.NewTextValue))
        {
            ErrNombre.IsVisible = false;
            SetFieldBorderError(BorderNombre, false);
        }
    }

    // ── Field border focus glow ──
    private void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            var themeColor = Application.Current?.Resources.TryGetValue("ThemeColor", out var c) == true
                ? (Color)c : Color.FromArgb("#32B864");
            border.Stroke = new SolidColorBrush(themeColor);
            border.StrokeThickness = 1.5;
            border.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(themeColor),
                Offset = new Point(0, 0),
                Radius = 6,
                Opacity = 0.18f
            };
        }
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.Parent is Border border)
        {
            // Restore to the theme-tinted border (ThemeColorLight from global resources)
            var themeLight = Application.Current?.Resources.TryGetValue("ThemeColorLight", out var c) == true
                ? (Color)c : Color.FromArgb("#C8D4CC");
            border.Stroke = new SolidColorBrush(themeLight);
            border.StrokeThickness = 1;
            border.Shadow = null;
        }
    }

    // ── Input filters ──
    private void OnCelularTextChanged(object sender, TextChangedEventArgs e)
    {
        var filtered = new string((e.NewTextValue ?? "").Where(char.IsDigit).ToArray());
        if (filtered != e.NewTextValue)
        {
            ((Entry)sender).Text = filtered;
            return;
        }
        if (filtered.Length == 10)
        {
            ErrCelular.IsVisible = false;
            SetFieldBorderError(BorderCelular, false);
        }
        else if (filtered.Length > 0)
        {
            SetFieldBorderError(BorderCelular, false); // partial input: no red
        }
    }

    private void OnEngancheTextChanged(object sender, TextChangedEventArgs e)
    {
        var raw = e.NewTextValue ?? "";
        var digits = raw.Where(c => char.IsDigit(c) || c == '.').ToArray();
        var filtered = new string(digits);
        var dotIdx = filtered.IndexOf('.');
        if (dotIdx >= 0)
            filtered = filtered[..(dotIdx + 1)] + new string(filtered[(dotIdx + 1)..].Where(char.IsDigit).ToArray());
        if (filtered != raw)
            ((Entry)sender).Text = filtered;
    }

    // ── Error border helper ──
    private void SetFieldBorderError(Border border, bool hasError)
    {
        if (hasError)
        {
            border.Stroke = new SolidColorBrush(Color.FromArgb("#C62828"));
            border.StrokeThickness = 1.5;
        }
        else
        {
            var themeLight = Application.Current?.Resources.TryGetValue("ThemeColorLight", out var c) == true
                ? (Color)c : Color.FromArgb("#C8D4CC");
            border.Stroke = new SolidColorBrush(themeLight);
            border.StrokeThickness = 1;
        }
    }

    // ═══ Save ═══
    private async void OnSaveTapped(object? sender, EventArgs e)
    {
        var customerName = EntryNombre.Text?.Trim();
        var celular = EntryCelular.Text?.Trim();

        // ── inline field validation ──
        ErrNombre.IsVisible    = string.IsNullOrEmpty(customerName);
        ErrCelular.IsVisible   = string.IsNullOrEmpty(celular) || celular?.Length < 10;
        ErrZona.IsVisible      = PickerZone.SelectedIndex < 0;
        ErrProducto.IsVisible  = PickerProduct.SelectedIndex < 0;
        ErrFormaPago.IsVisible = PickerFormaPago.SelectedIndex < 0;
        ErrDiaCobro.IsVisible  = PickerDiaCobro.SelectedIndex < 0;
        ErrFachada.IsVisible   = !_isEditMode && string.IsNullOrEmpty(_fotoFachadaPath);

        SetFieldBorderError(BorderNombre,  ErrNombre.IsVisible);
        SetFieldBorderError(BorderCelular, ErrCelular.IsVisible);

        // scroll/focus to first error
        VisualElement? firstError =
            ErrNombre.IsVisible    ? EntryNombre   :
            ErrCelular.IsVisible   ? EntryCelular  :
            ErrZona.IsVisible      ? (VisualElement)ErrZona      :
            ErrProducto.IsVisible  ? ErrProducto   :
            ErrFormaPago.IsVisible ? ErrFormaPago  :
            ErrDiaCobro.IsVisible  ? ErrDiaCobro   :
            ErrFachada.IsVisible   ? ErrFachada     :
            null;

        if (firstError != null)
        {
            await Task.Delay(60); // let UI render error labels
            if (firstError is Entry entry)
                entry.Focus();
            else
                await MainScrollView.ScrollToAsync(firstError, ScrollToPosition.MakeVisible, true);
            return;
        }

        try
        {
            var zone = _zones[PickerZone.SelectedIndex];
            var product = _products[PickerProduct.SelectedIndex];
            var paymentTerm = PickerFormaPago.SelectedItem?.ToString() ?? "SEMANAL";
            var collectionDay = PickerDiaCobro.SelectedItem?.ToString() ?? "";
            var estatus = SelectedSaleStatus?.StatusCode ?? "pending";
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

            int paymentTermDays = paymentTerm switch
            {
                "SEMANAL" => 7,
                "QUINCENAL" => 15,
                "MENSUAL" => 30,
                "CONTADO" => 0,
                _ => 7
            };

            if (_isEditMode)
            {
                // ── UPDATE path ──
                var updatePayload = new
                {
                    customerId = _cachedOriginalSale?.CustomerId ?? 0,
                    customerName,
                    phoneNumber = celular,
                    address,
                    zoneId = zone.ZoneId,
                    paymentTermDays,
                    paymentTerm,
                    collectionDay,
                    firstCollectionDate = DpPrimerCobro.Date,
                    downPayment = enganche,
                    notes,
                    status = estatus,
                    saleDate = DpFechaVenta.Date
                };

                var ok = await _apiService.PutAsync<object>($"api/sales/{_editSaleId}", updatePayload);
                if (ok)
                {
                    await SaveSalePhotosAsync(_editSaleId);
                    await UploadSalePhotosToServerAsync(_editSaleId);

                    // Build an optimistic SaleFullDetail so SaleDetailPage can update without an API round-trip
                    if (_cachedOriginalSale != null)
                    {
                        _cachedOriginalSale.CustomerName = customerName;
                        _cachedOriginalSale.CustomerPhone = celular;
                        _cachedOriginalSale.CustomerAddress = address;
                        _cachedOriginalSale.ZoneName = zone.ZoneName;
                        _cachedOriginalSale.PaymentTerm = paymentTerm;
                        _cachedOriginalSale.CollectionDay = collectionDay;
                        _cachedOriginalSale.FirstCollectionDate = DpPrimerCobro.Date;
                        _cachedOriginalSale.DownPayment = enganche;
                        _cachedOriginalSale.Notes = notes;
                        _cachedOriginalSale.Status = estatus;
                        _cachedOriginalSale.SaleDate = DpFechaVenta.Date;
                        if (_cachedOriginalSale.Items.Count > 0)
                        {
                            _cachedOriginalSale.Items[0].ProductId = product.ProductId;
                            _cachedOriginalSale.Items[0].ProductName = product.ProductName;
                            _cachedOriginalSale.Items[0].UnitPrice = product.UnitPrice;
                        }
                        Services.PageDataCache.RefreshedSaleDetail = _cachedOriginalSale;
                    }

                    Services.PageDataCache.ForceRefreshSalesList = true;

                    await Shell.Current.GoToAsync("..");
                    ShowToast($"✅ Venta #{_editSaleId} actualizada");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo actualizar la venta", "OK");
                }
            }
            else
            {
                // ── CREATE path ──
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

                var salePayload = new
                {
                    customerId = customerResult.CustomerId,
                    sellerId = userId > 0 ? userId : 1,
                    paymentTermDays,
                    paymentTerm,
                    collectionDay,
                    firstCollectionDate = DpPrimerCobro.Date,
                    downPayment = enganche,
                    notes,
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
                    await SaveSalePhotosAsync(saleResult.SaleId);
                    await UploadSalePhotosToServerAsync(saleResult.SaleId);

                    Services.PageDataCache.ForceRefreshSalesList = true;
                    Services.PageDataCache.RefreshedSaleDetail = null;

                    await Shell.Current.GoToAsync("..");
                    ShowToast($"✅ Venta #{saleResult.SaleId} registrada");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo crear la venta", "OK");
                }
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
    private async Task PopulateSaleData(SaleFullDetail sale)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EntryNombre.Text = sale.CustomerName?.ToUpperInvariant();
            EntryCelular.Text = sale.CustomerPhone;
            EditorNotas.Text = sale.Notes; // Only actual notes, not concatenated data

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
            
            // Set first collection date if available
            if (sale.FirstCollectionDate.HasValue)
            {
                DpPrimerCobro.Date = sale.FirstCollectionDate.Value;
            }

            // Select zone
            if (sale.ZoneName != null)
            {
                var zoneIdx = _zones.FindIndex(z => z.ZoneName.Equals(sale.ZoneName, StringComparison.OrdinalIgnoreCase));
                if (zoneIdx >= 0) PickerZone.SelectedIndex = zoneIdx;
            }

            // Select payment term (from structured field, not legacy text)
            var paymentTerm = sale.PaymentTerm ?? "";
            if (paymentTerm.Equals("SEMANAL", StringComparison.OrdinalIgnoreCase)) PickerFormaPago.SelectedIndex = 0;
            else if (paymentTerm.Equals("QUINCENAL", StringComparison.OrdinalIgnoreCase)) PickerFormaPago.SelectedIndex = 1;
            else if (paymentTerm.Equals("MENSUAL", StringComparison.OrdinalIgnoreCase)) PickerFormaPago.SelectedIndex = 2;
            else if (paymentTerm.Equals("CONTADO", StringComparison.OrdinalIgnoreCase)) PickerFormaPago.SelectedIndex = 3;

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

            // Set collection day (from structured field)
            var collectionDay = sale.CollectionDay ?? "";
            for (int i = 0; i < PickerDiaCobro.Items.Count; i++)
            {
                if (PickerDiaCobro.Items[i].Equals(collectionDay, StringComparison.OrdinalIgnoreCase))
                {
                    PickerDiaCobro.SelectedIndex = i;
                    break;
                }
            }

            // Set down payment (from structured field)
            if (sale.DownPayment > 0)
            {
                EntryEnganche.Text = sale.DownPayment.ToString("F2");
            }

            // Set status (from structured field)
            SelectedSaleStatus = SaleStatuses.FirstOrDefault(s => 
                s.StatusCode.Equals(sale.Status, StringComparison.OrdinalIgnoreCase) ||
                s.StatusName.Equals(sale.Status, StringComparison.OrdinalIgnoreCase));

            if (sale.CustomerGpsLatitude.HasValue && sale.CustomerGpsLongitude.HasValue)
            {
                _gpsLat = (double)sale.CustomerGpsLatitude.Value;
                _gpsLng = (double)sale.CustomerGpsLongitude.Value;
                _gpsObtained = true;
                EntryCoords.Text = $"{_gpsLat:F6}, {_gpsLng:F6}";
                LblGpsShort.Text = $"{_gpsLat:F4}, {_gpsLng:F4}";
            }
        });

        // Load previously saved photos from local SQLite DB
        await LoadEditPhotosAsync(sale.SaleId);
    }

    // ═══ Load photos from local DB when editing ═══
    private async Task LoadEditPhotosAsync(int saleId)
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
            if (!File.Exists(dbPath))
            {
                System.Diagnostics.Debug.WriteLine("[CreateSale] Local DB not found — skipping photo load");
                return;
            }

            var db = new Data.LocalDatabase(dbPath);
            var photos = await db.GetSalePhotosAsync(saleId);

            if (photos == null || photos.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateSale] No local photos for saleId={saleId}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CreateSale] Found {photos.Count} saved photos for saleId={saleId}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var photo in photos)
                {
                    if (string.IsNullOrEmpty(photo.LocalPath) || !File.Exists(photo.LocalPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateSale] Photo file missing: {photo.LocalPath}");
                        continue;
                    }

                    switch (photo.PhotoType)
                    {
                        case "Fachada":
                            _fotoFachadaPath = photo.LocalPath;
                            UpdatePhotoUI(LblFotoFachada, ImgFachada, BtnClearFachada, photo.LocalPath);
                            break;
                        case "Cliente":
                            _fotoClientePath = photo.LocalPath;
                            UpdatePhotoUI(LblFotoCliente, ImgCliente, BtnClearCliente, photo.LocalPath);
                            break;
                        case "Contrato":
                            _fotoContratoPath = photo.LocalPath;
                            UpdatePhotoUI(LblFotoContrato, ImgContrato, BtnClearContrato, photo.LocalPath);
                            break;
                        case "Adicional":
                            _fotoAdicionalPath = photo.LocalPath;
                            UpdatePhotoUI(LblFotoAdicional, ImgAdicional, BtnClearAdicional, photo.LocalPath);
                            break;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CreateSale] Error loading edit photos: {ex.Message}");
        }
    }

    // ═══ Navigation ═══
    private async void OnBackTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private async void OnCancelTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    // ═══ Toast no bloqueante ═══
    private static void ShowToast(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;

            var toast = new Frame
            {
                BackgroundColor = Color.FromArgb("#28A745"),
                CornerRadius = 12,
                Padding = new Thickness(18, 10),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 40),
                Opacity = 0,
                Content = new Label
                {
                    Text = message,
                    TextColor = Colors.White,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            };

            // Add to the page overlay
            if (page is ContentPage cp && cp.Content is Layout layout)
            {
                layout.Add(toast);
                await toast.FadeTo(1, 250);
                await Task.Delay(2500);
                await toast.FadeTo(0, 300);
                layout.Remove(toast);
            }
        });
    }
    
    // ═══ INotifyPropertyChanged ═══
    public new event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
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
    public string ProductDisplay => $"{ProductName}  ${UnitPrice:N2}";
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
