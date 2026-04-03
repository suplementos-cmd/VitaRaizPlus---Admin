using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;
using VitaRaiz.Mobile.Services;
using NLog;

namespace VitaRaiz.Mobile.Pages;

/// <summary>
/// Contexto de venta pasado como JSON desde SaleDetailPage (modo contextual).
/// </summary>
public class CobroContext
{
    public string CustomerName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string ZoneName { get; set; } = "";
    public DateTime SaleDate { get; set; }
    public DateTime? FirstCollectionDate { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>Acción de visita seleccionable del catálogo.</summary>
public class VisitActionItem : INotifyPropertyChanged
{
    public int    ActionId   { get; set; }   // STATUS_ID numérico (FK al catálogo)
    public string ActionCode { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string Icon       { get; set; } = "";
    public string ColorHex   { get; set; } = "#888888";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public string DisplayText => $"{Icon} {ActionName}".Trim();
    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>Monto rápido seleccionable.</summary>
public class QuickAmountItem : INotifyPropertyChanged
{
    public string Amount      { get; set; } = "";
    public string DisplayText { get; set; } = "";
    public bool   IsCustom    { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

[QueryProperty(nameof(SaleId), "saleId")]
[QueryProperty(nameof(ContextJson), "contextJson")]
public partial class RegistrarCobroPage : ContentPage
{
    private readonly Logger _logger = AppLogger.Get();
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private double _latitude;
    private double _longitude;

    public RegistrarCobroPage(ApiService apiService, CatalogService catalogService)
    {
        InitializeComponent();
        _apiService = apiService;
        _catalogService = catalogService;
        SelectAmountCommand    = new Command<string>(OnSelectAmount);
        SelectActionCommand    = new Command<string>(OnSelectAction);
        BindingContext = this;
    }

    // ── QueryProperties ──
    private int _saleId;
    public int SaleId
    {
        get => _saleId;
        set
        {
            _saleId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasContext));
            OnPropertyChanged(nameof(IsStandaloneMode));
            OnPropertyChanged(nameof(HeaderSubtitle));
        }
    }

    private string _contextJson = "";
    public string ContextJson
    {
        get => _contextJson;
        set
        {
            _contextJson = value;
            try
            {
                var decoded = Uri.UnescapeDataString(value);
                Context = JsonSerializer.Deserialize<CobroContext>(decoded) ?? new CobroContext();
            }
            catch { Context = new CobroContext(); }
        }
    }

    // ── Contexto ──
    private CobroContext _context = new();
    public CobroContext Context
    {
        get => _context;
        private set
        {
            _context = value;
            OnPropertyChanged(nameof(CustomerName));
            OnPropertyChanged(nameof(ProductName));
            OnPropertyChanged(nameof(ZoneName));
            OnPropertyChanged(nameof(SaleDateText));
            OnPropertyChanged(nameof(FirstCollectionText));
            OnPropertyChanged(nameof(LastPaymentText));
            OnPropertyChanged(nameof(BalanceText));
            OnPropertyChanged(nameof(HeaderSubtitle));
        }
    }

    public bool HasContext       => _saleId > 0;
    public bool IsStandaloneMode => _saleId <= 0;

    public string CustomerName        => Context.CustomerName;
    public string ProductName         => Context.ProductName;
    public string ZoneName            => Context.ZoneName;
    public string SaleDateText        => Context.SaleDate.ToString("dd/MM/yyyy");
    public string FirstCollectionText => Context.FirstCollectionDate?.ToString("dd/MM/yyyy") ?? "—";
    public string LastPaymentText     => Context.LastPaymentDate?.ToString("dd/MM/yyyy") ?? "Sin abonos";
    public string BalanceText         => $"${Context.Balance:N2}";
    public string HeaderSubtitle      => HasContext ? Context.CustomerName : "Cobranza";

    // ── GPS ──
    private string _gpsStatus = "⏳ Obteniendo GPS...";
    private bool   _gpsGaveUp = false;   // true cuando timeout sin señal
    public string GpsStatus
    {
        get => _gpsStatus;
        set
        {
            _gpsStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsGpsReady));
            OnPropertyChanged(nameof(CanRegister));
            OnPropertyChanged(nameof(GpsIcon));
            OnPropertyChanged(nameof(GpsIconColor));
            OnPropertyChanged(nameof(GpsButtonColor));
        }
    }

    public bool IsGpsReady      => _latitude != 0 || _longitude != 0 || _gpsGaveUp;
    public string GpsIcon        => (_latitude != 0 || _longitude != 0) ? "📍" : _gpsGaveUp ? "⚠" : "⏳";
    public string GpsIconColor   => (_latitude != 0 || _longitude != 0) ? "#28A745" : _gpsGaveUp ? "#F59E0B" : "#9E9E9E";
    public string GpsButtonColor => CanRegister ? Application.Current!.Resources["ThemeColor"] is Color c ? c.ToHex() : "#9E9E9E" : "#BDBDBD";
    public bool CanRegister      => !IsRegistering && IsGpsReady;

    private string _gpsCoordinates = "";
    public string GpsCoordinates
    {
        get => _gpsCoordinates;
        set { _gpsCoordinates = value; OnPropertyChanged(); }
    }

    // ── Fecha/hora del cobro (auto, no editable) ──
    private string _paymentDateTimeText = "";
    public string PaymentDateTimeText
    {
        get => _paymentDateTimeText;
        set { _paymentDateTimeText = value; OnPropertyChanged(); }
    }

    // ── Ventas (modo standalone) ──
    public ObservableCollection<PaymentSaleItem> Sales { get; } = new();

    private PaymentSaleItem? _selectedSale;
    public PaymentSaleItem? SelectedSale
    {
        get => _selectedSale;
        set { _selectedSale = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedSale)); }
    }
    public bool HasSelectedSale => SelectedSale != null;

    // ── Acciones de visita (catálogo dinámico) ──
    public ObservableCollection<VisitActionItem> VisitActions { get; } = new();

    private string? _selectedActionCode;
    public string? SelectedActionCode
    {
        get => _selectedActionCode;
        set { _selectedActionCode = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedAction)); OnPropertyChanged(nameof(SelectedActionDisplay)); }
    }
    public bool HasSelectedAction    => !string.IsNullOrEmpty(_selectedActionCode);
    public string SelectedActionDisplay => VisitActions.FirstOrDefault(a => a.IsSelected)?.DisplayText ?? "";

    // ── Notas rápidas (colección eliminada, ahora es texto libre) ──

    private string? _selectedNoteText;
    public string? SelectedNoteText
    {
        get => _selectedNoteText;
        set { _selectedNoteText = value; OnPropertyChanged(); OnPropertyChanged(nameof(NoteLength)); }
    }
    public int NoteLength => _selectedNoteText?.Length ?? 0;

    // ── Montos rápidos ──
    public ObservableCollection<QuickAmountItem> QuickAmounts { get; } = new(new QuickAmountItem[]
    {
        new() { Amount = "0",      DisplayText = "🚫 $0"    },
        new() { Amount = "100",    DisplayText = "$100"   },
        new() { Amount = "200",    DisplayText = "$200"   },
        new() { Amount = "500",    DisplayText = "$500"   },
        new() { Amount = "custom", DisplayText = "Otro ✏️", IsCustom = true },
    });

    // ── Monto ──
    private string _paymentAmountText = "";
    public string PaymentAmountText
    {
        get => _paymentAmountText;
        set { _paymentAmountText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPaymentAmount)); OnPropertyChanged(nameof(RemainingBalanceText)); OnPropertyChanged(nameof(HasRemainingBalance)); }
    }

    public string RemainingBalanceText
    {
        get
        {
            if (!HasContext) return "";
            if (!decimal.TryParse(_paymentAmountText, out var abono) || abono < 0) return "";
            var restante = Context.Balance - abono;
            return restante >= 0 ? $"Quedaría ${restante:N2}" : "⚠ Excede saldo";
        }
    }
    public bool HasRemainingBalance => HasContext && !string.IsNullOrEmpty(RemainingBalanceText);

    private bool _showCustomEntry;
    public bool ShowCustomEntry
    {
        get => _showCustomEntry;
        set { _showCustomEntry = value; OnPropertyChanged(); }
    }

    private bool _showAmountError;
    public bool ShowAmountError
    {
        get => _showAmountError;
        set { _showAmountError = value; OnPropertyChanged(); }
    }

    private bool _showActionError;
    public bool ShowActionError
    {
        get => _showActionError;
        set { _showActionError = value; OnPropertyChanged(); }
    }

    public bool HasPaymentAmount => !string.IsNullOrEmpty(_paymentAmountText);

    // ── Estado UI ──
    private bool _isRegistering;
    public bool IsRegistering
    {
        get => _isRegistering;
        set { _isRegistering = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanRegister)); }
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    // ── Comandos ──
    public ICommand SelectAmountCommand    { get; }
    public ICommand SelectActionCommand    { get; }

    // ── Lifecycle ──
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, IsStandaloneMode);
        PaymentDateTimeText = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
        await _catalogService.LoadAsync();
        await LoadPaymentStatusesAsync();
        ErrorMessage = "";
        _ = GetCurrentLocationAsync();
        if (IsStandaloneMode)
            await LoadSalesAsync();
    }

    private async Task LoadPaymentStatusesAsync()
    {
        VisitActions.Clear();
        try
        {
            // Usa el catálogo ya cargado (api/catalogs/all) — sin llamada adicional
            var cached = _catalogService.VisitActions;
            if (cached.Count > 0)
            {
                foreach (var a in cached.OrderBy(x => x.DisplayOrder))
                    VisitActions.Add(new VisitActionItem
                    {
                        ActionId   = a.StatusId,
                        ActionCode = a.ActionCode,
                        ActionName = a.ActionName,
                        Icon       = a.Icon ?? "",
                        ColorHex   = a.ColorHex ?? "#888888"
                    });
                _logger.Info("[LoadPaymentStatuses] {Count} acciones cargadas desde caché", cached.Count);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "[LoadPaymentStatuses] Error al leer caché, usando defaults");
        }
        // Fallback cuando el catálogo no pudo cargarse
        foreach (var a in new[]
        {
            new VisitActionItem { ActionId = 10, ActionCode = "PAGO_RECIBIDO",         ActionName = "Pago Recibido",         Icon = "💰", ColorHex = "#28A745" },
            new VisitActionItem { ActionId = 20, ActionCode = "CLIENTE_NO_ENCONTRADO", ActionName = "No Encontrado",         Icon = "🏠", ColorHex = "#FFC107" },
            new VisitActionItem { ActionId = 30, ActionCode = "PROMESA_PAGO",          ActionName = "Promesa de Pago",       Icon = "📅", ColorHex = "#17A2B8" },
            new VisitActionItem { ActionId = 40, ActionCode = "CLIENTE_REHUSA",        ActionName = "Se Rehúsa",             Icon = "🚫", ColorHex = "#DC3545" },
        })
            VisitActions.Add(a);
    }

    private async Task GetCurrentLocationAsync()
    {
        try
        {
            // 1) Última ubicación cacheada — normalmente instantáneo
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location != null)
            {
                SetGpsLocation(location);
                _ = Task.Run(RefreshGpsAsync); // refresco de precisión en segundo plano
                return;
            }
            // 2) Sin caché: intento rápido con timeout corto de 4 s
            GpsStatus = "⏳ Obteniendo GPS...";
            location = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Lowest, TimeSpan.FromSeconds(4)));
            if (location != null)
            {
                SetGpsLocation(location);
                _ = Task.Run(RefreshGpsAsync);
            }
            else
            {
                // 3) Sin señal: habilitar botón ya que el cobrador no puede esperar
                _gpsGaveUp = true;
                GpsStatus  = "⚠ Sin GPS"; // setter dispara IsGpsReady/CanRegister/GpsButtonColor
            }
        }
        catch (Exception ex)
        {
            GpsStatus = "⚠ Sin GPS";
            _logger.Warn(ex, "[GPS] Error en RegistrarCobroPage");
        }
    }

    private async Task RefreshGpsAsync()
    {
        try
        {
            var loc = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            if (loc != null)
                MainThread.BeginInvokeOnMainThread(() => SetGpsLocation(loc));
        }
        catch { /* refresco silencioso */ }
    }

    private void SetGpsLocation(Location location)
    {
        _latitude      = location.Latitude;
        _longitude     = location.Longitude;
        GpsStatus      = "✅ GPS listo";   // dispara CanRegister / GpsIcon
        GpsCoordinates = $"{_latitude:F5}, {_longitude:F5}";
        _logger.Info("[GPS] Lat={Lat}, Lon={Lon}", _latitude, _longitude);
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            var salesData = await _apiService.GetAsync<List<SaleDto>>("api/sales/active");
            if (salesData == null) return;
            var items = salesData.Select(s => new PaymentSaleItem
            {
                SaleId        = s.SaleId,
                CustomerName  = s.CustomerName,
                TotalAmount   = s.TotalAmount,
                PendingAmount = s.Balance
            }).ToList();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Sales.Clear();
                foreach (var item in items) Sales.Add(item);
            });
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "[LoadSalesAsync] Error al cargar ventas");
        }
    }

    // ── Navegación ──
    private async void OnBackTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private void OnCustomAmount(object? sender, EventArgs e) => EntryMonto.Focus();

    private void OnSelectAmount(string amount)
    {
        ShowAmountError = false;
        foreach (var item in QuickAmounts)
            item.IsSelected = item.Amount == amount;

        if (amount == "custom")
        {
            ShowCustomEntry = true;
            PaymentAmountText = "";
            // pequeño delay para que el Entry se vuelva visible antes de enfocar
            Dispatcher.Dispatch(() => EntryMonto.Focus());
            return;
        }

        ShowCustomEntry = false;
        PaymentAmountText = amount;
        // Auto-seleccionar acción según monto
        if (amount == "0")
            AutoSelectAction("CLIENTE_NO_ENCONTRADO");
        else if (decimal.TryParse(amount, out var a) && a > 0)
            AutoSelectAction("PAGO_RECIBIDO");
    }

    private void AutoSelectAction(string preferredCode)
    {
        var target = VisitActions.FirstOrDefault(a => a.ActionCode == preferredCode)
                  ?? VisitActions.FirstOrDefault();
        if (target == null) return;
        foreach (var a in VisitActions) a.IsSelected = false;
        target.IsSelected  = true;
        SelectedActionCode = target.ActionCode;
        ShowActionError    = false;
    }

    // ── Selección de acción de cobro ──
    private void OnSelectAction(string actionCode)
    {
        ShowActionError = false;
        foreach (var a in VisitActions) a.IsSelected = a.ActionCode == actionCode;
        SelectedActionCode = actionCode;
    }

    // ── Guardar cobro ──
    private async void OnRegistrarCobro(object? sender, EventArgs e)
    {
        ErrorMessage    = "";
        ShowAmountError = false;
        ShowActionError = false;

        var saleId = HasContext ? _saleId : SelectedSale?.SaleId ?? 0;
        if (saleId <= 0)
        {
            ErrorMessage = IsStandaloneMode ? "Selecciona una venta" : "Error: venta no disponible";
            return;
        }
        if (!decimal.TryParse(PaymentAmountText, out decimal monto) || monto < 0)
        {
            ShowAmountError = true;
            return;
        }
        // Monto > 0 requerido para acciones de tipo pago
        var isPaymentAction = SelectedActionCode?.StartsWith("PAGO") == true;
        if (monto == 0 && isPaymentAction)
        {
            ShowAmountError = true;
            ErrorMessage = "Para una acción de pago el monto debe ser > $0";
            return;
        }
        if (string.IsNullOrEmpty(SelectedActionCode))
        {
            ShowActionError = true;
            return;
        }

        IsRegistering = true;
        try
        {
            var userIdStr = await SecureStorage.GetAsync("user_id");
            int.TryParse(userIdStr, out int userId);

            var success = await _apiService.PostAsync<object>("api/payments", new
            {
                saleId,
                amount               = monto,
                collectorId          = userId > 0 ? userId : 1,
                gpsLatitude          = _latitude,
                gpsLongitude         = _longitude,
                notes                = SelectedNoteText ?? "",
                collectionActionId   = VisitActions.FirstOrDefault(a => a.IsSelected)?.ActionId
            });

            if (success)
            {
                _logger.Info("[OnRegistrarCobro] ✓ SaleId={SaleId}, Amount={Amount}, Action={Action}",
                    saleId, monto, SelectedActionCode);
                await DisplayAlert("✅ Cobro registrado", $"Abono de ${monto:N2} guardado correctamente", "OK");
                ResetForm();
                if (HasContext) await Shell.Current.GoToAsync("..");
            }
            else
            {
                ErrorMessage = "No se pudo registrar el cobro. Intenta de nuevo.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, "[OnRegistrarCobro] Error");
            ErrorMessage = "Error inesperado. Intenta de nuevo.";
        }
        finally
        {
            IsRegistering = false;
        }
    }

    private void ResetForm()
    {
        PaymentAmountText  = "";
        SelectedNoteText   = null;
        SelectedActionCode = null;
        ErrorMessage       = "";
        SelectedSale       = null;
        foreach (var a in VisitActions)   a.IsSelected = false;
        foreach (var q in QuickAmounts)   q.IsSelected = false;
        ShowCustomEntry     = false;
        ShowAmountError     = false;
        ShowActionError     = false;
        PaymentDateTimeText = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
    }
}

// ── DTO para el selector de ventas en modo standalone ──
public class PaymentSaleItem
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string DisplayText =>
        $"Venta #{SaleId} - {CustomerName} (Pend: ${PendingAmount:N2})";
}
