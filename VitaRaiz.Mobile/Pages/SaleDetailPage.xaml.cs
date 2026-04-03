using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;
using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

// ── Photo Info Model ──
public class PhotoInfo
{
    public string? PhotoPath { get; set; }
    public string PhotoType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Icon { get; set; } = "🖼️";
    public bool IsAvailable => !string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath);
}

[QueryProperty(nameof(SaleId), "saleId")]
public partial class SaleDetailPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private bool _isCobrador;

    public SaleDetailPage(ApiService apiService, CatalogService catalogService)
    {
        InitializeComponent();
        _apiService = apiService;
        _catalogService = catalogService;

        CallClientCommand = new Command(async () => await CallClient());
        WhatsAppCommand = new Command(async () => await OpenWhatsApp());
        OpenPaymentDetailCommand = new Command<int>(OnOpenPaymentDetail);
        OpenPhotoCommand = new Command<string>(OnOpenPhoto);
        NextPageCommand = new Command(() => { _currentPage++; ApplyPage(); }, () => CanGoNext);
        PrevPageCommand = new Command(() => { _currentPage--; ApplyPage(); }, () => CanGoPrev);
        OpenRegistrarCobroCommand = new Command(OnOpenRegistrarCobro);

        BindingContext = this;
    }

    // ── QueryProperty ──
    private int _saleId;
    public int SaleId
    {
        get => _saleId;
        set { _saleId = value; OnPropertyChanged(); }
    }

    // ── Observable properties ──
    private SaleFullDetail? _sale;
    public SaleFullDetail? Sale
    {
        get => _sale;
        set
        {
            _sale = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ZoneName));
            OnPropertyChanged(nameof(CustomerName));
            OnPropertyChanged(nameof(CollectionDay));
            OnPropertyChanged(nameof(HasPayments));
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ObservableCollection<SaleItemDetail> Items { get; } = new();
    public ObservableCollection<PaymentDetail> Payments { get; } = new();
    public ObservableCollection<PaymentDetail> PagedPayments { get; } = new();
    public ObservableCollection<PhotoInfo> AvailablePhotos { get; } = new();

    // ── Paginación historial ──
    private const int PageSize = 10;
    private int _currentPage = 0;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Payments.Count / (double)PageSize));
    public bool CanGoPrev => _currentPage > 0;
    public bool CanGoNext => _currentPage < TotalPages - 1;
    public bool HasMultiplePages => TotalPages > 1;
    public string PageLabel => $"{_currentPage + 1} / {TotalPages}";
    public ICommand NextPageCommand { get; }
    public ICommand PrevPageCommand { get; }

    private void ApplyPage()
    {
        var page = Payments.Skip(_currentPage * PageSize).Take(PageSize).ToList();
        PagedPayments.Clear();
        foreach (var p in page) PagedPayments.Add(p);
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(HasMultiplePages));
        OnPropertyChanged(nameof(PageLabel));
        ((Command)NextPageCommand).ChangeCanExecute();
        ((Command)PrevPageCommand).ChangeCanExecute();
    }

    private string? _bannerPhotoPath;
    public string? BannerPhotoPath
    {
        get => _bannerPhotoPath;
        set
        {
            _bannerPhotoPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasBannerPhoto));
        }
    }

    public bool HasBannerPhoto => !string.IsNullOrEmpty(BannerPhotoPath);

    // ── Individual Photo Properties ──
    private string? _fachadaPhotoPath;
    public string? FachadaPhotoPath
    {
        get => _fachadaPhotoPath;
        set { _fachadaPhotoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFachadaPhoto)); }
    }
    public bool HasFachadaPhoto => !string.IsNullOrEmpty(FachadaPhotoPath) && File.Exists(FachadaPhotoPath);

    private string? _clientePhotoPath;
    public string? ClientePhotoPath
    {
        get => _clientePhotoPath;
        set { _clientePhotoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasClientePhoto)); }
    }
    public bool HasClientePhoto => !string.IsNullOrEmpty(ClientePhotoPath) && File.Exists(ClientePhotoPath);

    private string? _contratoPhotoPath;
    public string? ContratoPhotoPath
    {
        get => _contratoPhotoPath;
        set { _contratoPhotoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasContratoPhoto)); }
    }
    public bool HasContratoPhoto => !string.IsNullOrEmpty(ContratoPhotoPath) && File.Exists(ContratoPhotoPath);

    private string? _adicionalPhotoPath;
    public string? AdicionalPhotoPath
    {
        get => _adicionalPhotoPath;
        set { _adicionalPhotoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasAdicionalPhoto)); }
    }
    public bool HasAdicionalPhoto => !string.IsNullOrEmpty(AdicionalPhotoPath) && File.Exists(AdicionalPhotoPath);

    public string ZoneName => Sale?.ZoneName ?? "—";
    public string CustomerName => Sale?.CustomerName is { Length: > 0 } n ? n : "Detalle de Venta";
    public string CollectionDay
    {
        get
        {
            var notes = Sale?.Notes ?? "";
            var parts = notes.Split('|');
            if (parts.Length >= 2)
            {
                var day = parts[1].Trim();
                if (day.StartsWith("Cobro:")) day = day.Replace("Cobro:", "").Trim();
                return day;
            }
            return "—";
        }
    }
    public bool HasPayments => Payments.Count > 0;

    public ICommand CallClientCommand { get; }
    public ICommand WhatsAppCommand { get; }
    public ICommand OpenPaymentDetailCommand { get; }
    public ICommand OpenPhotoCommand { get; }
    public ICommand OpenRegistrarCobroCommand { get; }

    // ── Lifecycle ──
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _catalogService.LoadAsync();
        await CheckRoleAsync();

        // If the edit form just saved this sale, use the cached refresh data (no API call → no blink)
        var refreshed = Services.PageDataCache.RefreshedSaleDetail;
        if (refreshed != null && refreshed.SaleId == SaleId)
        {
            Services.PageDataCache.RefreshedSaleDetail = null; // consume
            System.Diagnostics.Debug.WriteLine("[SaleDetailPage] ⚡ Using RefreshedSaleDetail from cache — skipping API call");
            ApplySaleData(refreshed);
            await LoadSalePhotosAsync();
            return;
        }

        await LoadSaleDetail();
    }

    private async Task CheckRoleAsync()
    {
        var role = await SecureStorage.GetAsync("role") ?? "";
        var normalizedRole = role.Trim();
        
        // Role classifications
        var isCobrador = normalizedRole.Equals("Cobrador", StringComparison.OrdinalIgnoreCase);
        var isSupervisorCobro = normalizedRole.Equals("Supervisor de cobro", StringComparison.OrdinalIgnoreCase);
        var isVendedora = normalizedRole.Equals("Vendedora", StringComparison.OrdinalIgnoreCase);
        var isSupervisora = normalizedRole.Equals("Supervisora", StringComparison.OrdinalIgnoreCase) || 
                           normalizedRole.Equals("Supervisora ventas", StringComparison.OrdinalIgnoreCase);
        var isAdmin = normalizedRole.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || 
                      normalizedRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                      normalizedRole.Equals("AdminFull", StringComparison.OrdinalIgnoreCase);
        
        // View assignments:
        // 1. Cobrador, Supervisor de cobro → Historial + acciones + formulario
        // 2. Vendedora → Nada
        // 3. Supervisora, Administradores → Tabla
        
        var showHistorialConAcciones = isCobrador || isSupervisorCobro;
        var showTabla = isSupervisora || isAdmin;
        
        // Show/hide sections based on role
        VendedoraAbonosSection.IsVisible = showHistorialConAcciones;  // Historial + botón ➕
        AbonosSection.IsVisible = showTabla;                          // Tabla (solo Supervisora/Admin)
        HeaderActions.IsVisible = showHistorialConAcciones || showTabla;

        // FAB editar: solo perfil ventas y supervisión ventas
        EditFab.IsVisible = isVendedora || isSupervisora;

        // FAB cobro: cobrador y supervisor de cobro
        AddCobroFab.IsVisible = isCobrador || isSupervisorCobro;

        _isCobrador = isCobrador; // Keep for backward compatibility
    }

    private async Task LoadSaleDetail()
    {
        if (SaleId <= 0) return;

        IsLoading = true;
        try
        {
            var dto = await _apiService.GetAsync<SaleFullDetail>($"api/sales/{SaleId}/full");
            if (dto == null)
            {
                await DisplayAlert("Error", "No se pudo cargar el detalle de la venta.", "OK");
                return;
            }

            ApplySaleData(dto);

            // Load photos from server
            await LoadSalePhotosAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Error: {ex.Message}");
            await DisplayAlert("Error", "Error de conexión al cargar la venta.", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Populate UI from a SaleFullDetail (shared by LoadSaleDetail and cache path) ──
    private void ApplySaleData(SaleFullDetail dto)
    {
        // Resolve sale status from catalogs
        var (sLabel, sColor, _) = _catalogService.ResolveSaleStatus(dto.Status);
        dto.StatusLabel = sLabel;
        dto.StatusColor = sColor;

        foreach (var p in dto.Payments)
        {
            var (pLabel, pColor, _) = _catalogService.ResolvePaymentStatus(p.Status);
            p.StatusLabel = pLabel;
            p.StatusColor = pColor;
        }

        Sale = dto;

        // Prepare payment data with running pending amounts
        decimal runningPaid = 0;
        foreach (var p in dto.Payments.OrderBy(p => p.PaymentDate))
        {
            runningPaid += p.Amount;
            p.PendingAmount = dto.TotalAmount - runningPaid;
        }

        var sortedPayments = dto.Payments.OrderByDescending(p => p.PaymentDate).ToList();

        // CRITICAL: Modify ObservableCollection only on UI thread to prevent crash 0xc000027b
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Items.Clear();
            foreach (var item in dto.Items)
                Items.Add(item);

            Payments.Clear();
            foreach (var p in sortedPayments)
                Payments.Add(p);

            OnPropertyChanged(nameof(HasPayments));
            _currentPage = 0;
            ApplyPage();
        });
    }

    // ── Load photos from server ──
    private async Task LoadSalePhotosAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] LoadSalePhotosAsync START for saleId={SaleId}");
            
            // First, try to load photos from local database (these are the uploaded photos)
            await LoadPhotosFromLocalDbAsync();
            
            // If we found photos locally, we're done
            if (AvailablePhotos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Using {AvailablePhotos.Count} photos from local DB");
                return;
            }
            
            // If no local photos, try server (though server paths usually don't work on client)
            var photos = await _apiService.GetAsync<List<SalePhotoDto>>($"api/sales/{SaleId}/photos");
            
            if (photos != null && photos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Loaded {photos.Count} photos from server, checking if accessible...");
                
                // Find photos by type
                var fachadaPhoto = photos.FirstOrDefault(p => p.PhotoType.Contains("FACHADA"));
                var clientePhoto = photos.FirstOrDefault(p => p.PhotoType.Contains("CLIENTE"));
                var contratoPhoto = photos.FirstOrDefault(p => p.PhotoType.Contains("CONTRATO"));
                var adicionalPhoto = photos.FirstOrDefault(p => p.PhotoType.Contains("ADICIONAL"));

                // Try to use server photos (will only work if paths are accessible locally)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AvailablePhotos.Clear();
                    
                    if (fachadaPhoto != null && File.Exists(fachadaPhoto.FilePath))
                    {
                        FachadaPhotoPath = fachadaPhoto.FilePath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = fachadaPhoto.FilePath, 
                            PhotoType = "FACHADA", 
                            DisplayName = "Fachada",
                            Icon = "🏠"
                        });
                    }
                    
                    if (clientePhoto != null && File.Exists(clientePhoto.FilePath))
                    {
                        ClientePhotoPath = clientePhoto.FilePath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = clientePhoto.FilePath, 
                            PhotoType = "CLIENTE", 
                            DisplayName = "Cliente",
                            Icon = "👤"
                        });
                    }
                    
                    if (contratoPhoto != null && File.Exists(contratoPhoto.FilePath))
                    {
                        ContratoPhotoPath = contratoPhoto.FilePath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = contratoPhoto.FilePath, 
                            PhotoType = "CONTRATO", 
                            DisplayName = "Contrato",
                            Icon = "📄"
                        });
                    }
                    
                    if (adicionalPhoto != null && File.Exists(adicionalPhoto.FilePath))
                    {
                        AdicionalPhotoPath = adicionalPhoto.FilePath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = adicionalPhoto.FilePath, 
                            PhotoType = "ADICIONAL", 
                            DisplayName = "Adicional",
                            Icon = "🖼️"
                        });
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] AvailablePhotos populated with {AvailablePhotos.Count} accessible server photos");
                    
                    // Set banner photo
                    if (AvailablePhotos.Count > 0)
                    {
                        BannerPhotoPath = AvailablePhotos[0].PhotoPath;
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] No photos from server");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] LoadSalePhotosAsync ERROR: {ex.Message}");
        }
    }

    // ── Load photos from local SQLite DB (fallback) ──
    private async Task LoadPhotosFromLocalDbAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] LoadPhotosFromLocalDbAsync START for saleId={SaleId}");
            
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
            if (!File.Exists(dbPath))
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Local DB not found");
                return;
            }

            var db = new Data.LocalDatabase(dbPath);
            var localPhotos = await db.GetSalePhotosAsync(SaleId);

            if (localPhotos != null && localPhotos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Found {localPhotos.Count} photos in local DB");

                // Priority: Fachada > Cliente > Contrato > Adicional
                var fachadaPhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Fachada");
                var clientePhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Cliente");
                var contratoPhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Contrato");
                var adicionalPhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Adicional");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Clear and populate available photos collection
                    AvailablePhotos.Clear();
                    
                    if (fachadaPhoto != null && File.Exists(fachadaPhoto.LocalPath))
                    {
                        FachadaPhotoPath = fachadaPhoto.LocalPath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = fachadaPhoto.LocalPath, 
                            PhotoType = "FACHADA", 
                            DisplayName = "Fachada",
                            Icon = "🏠"
                        });
                    }
                    
                    if (clientePhoto != null && File.Exists(clientePhoto.LocalPath))
                    {
                        ClientePhotoPath = clientePhoto.LocalPath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = clientePhoto.LocalPath, 
                            PhotoType = "CLIENTE", 
                            DisplayName = "Cliente",
                            Icon = "👤"
                        });
                    }
                    
                    if (contratoPhoto != null && File.Exists(contratoPhoto.LocalPath))
                    {
                        ContratoPhotoPath = contratoPhoto.LocalPath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = contratoPhoto.LocalPath, 
                            PhotoType = "CONTRATO", 
                            DisplayName = "Contrato",
                            Icon = "📄"
                        });
                    }
                    
                    if (adicionalPhoto != null && File.Exists(adicionalPhoto.LocalPath))
                    {
                        AdicionalPhotoPath = adicionalPhoto.LocalPath;
                        AvailablePhotos.Add(new PhotoInfo 
                        { 
                            PhotoPath = adicionalPhoto.LocalPath, 
                            PhotoType = "ADICIONAL", 
                            DisplayName = "Adicional",
                            Icon = "🖼️"
                        });
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] AvailablePhotos populated with {AvailablePhotos.Count} photos from local DB");
                    
                    // Set banner photo (prioritize first available)
                    if (AvailablePhotos.Count > 0)
                    {
                        BannerPhotoPath = AvailablePhotos[0].PhotoPath;
                        System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner (Local): Using {AvailablePhotos[0].DisplayName}");
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] No photos in local DB");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BannerPhotoPath = null;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Error loading local photos: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BannerPhotoPath = null;
            });
        }
    }

    // ── Navigation ──
    private async void OnBackTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnEditTapped(object? sender, EventArgs e)
    {
        if (Sale == null) return;
        
        // ⚡ Pass object directly via in-memory cache — zero serialization cost
        Services.PageDataCache.PendingEditSale = Sale;
        await Shell.Current.GoToAsync($"CreateSalePage?saleId={Sale.SaleId}");
    }

    private async void OnRegistrarCobroTapped(object? sender, EventArgs e)
        => OnOpenRegistrarCobro();

    // ── Phone / WhatsApp ──
    private async Task CallClient()
    {
        if (string.IsNullOrWhiteSpace(Sale?.CustomerPhone))
        {
            await DisplayAlert("Llamar", "No hay teléfono registrado.", "OK");
            return;
        }
        try { PhoneDialer.Open(Sale.CustomerPhone); }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir el marcador: {ex.Message}", "OK");
        }
    }

    private async Task OpenWhatsApp()
    {
        if (string.IsNullOrWhiteSpace(Sale?.CustomerPhone))
        {
            await DisplayAlert("WhatsApp", "No hay teléfono registrado.", "OK");
            return;
        }
        var phone = Sale.CustomerPhone.Replace(" ", "").Replace("-", "");
        if (!phone.StartsWith("+")) phone = "+52" + phone;
        try { await Launcher.OpenAsync($"https://wa.me/{phone}"); }
        catch { await DisplayAlert("Error", "No se pudo abrir WhatsApp.", "OK"); }
    }

    // ── GPS Map ──
    private async void OnOpenGps(object? sender, EventArgs e)
    {
        if (Sale == null) return;
        var lat = Sale.CustomerGpsLatitude;
        var lng = Sale.CustomerGpsLongitude;
        if (lat == null || lng == null || (lat == 0 && lng == 0))
        {
            await DisplayAlert("GPS", "No hay coordenadas registradas.", "OK");
            return;
        }
        try { await Launcher.OpenAsync($"https://maps.google.com/?q={lat},{lng}"); }
        catch { await DisplayAlert("Error", "No se pudo abrir el mapa.", "OK"); }
    }

    // ── Navegar a RegistrarCobroPage ──
    private async void OnOpenRegistrarCobro()
    {
        if (Sale == null) return;

        // Último abono (primero en la lista porque están ordenados desc)
        var lastPayment = Payments.FirstOrDefault();

        var ctx = new CobroContext
        {
            CustomerName        = Sale.CustomerName,
            ProductName         = Sale.Items?.FirstOrDefault()?.ProductName ?? "",
            ZoneName            = Sale.ZoneName ?? "",
            SaleDate            = Sale.SaleDate,
            FirstCollectionDate = Sale.FirstCollectionDate,
            LastPaymentDate     = lastPayment?.PaymentDate,
            Balance             = Sale.Balance,
            TotalAmount         = Sale.TotalAmount
        };

        var json = Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(ctx));
        await Shell.Current.GoToAsync($"RegistrarCobroPage?saleId={SaleId}&contextJson={json}");
    }

    private void UpdatePhotoLabel(Label label, string? path)
    {
        if (path != null)
        {
            label.Text = "Foto lista ✓";
            label.TextColor = Color.FromArgb("#28A745");
        }
    }

    private async Task SavePhotoToDbAndServer(string photoType, string localPath)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] SavePhotoToDbAndServer - Type: {photoType}, Path: {localPath}");
            
            // 1. Guardar en base de datos local
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
            var db = new Data.LocalDatabase(dbPath);
            
            var photo = new Data.LocalSalePhoto
            {
                SaleId = this.SaleId,
                PhotoType = photoType,
                LocalPath = localPath,
                CreatedAt = DateTime.Now
            };
            
            await db.SaveSalePhotoAsync(photo);
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] ✓ Photo saved to local DB: {photoType}");
            
            // 2. Enviar al servidor
            try
            {
                var fileInfo = new FileInfo(localPath);
                var photoPayload = new
                {
                    photoType,
                    filePath = localPath,
                    gpsLatitude = (double?)null,
                    gpsLongitude = (double?)null,
                    fileSize = fileInfo.Length
                };
                
                var result = await _apiService.PostAsync<object>($"api/sales/{SaleId}/photos", photoPayload);
                if (result)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaleDetail] ✓ Photo uploaded to server: {photoType}");
                    await DisplayAlert("Éxito", $"Foto {photoType} guardada correctamente", "OK");
                }
            }
            catch (Exception apiEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetail] Server upload error: {apiEx.Message}");
                // No mostrar error al usuario, la foto ya está guardada localmente
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] SavePhotoToDbAndServer error: {ex.Message}");
            await DisplayAlert("Error", "No se pudo guardar la foto", "OK");
        }
    }

    private async Task CapturePhoto(string name, Func<string?, Task> callback)
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
            var path = Path.Combine(FileSystem.AppDataDirectory, $"{name}_{photo.FileName}");
            using var stream = await photo.OpenReadAsync();
            using var fs = File.OpenWrite(path);
            await stream.CopyToAsync(fs);
            await callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] Camera error ({name}): {ex.Message}");
            await DisplayAlert("Error", $"Error al capturar foto: {ex.Message}", "OK");
        }
    }

    private async Task PickPhoto(Func<string?, Task> callback)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;
            var path = Path.Combine(FileSystem.AppDataDirectory, $"upload_{photo.FileName}");
            using var stream = await photo.OpenReadAsync();
            using var fs = File.OpenWrite(path);
            await stream.CopyToAsync(fs);
            await callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] Upload error: {ex.Message}");
            await DisplayAlert("Error", $"Error al seleccionar foto: {ex.Message}", "OK");
        }
    }

    // ── Navigate to Payment Detail ──
    private async void OnOpenPaymentDetail(int paymentId)
    {
        System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Navigating to PaymentDetailPage with paymentId={paymentId}");
        await Shell.Current.GoToAsync($"PaymentDetailPage?paymentId={paymentId}");
    }

    // ── Image Viewer Modal (Enhanced) ──
    private bool _isImageViewerVisible;
    public bool IsImageViewerVisible
    {
        get => _isImageViewerVisible;
        set { _isImageViewerVisible = value; OnPropertyChanged(); }
    }

    private bool _isImageLoading;
    public bool IsImageLoading
    {
        get => _isImageLoading;
        set { _isImageLoading = value; OnPropertyChanged(); }
    }

    private string? _expandedImagePath;
    public string? ExpandedImagePath
    {
        get => _expandedImagePath;
        set { _expandedImagePath = value; OnPropertyChanged(); }
    }

    private async void OnImageTapped(object? sender, EventArgs e)
    {
        try
        {
            string? imagePath = null;

            if (sender is Image image && image.Source is FileImageSource fileSource)
            {
                imagePath = fileSource.File;
            }
            else if (!string.IsNullOrEmpty(BannerPhotoPath))
            {
                imagePath = BannerPhotoPath;
            }

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                await OpenImageInModalAsync(imagePath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la imagen", "OK");
            IsImageViewerVisible = false;
        }
    }

    private async void OnOpenPhoto(string? photoPath)
    {
        if (!string.IsNullOrEmpty(photoPath) && File.Exists(photoPath))
        {
            await OpenImageInModalAsync(photoPath);
        }
    }

    private async Task OpenImageInModalAsync(string imagePath)
    {
        try
        {
            // Configurar imagen y mostrar modal
            ExpandedImagePath = imagePath;
            IsImageLoading = true;
            IsImageViewerVisible = true;

            // Animación de entrada suave
            await Task.WhenAll(
                ImageViewerModal.FadeTo(1, 250, Easing.CubicOut),
                ImageContainer.ScaleTo(1, 300, Easing.CubicOut)
            );

            // Simular tiempo de carga
            await Task.Delay(200);
            IsImageLoading = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "No se pudo cargar la imagen", "OK");
            IsImageViewerVisible = false;
        }
    }

    private async void OnCloseImageViewer(object? sender, EventArgs e)
    {
        try
        {
            // Animación de salida suave
            await Task.WhenAll(
                ImageViewerModal.FadeTo(0, 200, Easing.CubicIn),
                ImageContainer.ScaleTo(0.8, 200, Easing.CubicIn)
            );
            
            IsImageViewerVisible = false;
            ExpandedImagePath = null;
        }
        catch (Exception ex)
        {
            IsImageViewerVisible = false;
        }
    }

    // ═══ Cobrador Action Bar ═══
    private async void OnPasarDespues(object? sender, EventArgs e)
    {
        await DisplayAlert("Programado", "Se registró: pasar después", "OK");
    }

    private async void OnPasarMasTarde(object? sender, EventArgs e)
    {
        await DisplayAlert("Programado", "Se registró: pasar más tarde", "OK");
    }

    private async void OnProximaSemana(object? sender, EventArgs e)
    {
        await DisplayAlert("Programado", "Se registró: pasar próxima semana", "OK");
    }

    private async void OnRegistrarAbono(object? sender, EventArgs e)
    {
        await DisplayAlert("Abono", "Función de registro de abono pendiente de implementar", "OK");
    }
}
