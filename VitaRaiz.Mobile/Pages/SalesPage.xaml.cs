using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Data;
using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;
using NLog;

namespace VitaRaiz.Mobile.Pages;

public partial class SalesPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private readonly Logger _logger = AppLogger.Get();
    private List<SaleListItem> _allSales = new();
    private string _searchText = "";
    private bool _isRefreshing;

    private bool _initialized;

    // REDESIGNED: Flat list with mixed item types (headers + sales)
    public ObservableCollection<SaleDisplayItem> FlatSalesList { get; } = new();

    public SalesPage()
    {
        System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        System.Diagnostics.Debug.WriteLine("║  SALESPAGE CONSTRUCTOR START                                  ║");
        System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        
        _logger.LogEntry();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("[SalesPage] InitializeComponent...");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[SalesPage] BindingContext = this...");
            BindingContext = this;

            System.Diagnostics.Debug.WriteLine("[SalesPage] Creating ApiService...");
            _apiService = new ApiService();
            System.Diagnostics.Debug.WriteLine("[SalesPage] Creating CatalogService...");
            _catalogService = Application.Current?.Handler?.MauiContext?.Services
                .GetService<CatalogService>() ?? new CatalogService(_apiService);

            System.Diagnostics.Debug.WriteLine("[SalesPage] Creating commands...");
            OpenDetailCommand = new Command<int>(OnOpenDetail);
            RefreshCommand = new Command(async () => await LoadDataAsync());
            OpenImageCommand = new Command<string>(OnOpenImage);

            _logger.Info("SalesPage initialized successfully");
            System.Diagnostics.Debug.WriteLine("[SalesPage] Constructor completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesPage] EXCEPTION in constructor: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[SalesPage] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SalesPage] StackTrace: {ex.StackTrace}");
            
            _logger.LogException(ex, "Error crítico en constructor SalesPage");
            throw;
        }
        finally
        {
            _logger.LogExit();
            System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  SALESPAGE CONSTRUCTOR END                                    ║");
            System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        }
    }

    // ═══ Bindable Properties ═══
    // FilteredSales removed - now using FlatSalesList defined above
    
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Habilita el botón de búsqueda solo cuando hay ventas
    /// </summary>
    public bool HasSales => _allSales.Count > 0;

    // ═══ Commands ═══
    public ICommand OpenDetailCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenImageCommand { get; }

    // ═══ Init ═══
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogEntry();
        
        if (_initialized)
        {
            _logger.Debug("Already initialized, skipping");
            return;
        }
        _initialized = true;

        try
        {
            await _catalogService.LoadAsync();
            _logger.Info("CatalogService loaded successfully");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error in OnAppearing");
        }
        finally
        {
            _logger.LogExit();
        }
    }

    private async Task LoadDataAsync()
    {
        System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        System.Diagnostics.Debug.WriteLine("║  LOADDATAASYNC START                                          ║");
        System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        
        try
        {
            IsRefreshing = true;
            _logger.Info("[LoadDataAsync] INICIO");
            System.Diagnostics.Debug.WriteLine("[LoadDataAsync] Calling API: api/sales");
            
            var salesData = await _apiService.GetAsync<List<SaleDto>>("api/sales");
            _logger.Info("[LoadDataAsync] API Response: Count={Count}, IsNull={IsNull}", 
                salesData?.Count ?? 0, salesData == null);
            System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] API returned {salesData?.Count ?? 0} sales");

            _allSales.Clear();
            System.Diagnostics.Debug.WriteLine("[LoadDataAsync] _allSales cleared");
            
            if (salesData != null && salesData.Count > 0)
            {
                _logger.Info("[LoadDataAsync] Procesando {Count} ventas...", salesData.Count);
                System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] Processing {salesData.Count} sales...");
                
                // Log detallado de las primeras 2 ventas crudas del API
                for (int idx = 0; idx < Math.Min(salesData.Count, 2); idx++)
                {
                    var s = salesData[idx];
                    System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] Sale[{idx}]: SaleId={s.SaleId}, Customer={s.CustomerName}, Total={s.TotalAmount}, Date={s.SaleDate:yyyy-MM-dd}");
                    _logger.Debug("[LoadDataAsync] API Sale [{Index}]: SaleId={SaleId}, Customer={Customer}, Total={Total}, Paid={Paid}, Balance={Balance}, Date={Date:yyyy-MM-dd}, Status={Status}, PaymentTerms={Terms}",
                        idx, s.SaleId, s.CustomerName, s.TotalAmount, s.PaidAmount, s.Balance, s.SaleDate, s.Status, s.PaymentTerms ?? "NULL");
                }
                
                // Load photos dictionary (saleId -> thumbnailPath)
                System.Diagnostics.Debug.WriteLine("[LoadDataAsync] Loading sale photos...");
                var salePhotos = await LoadAllSalePhotosAsync(salesData.Select(s => s.SaleId).ToList());
                System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] Loaded photos for {salePhotos.Count} sales");
                
                foreach (var s in salesData)
                {
                    try
                    {
                        var (label, color, icon) = _catalogService.ResolveSaleStatus(s.Status);
                        
                        if (_allSales.Count < 3) // Log primeras 3 para muestra
                            _logger.Debug("[LoadDataAsync] Creating SaleListItem para venta {SaleId}: Customer={Name}, Amount={Amount}",
                                s.SaleId, s.CustomerName, s.TotalAmount);

                        // Get photo for this sale
                        string? thumbnailPath = null;
                        if (salePhotos.TryGetValue(s.SaleId, out var photoPath))
                        {
                            thumbnailPath = photoPath;
                            if (_allSales.Count < 3)
                                System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] Sale {s.SaleId} has photo: {thumbnailPath}");
                        }

                        // Use factory method to create completely safe item with all calculations pre-computed
                        var item = SaleListItem.CreateSafe(
                            saleId: s.SaleId,
                            customerName: s.CustomerName,
                            totalAmount: s.TotalAmount,
                            paidAmount: s.PaidAmount,
                            balance: s.Balance,
                            saleDate: s.SaleDate,
                            status: s.Status,
                            statusLabel: label,
                            statusColor: color,
                            statusIcon: icon,
                            paymentTerms: s.PaymentTerms ?? "",
                            thumbnailPath: thumbnailPath,
                            customerAddress: s.CustomerAddress,
                            productName: s.ProductName,
                            sellerName: s.SellerName,
                            firstPaymentDate: s.FirstPaymentDate
                        );
                        
                        _allSales.Add(item);
                        
                        // Log detallado de los items creados (primeros 2)
                        if (_allSales.Count <= 2)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] Item created: SaleId={item.SaleId}, Customer={item.CustomerName}, Progress={item.PaymentProgressValue:F3}");
                            _logger.Debug("[LoadDataAsync]   Item Creado: SaleId={SaleId}, Customer={Customer}, Total={Total}, Balance={Balance}, Progress={Progress:F3}, Date={Date:yyyy-MM-dd}, StatusLabel={StatusLabel}, StatusColor={StatusColor}",
                                item.SaleId, item.CustomerName, item.TotalAmount, item.Balance, item.PaymentProgressValue, item.SaleDate, item.StatusLabel, item.StatusColor);
                        }
                    }
                    catch (Exception exInner)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] ERROR processing sale {s.SaleId}: {exInner.Message}");
                        _logger.Error(exInner, "[LoadDataAsync] Error procesando venta {SaleId}", s.SaleId);
                    }
                }
                
                _logger.Info("[LoadDataAsync] {Count} ventas procesadas exitosamente", _allSales.Count);
                System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] {_allSales.Count} sales processed successfully");
                
                // Notificar cambios en propiedades dependientes
                OnPropertyChanged(nameof(HasSales));
            }

            _logger.Debug("[LoadDataAsync] Llamando ApplyFilter...");
            System.Diagnostics.Debug.WriteLine("[LoadDataAsync] Calling ApplyFilter...");
            
            ApplyFilter();
            
            _logger.Info("[LoadDataAsync] FIN - FlatSalesList.Count = {Count}", FlatSalesList.Count);
            System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] ApplyFilter completed. FlatSalesList.Count={FlatSalesList.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadDataAsync] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            _logger.LogException(ex, "Error CRÍTICO en LoadDataAsync");
        }
        finally
        {
            IsRefreshing = false;
            _logger.Debug("[LoadDataAsync] IsRefreshing = false");
            System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  LOADDATAASYNC END                                            ║");
            System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        }
    }

    // ═══ Load photos for multiple sales ═══
    private async Task<Dictionary<int, string>> LoadAllSalePhotosAsync(List<int> saleIds)
    {
        var photoDict = new Dictionary<int, string>();
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoadAllSalePhotosAsync] Loading photos for {saleIds.Count} sales");
            
            // OPTIMIZACIÓN: Solo cargar desde BD local (SQLite es rápido)
            // Eliminar llamadas individuales al API que causan problema N+1
            
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
            if (File.Exists(dbPath))
            {
                var db = new Data.LocalDatabase(dbPath);
                
                foreach (var saleId in saleIds)
                {
                    try
                    {
                        var localPhotos = await db.GetSalePhotosAsync(saleId);
                        if (localPhotos != null && localPhotos.Count > 0)
                        {
                            // Priority: Fachada > Cliente > Contrato
                            var selectedPhoto = localPhotos.FirstOrDefault(p => p.PhotoType == "Fachada")
                                             ?? localPhotos.FirstOrDefault(p => p.PhotoType == "Cliente")
                                             ?? localPhotos.FirstOrDefault(p => p.PhotoType == "Contrato");
                            
                            if (selectedPhoto != null && !string.IsNullOrEmpty(selectedPhoto.LocalPath) &&
                                File.Exists(selectedPhoto.LocalPath))
                            {
                                photoDict[saleId] = selectedPhoto.LocalPath;
                            }
                        }
                    }
                    catch (Exception exSale)
                    {
                        // Silently skip sales with photo errors
                        System.Diagnostics.Debug.WriteLine($"[LoadAllSalePhotosAsync] Error for sale {saleId}: {exSale.Message}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAllSalePhotosAsync] Local DB not found: {dbPath}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[LoadAllSalePhotosAsync] Loaded {photoDict.Count} photos from local DB");
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "[LoadAllSalePhotosAsync] Error general");
            System.Diagnostics.Debug.WriteLine($"[LoadAllSalePhotosAsync] General error: {ex.Message}");
        }
        
        return photoDict;
    }

    // ═══ Filtering + Grouping ═══
    private void ApplyFilter()
    {
        System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        System.Diagnostics.Debug.WriteLine("║  APPLYFILTER START                                            ║");
        System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        
        try
        {
            _logger.Info("[ApplyFilter] INICIO - _allSales.Count={Count}", _allSales.Count);
            System.Diagnostics.Debug.WriteLine($"[ApplyFilter] _allSales.Count = {_allSales.Count}");
            
            var filtered = _allSales.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(s =>
                    s.CustomerName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
                _logger.Debug("[ApplyFilter] After search filter: {Count} ventas", filtered.Count());
            }

            System.Diagnostics.Debug.WriteLine($"[ApplyFilter] Creating groups...");
            var groups = filtered
                .OrderByDescending(s => s.SaleDate)
                .GroupBy(s => s.SaleDate.ToString("dd/M/yyyy"))
                .Select(g => new { Key = g.Key, Sales = g.ToList() })
                .ToList();

            _logger.Info("[ApplyFilter] Creados {GroupCount} grupos. Construyendo lista plana...", groups.Count);
            System.Diagnostics.Debug.WriteLine($"[ApplyFilter] Created {groups.Count} groups");
            
            // Build flat list with headers + items
            var flatList = new List<SaleDisplayItem>();
            
            foreach (var group in groups)
            {
                // Add group header
                flatList.Add(new SaleGroupHeader(group.Key, group.Sales.Count));
                
                // Add all sales in this group
                foreach (var sale in group.Sales)
                {
                    flatList.Add(new SaleDataItem(sale));
                }
            }
            
            _logger.Info("[ApplyFilter] Lista plana construida: {TotalItems} items ({GroupCount} headers + {SaleCount} sales)", 
                flatList.Count, groups.Count, filtered.Count());
            System.Diagnostics.Debug.WriteLine($"[ApplyFilter] Flat list built: {flatList.Count} items total");
            
            // CRITICAL: Must update UI on main thread
            if (MainThread.IsMainThread)
            {
                _logger.Debug("[ApplyFilter] Ya en UI thread, actualizando directamente");
                System.Diagnostics.Debug.WriteLine("[ApplyFilter] On main thread, updating directly");
                
                try
                {
                    _logger.Debug("[ApplyFilter] Limpiando FlatSalesList...");
                    System.Diagnostics.Debug.WriteLine("[ApplyFilter] Clearing FlatSalesList...");
                    FlatSalesList.Clear();
                    _logger.Debug("[ApplyFilter] FlatSalesList cleared OK");
                    System.Diagnostics.Debug.WriteLine("[ApplyFilter] FlatSalesList cleared OK");
                    
                    System.Diagnostics.Debug.WriteLine($">>> ADDING {flatList.Count} items to FlatSalesList");
                    
                    foreach (var item in flatList)
                    {
                        FlatSalesList.Add(item);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($">>> SUCCESS! FlatSalesList.Count = {FlatSalesList.Count}");
                    _logger.Info("[ApplyFilter] ✓✓✓ FIN EXITOSO - FlatSalesList.Count={Count}", FlatSalesList.Count);
                    System.Diagnostics.Debug.WriteLine($"[ApplyFilter] SUCCESS - FlatSalesList.Count={FlatSalesList.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($">>> CRASH in FlatSalesList update: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($">>> Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($">>> HResult: 0x{ex.HResult:X8}");
                    
                    _logger.Error(ex, "[ApplyFilter] CRASH FATAL al actualizar ObservableCollection FlatSalesList");
                    _logger.Error("[ApplyFilter] ExceptionType: {Type}", ex.GetType().FullName);
                    _logger.Error("[ApplyFilter] HResult: 0x{HResult:X8}", ex.HResult);
                    if (ex.InnerException != null)
                    {
                        _logger.Error("[ApplyFilter] InnerException: {Message}", ex.InnerException.Message);
                        System.Diagnostics.Debug.WriteLine($">>> InnerException: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
            else
            {
                _logger.Debug("[ApplyFilter] No en UI thread, usando MainThread.BeginInvokeOnMainThread");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        _logger.Debug("[ApplyFilter] En UI thread dispatched. Limpiando FlatSalesList...");
                        FlatSalesList.Clear();
                        
                        foreach (var item in flatList)
                        {
                            FlatSalesList.Add(item);
                        }
                        
                        _logger.Info("[ApplyFilter] FIN (dispatched) - FlatSalesList.Count={Count}", FlatSalesList.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogException(ex, "CRASH en ApplyFilter UI thread dispatched");
                        throw;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error CRÍTICO en ApplyFilter");
            throw;
        }
    }

    // ═══ Event Handlers ═══
    private void OnSearchCompleted(object? sender, EventArgs e) => ApplyFilter();

    private void OnSearchToggle(object? sender, EventArgs e)
    {
        if (!HasSales)
        {
            System.Diagnostics.Debug.WriteLine("[OnSearchToggle] No hay ventas para buscar");
            return;
        }
        
        // Toggle la barra de búsqueda compacta dentro del header
        SearchBarCompact.IsVisible = !SearchBarCompact.IsVisible;
        System.Diagnostics.Debug.WriteLine($"[OnSearchToggle] SearchBarCompact visible: {SearchBarCompact.IsVisible}");
    }

    private async void OnRefreshTapped(object? sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void OnLogoutTapped(object? sender, EventArgs e)
    {
        try
        {
            var confirm = await DisplayAlert(
                "Cerrar Sesión", 
                "¿Está seguro que desea cerrar sesión?", 
                "Sí", 
                "No"
            );
            
            if (!confirm) return;
            
            // IMPORTANTE: Resetear tema ANTES de limpiar storage
            App.ResetThemeToDefault();
            
            // Limpiar credenciales almacenadas
            SecureStorage.Remove("auth_token");
            SecureStorage.Remove("username");
            SecureStorage.Remove("role");
            SecureStorage.Remove("user_id");
            SecureStorage.RemoveAll();
            
            // Cambiar la MainPage a LoginPage
            Application.Current!.MainPage = new LoginPage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR en OnLogoutTapped: {ex.Message}");
            await DisplayAlert("Error", "No se pudo cerrar la sesión", "OK");
        }
    }

    private async void OnAddSaleTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("CreateSalePage");
    }

    private async void OnOpenDetail(int saleId)
    {
        await Shell.Current.GoToAsync($"SaleDetailPage?saleId={saleId}");
    }

    // ═══ Bottom Tab Navigation ═══
    private async void OnTabInicio(object? s, EventArgs e) => await Shell.Current.GoToAsync("//HomePage");
    private async void OnTabCobranza(object? s, EventArgs e) => await Shell.Current.GoToAsync("//PaymentPage");
    private async void OnTabClientes(object? s, EventArgs e) => await Shell.Current.GoToAsync("//CustomersPage");

    // ═══ Image Viewer Modal (Enhanced) ═══
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

    private async void OnOpenImage(string? imagePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
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

                // Simular tiempo de carga (opcional, puedes quitarlo si las imágenes cargan rápido)
                await Task.Delay(200);
                IsImageLoading = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "Error al abrir imagen");
            IsImageViewerVisible = false;
            await DisplayAlert("Error", "No se pudo cargar la imagen", "OK");
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
            _logger.LogException(ex, "Error al cerrar imagen");
            IsImageViewerVisible = false;
        }
    }
}

/// <summary>
/// REDESIGNED: Base class for heterogeneous flat list items
/// Eliminates WinUI CollectionView IsGrouped crash 0xc000027b
/// </summary>
public abstract class SaleDisplayItem
{
    public abstract bool IsHeader { get; }
}

/// <summary>
/// Group header item in flat list
/// </summary>
public class SaleGroupHeader : SaleDisplayItem
{
    public override bool IsHeader => true;
    public string DateLabel { get; set; } = "";
    public int ItemCount { get; set; }

    public SaleGroupHeader(string dateLabel, int itemCount)
    {
        DateLabel = dateLabel;
        ItemCount = itemCount;
    }
}

/// <summary>
/// Sale data item in flat list (wraps SaleListItem)
/// </summary>
public class SaleDataItem : SaleDisplayItem
{
    public override bool IsHeader => false;
    public SaleListItem Sale { get; set; }

    public SaleDataItem(SaleListItem sale)
    {
        Sale = sale ?? throw new ArgumentNullException(nameof(sale));
    }
}
