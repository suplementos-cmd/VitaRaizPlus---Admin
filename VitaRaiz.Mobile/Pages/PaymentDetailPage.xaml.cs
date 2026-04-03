using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

[QueryProperty(nameof(PaymentId), "paymentId")]
public partial class PaymentDetailPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private PaymentDetail? _originalPayment;

    public PaymentDetailPage(ApiService apiService, CatalogService catalogService)
    {
        InitializeComponent();
        _apiService = apiService;
        _catalogService = catalogService;
        BindingContext = this;
    }

    // ── QueryProperty ──
    private int _paymentId;
    public int PaymentId
    {
        get => _paymentId;
        set { _paymentId = value; OnPropertyChanged(); }
    }

    // ── Observable properties ──
    private PaymentDetail? _payment;
    public PaymentDetail? Payment
    {
        get => _payment;
        set { _payment = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); }
    }

    public bool IsNotLoading => !IsLoading;

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set { _isEditMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsViewMode)); }
    }

    public bool IsViewMode => !IsEditMode;
    
    // ── Catálogos de API ──
    public IReadOnlyList<Models.CatalogPaymentStatus> PaymentStatuses => _catalogService.PaymentStatuses;
    
    public Models.CatalogPaymentStatus? SelectedPaymentStatus
    {
        get
        {
            if (Payment == null || string.IsNullOrEmpty(Payment.Status))
                return null;
            return PaymentStatuses.FirstOrDefault(s => 
                s.StatusCode.Equals(Payment.Status, StringComparison.OrdinalIgnoreCase));
        }
        set
        {
            if (Payment != null && value != null)
            {
                Payment.Status = value.StatusCode;
                var (label, color, _) = _catalogService.ResolvePaymentStatus(value.StatusCode);
                Payment.StatusLabel = label;
                Payment.StatusColor = color;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Payment));
            }
        }
    }

    // ── Lifecycle ──
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _catalogService.LoadAsync();
        await LoadPaymentDetail();
        OnPropertyChanged(nameof(PaymentStatuses));
    }

    private async Task LoadPaymentDetail()
    {
        if (PaymentId <= 0) return;

        IsLoading = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[PaymentDetailPage] Loading payment {PaymentId}");
            
            // Get payment from API
            var payment = await _apiService.GetAsync<PaymentDetail>($"api/payments/{PaymentId}");
            
            if (payment == null)
            {
                await DisplayAlert("Error", "No se pudo cargar el detalle del abono.", "OK");
                return;
            }

            // Resolve status from catalog
            var (label, color, _) = _catalogService.ResolvePaymentStatus(payment.Status);
            payment.StatusLabel = label;
            payment.StatusColor = color;

            Payment = payment;
            _originalPayment = new PaymentDetail
            {
                PaymentId = payment.PaymentId,
                SaleId = payment.SaleId,
                CollectorId = payment.CollectorId,
                CollectorName = payment.CollectorName,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                GpsLatitude = payment.GpsLatitude,
                GpsLongitude = payment.GpsLongitude,
                Status = payment.Status,
                Notes = payment.Notes,
                PendingAmount = payment.PendingAmount,
                StatusLabel = payment.StatusLabel,
                StatusColor = payment.StatusColor
            };
            
            OnPropertyChanged(nameof(SelectedPaymentStatus));

            System.Diagnostics.Debug.WriteLine($"[PaymentDetailPage] Payment loaded: Amount={payment.Amount}, Collector={payment.CollectorName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PaymentDetailPage] Error: {ex.Message}");
            await DisplayAlert("Error", "Error de conexión al cargar el abono.", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Navigation ──
    private async void OnBackTapped(object? sender, EventArgs e)
    {
        if (IsEditMode)
        {
            bool shouldExit = await DisplayAlert("Cancelar cambios", 
                "Tienes cambios sin guardar. ¿Deseas salir sin guardar?", "Sí", "No");
            if (!shouldExit) return;
        }
        await Shell.Current.GoToAsync("..");
    }

    // ── Edit mode ──
    private void OnEditModeTapped(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[PaymentDetailPage] Entering edit mode");
        IsEditMode = true;
    }

    private void OnCancelEditTapped(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[PaymentDetailPage] Cancelling edit mode");
        
        // Restore original values
        if (_originalPayment != null && Payment != null)
        {
            Payment.Amount = _originalPayment.Amount;
            Payment.PaymentDate = _originalPayment.PaymentDate;
            Payment.Status = _originalPayment.Status;
            Payment.Notes = _originalPayment.Notes;
            
            var (label, color, _) = _catalogService.ResolvePaymentStatus(Payment.Status);
            Payment.StatusLabel = label;
            Payment.StatusColor = color;
            
            // Update UI
            EntryAmount.Text = Payment.Amount.ToString();
            DatePayment.Date = Payment.PaymentDate;
            EditorNotes.Text = Payment.Notes;
            OnPropertyChanged(nameof(SelectedPaymentStatus));
            OnPropertyChanged(nameof(Payment));
        }

        IsEditMode = false;
    }

    private async void OnSaveTapped(object? sender, EventArgs e)
    {
        if (Payment == null) return;

        // Validate amount
        if (!decimal.TryParse(EntryAmount.Text, out decimal newAmount) || newAmount <= 0)
        {
            await DisplayAlert("Validación", "Ingresa un importe válido", "OK");
            return;
        }

        // Validate status
        if (SelectedPaymentStatus == null)
        {
            await DisplayAlert("Validación", "Selecciona un estatus", "OK");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[PaymentDetailPage] Saving changes for payment {PaymentId}");

            var payload = new
            {
                paymentId = Payment.PaymentId,
                amount = newAmount,
                paymentDate = Payment.PaymentDate,        // fecha no editable — usar valor original
                status = SelectedPaymentStatus.StatusCode,
                notes = Payment.Notes ?? ""               // notas no editables — preservar valor
            };

            var saved = await _apiService.PutAsync<object>($"api/payments/{PaymentId}", payload);

            if (saved)
            {
                await DisplayAlert("✅", "Cambios guardados correctamente", "OK");
                IsEditMode = false;
                await LoadPaymentDetail(); // Refresh
            }
            else
            {
                await DisplayAlert("Error", "No se pudieron guardar los cambios", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PaymentDetailPage] Save error: {ex.Message}");
            await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "OK");
        }
    }
}
