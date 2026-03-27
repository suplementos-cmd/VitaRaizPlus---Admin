using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text.Json;
using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

[QueryProperty(nameof(SaleId), "saleId")]
public partial class SaleDetailPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private bool _isCobrador;
    private string? _fotoContratoPath;
    private string? _fotoAdicionalPath;

    public SaleDetailPage(ApiService apiService, CatalogService catalogService)
    {
        InitializeComponent();
        _apiService = apiService;
        _catalogService = catalogService;

        CallClientCommand = new Command(async () => await CallClient());
        WhatsAppCommand = new Command(async () => await OpenWhatsApp());
        OpenPaymentDetailCommand = new Command<int>(OnOpenPaymentDetail);

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

    public string ZoneName => Sale?.ZoneName ?? "—";
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

    // ── Lifecycle ──
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _catalogService.LoadAsync();
        await CheckRoleAsync();
        await LoadSaleDetail();
    }

    private async Task CheckRoleAsync()
    {
        var role = await SecureStorage.GetAsync("role");
        _isCobrador = role != null && role.Equals("Cobrador", StringComparison.OrdinalIgnoreCase);

        // Show/hide sections based on role
        CobradorActionSection.IsVisible = _isCobrador;
        AbonosSection.IsVisible = _isCobrador;
        HeaderActions.IsVisible = _isCobrador;
        VendedoraAbonosSection.IsVisible = !_isCobrador;
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

            // Resolve sale status from catalogs
            var (sLabel, sColor, sIcon) = _catalogService.ResolveSaleStatus(dto.Status);
            dto.StatusLabel = sLabel;
            dto.StatusColor = sColor;

            foreach (var p in dto.Payments)
            {
                var (pLabel, pColor, _) = _catalogService.ResolvePaymentStatus(p.Status);
                p.StatusLabel = pLabel;
                p.StatusColor = pColor;
            }

            Sale = dto;

            // Prepare payment data with pending amounts
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
            });

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

    // ── Load photos from server ──
    private async Task LoadSalePhotosAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] LoadSalePhotosAsync START for saleId={SaleId}");
            
            var photos = await _apiService.GetAsync<List<SalePhotoDto>>($"api/sales/{SaleId}/photos");
            
            if (photos != null && photos.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Loaded {photos.Count} photos from server");
                
                // Find photos by type
                var fachadaPhoto = photos.FirstOrDefault(p => p.PhotoType == "FACHADA");
                var clientePhoto = photos.FirstOrDefault(p => p.PhotoType == "CLIENTE");
                var contratoPhoto = photos.FirstOrDefault(p => p.PhotoType == "CONTRATO");
                var adicionalPhoto = photos.FirstOrDefault(p => p.PhotoType == "ADICIONAL");

                // Select banner photo with priority: Fachada > Cliente > Contrato
                string? selectedPhotoPath = null;
                if (fachadaPhoto != null && File.Exists(fachadaPhoto.FilePath))
                {
                    selectedPhotoPath = fachadaPhoto.FilePath;
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner: Using Fachada photo");
                }
                else if (clientePhoto != null && File.Exists(clientePhoto.FilePath))
                {
                    selectedPhotoPath = clientePhoto.FilePath;
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner: Using Cliente photo (Fachada not found)");
                }
                else if (contratoPhoto != null && File.Exists(contratoPhoto.FilePath))
                {
                    selectedPhotoPath = contratoPhoto.FilePath;
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner: Using Contrato photo (Fachada/Cliente not found)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner: No valid photo files found, checking local DB");
                    // Fallback: Try to load from local SQLite DB
                    await LoadPhotosFromLocalDbAsync();
                    return;
                }

                // Set banner photo on UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BannerPhotoPath = selectedPhotoPath;
                });

                // Log all photos for debugging
                if (fachadaPhoto != null)
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Fachada photo: {fachadaPhoto.FilePath}");
                if (clientePhoto != null)
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Cliente photo: {clientePhoto.FilePath}");
                if (contratoPhoto != null)
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Contrato photo: {contratoPhoto.FilePath}");
                if (adicionalPhoto != null)
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Adicional photo: {adicionalPhoto.FilePath}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] No photos from server, checking local DB");
                await LoadPhotosFromLocalDbAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Error loading photos from server: {ex.Message}");
            // Fallback to local DB
            await LoadPhotosFromLocalDbAsync();
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

                // Priority: Fachada > Cliente > Contrato
                var fachadaPhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Fachada");
                var clientePhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Cliente");
                var contratoPhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Contrato");

                string? selectedPhotoPath = null;
                if (fachadaPhoto != null && File.Exists(fachadaPhoto.LocalPath))
                {
                    selectedPhotoPath = fachadaPhoto.LocalPath;
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner (Local): Using Fachada");
                }
                else if (clientePhoto != null && File.Exists(clientePhoto.LocalPath))
                {
                    selectedPhotoPath = clientePhoto.LocalPath;
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner (Local): Using Cliente");
                }
                else if (contratoPhoto != null && File.Exists(contratoPhoto.LocalPath))
                {
                    selectedPhotoPath = contratoPhoto.LocalPath;
                    System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Banner (Local): Using Contrato");
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BannerPhotoPath = selectedPhotoPath;
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
        
        // ⚡ OPTIMIZATION: Pass sale data directly to avoid API reload
        var saleJson = JsonSerializer.Serialize(Sale);
        var encodedJson = Uri.EscapeDataString(saleJson);
        await Shell.Current.GoToAsync($"CreateSalePage?saleId={Sale.SaleId}&saleData={encodedJson}");
    }

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

    // ── Cobrador actions ──
    private async void OnPasarDespues(object? sender, EventArgs e)
    {
        await DisplayAlert("Acción", "Marcado: Pasar después", "OK");
    }

    private async void OnPasarMasTarde(object? sender, EventArgs e)
    {
        await DisplayAlert("Acción", "Marcado: Pasar más tarde", "OK");
    }

    private async void OnProximaSemana(object? sender, EventArgs e)
    {
        await DisplayAlert("Acción", "Marcado: Próxima semana", "OK");
    }

    // ── Photos (Cobrador) ──
    private async void OnCameraContrato(object? s, EventArgs e) => await CapturePhoto("contrato", r => { _fotoContratoPath = r; UpdatePhotoLabel(LblFotoContrato, r); });
    private async void OnUploadContrato(object? s, EventArgs e) => await PickPhoto(r => { _fotoContratoPath = r; UpdatePhotoLabel(LblFotoContrato, r); });
    private async void OnCameraAdicionalD(object? s, EventArgs e) => await CapturePhoto("adicional", r => { _fotoAdicionalPath = r; UpdatePhotoLabel(LblFotoAdicional, r); });
    private async void OnUploadAdicionalD(object? s, EventArgs e) => await PickPhoto(r => { _fotoAdicionalPath = r; UpdatePhotoLabel(LblFotoAdicional, r); });

    private void UpdatePhotoLabel(Label label, string? path)
    {
        if (path != null)
        {
            label.Text = "Foto lista ✓";
            label.TextColor = Color.FromArgb("#28A745");
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
            var path = Path.Combine(FileSystem.AppDataDirectory, $"{name}_{photo.FileName}");
            using var stream = await photo.OpenReadAsync();
            using var fs = File.OpenWrite(path);
            await stream.CopyToAsync(fs);
            callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] Camera error ({name}): {ex.Message}");
        }
    }

    private async Task PickPhoto(Action<string?> callback)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;
            var path = Path.Combine(FileSystem.AppDataDirectory, $"upload_{photo.FileName}");
            using var stream = await photo.OpenReadAsync();
            using var fs = File.OpenWrite(path);
            await stream.CopyToAsync(fs);
            callback(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SaleDetail] Upload error: {ex.Message}");
        }
    }

    // ── Registrar abono ──
    private async void OnRegistrarAbono(object? sender, EventArgs e)
    {
        if (!decimal.TryParse(EntryAbono.Text, out decimal monto) || monto <= 0)
        {
            await DisplayAlert("Validación", "Ingresa un importe válido", "OK");
            return;
        }
        if (PickerEstatusAbono.SelectedIndex < 0)
        {
            await DisplayAlert("Validación", "Selecciona un estatus", "OK");
            return;
        }

        try
        {
            var estatus = PickerEstatusAbono.SelectedItem?.ToString() ?? "";
            var userIdStr = await SecureStorage.GetAsync("user_id");
            int.TryParse(userIdStr, out int userId);

            // Get GPS for abono
            double lat = 0, lng = 0;
            try
            {
                var loc = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
                if (loc != null) { lat = loc.Latitude; lng = loc.Longitude; }
            }
            catch { /* GPS optional for abono */ }

            var payload = new
            {
                saleId = SaleId,
                amount = monto,
                paymentMethod = "EFECTIVO",
                collectorId = userId > 0 ? userId : 1,
                notes = $"{estatus} | GPS: {lat:F6},{lng:F6} | {EditorNotaCobrador.Text?.Trim()}"
            };

            var success = await _apiService.PostAsync<object>("api/payments", payload);
            if (success)
            {
                await DisplayAlert("✅", $"Abono de ${monto:N2} registrado", "OK");
                EntryAbono.Text = "";
                EditorNotaCobrador.Text = "";
                await LoadSaleDetail(); // Refresh
            }
            else
            {
                await DisplayAlert("Error", "No se pudo registrar el abono", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    // ── Navigate to Payment Detail ──
    private async void OnOpenPaymentDetail(int paymentId)
    {
        System.Diagnostics.Debug.WriteLine($"[SaleDetailPage] Navigating to PaymentDetailPage with paymentId={paymentId}");
        await Shell.Current.GoToAsync($"PaymentDetailPage?paymentId={paymentId}");
    }

    // ── Image Viewer Modal ──
    private bool _isImageViewerVisible;
    public bool IsImageViewerVisible
    {
        get => _isImageViewerVisible;
        set { _isImageViewerVisible = value; OnPropertyChanged(); }
    }

    private string? _expandedImagePath;
    public string? ExpandedImagePath
    {
        get => _expandedImagePath;
        set { _expandedImagePath = value; OnPropertyChanged(); }
    }

    private void OnImageTapped(object? sender, EventArgs e)
    {
        if (sender is Image image && image.Source is FileImageSource fileSource)
        {
            ExpandedImagePath = fileSource.File;
            IsImageViewerVisible = true;
        }
        else if (!string.IsNullOrEmpty(BannerPhotoPath))
        {
            ExpandedImagePath = BannerPhotoPath;
            IsImageViewerVisible = true;
        }
    }

    private void OnCloseImageViewer(object? sender, EventArgs e)
    {
        IsImageViewerVisible = false;
    }
}
