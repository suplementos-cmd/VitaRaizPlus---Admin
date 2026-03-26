using System.Collections.ObjectModel;
using System.Windows.Input;
using VitaRaiz.Mobile.Models;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile.Pages;

public partial class SalesPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly CatalogService _catalogService;
    private List<SaleListItem> _allSales = new();
    private string _searchText = "";
    private bool _isRefreshing;

    public SalesPage()
    {
        InitializeComponent();
        BindingContext = this;

        _apiService = new ApiService();
        _catalogService = Application.Current?.Handler?.MauiContext?.Services
            .GetService<CatalogService>() ?? new CatalogService(_apiService);

        OpenDetailCommand = new Command<int>(OnOpenDetail);
        RefreshCommand = new Command(async () => await LoadDataAsync());

        _ = InitAsync();
    }

    // ═══ Bindable Properties ═══
    public ObservableCollection<SaleGroup> FilteredSales { get; } = new();

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

    // ═══ Commands ═══
    public ICommand OpenDetailCommand { get; }
    public ICommand RefreshCommand { get; }

    // ═══ Init ═══
    private async Task InitAsync()
    {
        try
        {
            await _catalogService.LoadAsync();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesPage] Init error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsRefreshing = true;
            var salesData = await _apiService.GetAsync<List<SaleDto>>("api/sales");

            _allSales.Clear();
            if (salesData != null)
            {
                foreach (var s in salesData)
                {
                    var (label, color, icon) = _catalogService.ResolveSaleStatus(s.Status);

                    _allSales.Add(new SaleListItem
                    {
                        SaleId = s.SaleId,
                        CustomerName = s.CustomerName,
                        TotalAmount = s.TotalAmount,
                        PaidAmount = s.PaidAmount,
                        Balance = s.Balance,
                        SaleDate = s.SaleDate,
                        Status = s.Status,
                        StatusLabel = label,
                        StatusColor = color,
                        StatusIcon = icon,
                        PaymentTerms = s.PaymentTerms ?? ""
                    });
                }
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesPage] Error: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // ═══ Filtering + Grouping ═══
    private void ApplyFilter()
    {
        var filtered = _allSales.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(s =>
                s.CustomerName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        var groups = filtered
            .OrderByDescending(s => s.SaleDate)
            .GroupBy(s => s.SaleDate.ToString("dd/M/yyyy"))
            .Select(g => new SaleGroup(g.Key, g.Count(), g.ToList()))
            .ToList();

        FilteredSales.Clear();
        foreach (var g in groups)
            FilteredSales.Add(g);
    }

    // ═══ Event Handlers ═══
    private void OnSearchCompleted(object? sender, EventArgs e) => ApplyFilter();

    private void OnSearchToggle(object? sender, EventArgs e)
    {
        SearchBar.IsVisible = !SearchBar.IsVisible;
    }

    private async void OnRefreshTapped(object? sender, EventArgs e)
    {
        await LoadDataAsync();
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
}

/// <summary>
/// Grouped sales by date for CollectionView IsGrouped.
/// </summary>
public class SaleGroup : List<SaleListItem>
{
    public string Key { get; }
    public int Count { get; }

    public SaleGroup(string key, int count, List<SaleListItem> items) : base(items)
    {
        Key = key;
        Count = count;
    }
}
