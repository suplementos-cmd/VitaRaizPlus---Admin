namespace VitaRaiz.Mobile.Controls;

public partial class AppHeader : ContentView
{
    // ── Bindable Properties ──────────────────────────────────────────

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(AppHeader), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(AppHeader), string.Empty,
            propertyChanged: (b, _, n) => ((AppHeader)b).UpdateSubtitle((string)n));

    /// <summary>Secondary line below the title. Hidden when empty.</summary>
    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly BindableProperty CanGoBackProperty =
        BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(AppHeader), false,
            propertyChanged: (b, _, n) => ((AppHeader)b).UpdateBackButton((bool)n));

    /// <summary>Shows a back arrow button on the left when true.</summary>
    public bool CanGoBack
    {
        get => (bool)GetValue(CanGoBackProperty);
        set => SetValue(CanGoBackProperty, value);
    }

    public static readonly BindableProperty ShowActionsProperty =
        BindableProperty.Create(nameof(ShowActions), typeof(bool), typeof(AppHeader), true,
            propertyChanged: (b, _, n) => ((AppHeader)b).UpdateActionsVisibility((bool)n));

    /// <summary>Controls visibility of the search / refresh / logout icons. Set to false for sub-pages.</summary>
    public bool ShowActions
    {
        get => (bool)GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    public static readonly BindableProperty CanSearchProperty =
        BindableProperty.Create(nameof(CanSearch), typeof(bool), typeof(AppHeader), true,
            propertyChanged: (b, _, n) => ((AppHeader)b).UpdateSearchIconState((bool)n));

    /// <summary>Whether search is available. When false the search icon is dimmed and non-interactive.</summary>
    public bool CanSearch
    {
        get => (bool)GetValue(CanSearchProperty);
        set => SetValue(CanSearchProperty, value);
    }

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(AppHeader), string.Empty,
            BindingMode.TwoWay);

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly BindableProperty ExtraIconProperty =
        BindableProperty.Create(nameof(ExtraIcon), typeof(string), typeof(AppHeader), string.Empty);

    /// <summary>Optional icon text shown on the right side for a page-specific action.</summary>
    public string ExtraIcon
    {
        get => (string)GetValue(ExtraIconProperty);
        set => SetValue(ExtraIconProperty, value);
    }

    public static readonly BindableProperty ExtraIconVisibleProperty =
        BindableProperty.Create(nameof(ExtraIconVisible), typeof(bool), typeof(AppHeader), false,
            propertyChanged: (b, _, n) => ((AppHeader)b).UpdateExtraIconVisibility((bool)n));

    /// <summary>Controls visibility of the extra icon. Bind to a VM property for dynamic show/hide.</summary>
    public bool ExtraIconVisible
    {
        get => (bool)GetValue(ExtraIconVisibleProperty);
        set => SetValue(ExtraIconVisibleProperty, value);
    }

    public static readonly BindableProperty CanGoForwardProperty =
        BindableProperty.Create(nameof(CanGoForward), typeof(bool), typeof(AppHeader), false,
            propertyChanged: (b, _, n) => ((AppHeader)b).UpdateForwardButton((bool)n));

    /// <summary>Shows the forward arrow (›) in the nav pill when true.</summary>
    public bool CanGoForward
    {
        get => (bool)GetValue(CanGoForwardProperty);
        set => SetValue(CanGoForwardProperty, value);
    }

    // ── Events exposed to pages ──────────────────────────────────────────────

    public event EventHandler? BackClicked;
    public event EventHandler? ForwardClicked;
    public event EventHandler? RefreshClicked;
    public event EventHandler? LogoutClicked;
    public event EventHandler? SearchCompleted;
    public event EventHandler? ExtraIconClicked;

    // ── Constructor ──────────────────────────────────────────────────

    public AppHeader()
    {
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateNavPill();
        UpdateSubtitle(Subtitle);
        UpdateActionsVisibility(ShowActions);
        UpdateSearchIconState(CanSearch);
        UpdateExtraIconVisibility(ExtraIconVisible);
    }

    // ── Internal handlers ────────────────────────────────────────────

    private void OnBackIconTapped(object? sender, EventArgs e)
    {
        _ = AnimatePress(BackButtonContainer);
        BackClicked?.Invoke(this, e);
    }

    private void OnForwardIconTapped(object? sender, EventArgs e)
    {
        _ = AnimatePress(ForwardButtonContainer);
        ForwardClicked?.Invoke(this, e);
    }

    private void OnSearchIconTapped(object? sender, EventArgs e)
    {
        _ = AnimatePress(SearchIconLabel);
        if (!CanSearch)
            return;
        SearchBarCompact.IsVisible = !SearchBarCompact.IsVisible;
        if (SearchBarCompact.IsVisible)
            SearchEntry.Focus();
    }

    private void OnRefreshIconTapped(object? sender, EventArgs e)
    {
        _ = AnimatePress(RefreshIconLabel);
        RefreshClicked?.Invoke(this, e);
    }

    private void OnLogoutIconTapped(object? sender, EventArgs e)
    {
        _ = AnimatePress(LogoutIconLabel);
        LogoutClicked?.Invoke(this, e);
    }

    private void OnSearchEntryCompleted(object? sender, EventArgs e) =>
        SearchCompleted?.Invoke(this, e);

    private void OnExtraIconTapped(object? sender, EventArgs e)
    {
        _ = AnimatePress(ExtraIconLabel);
        ExtraIconClicked?.Invoke(this, e);
    }

    private static async Task AnimatePress(View v)
    {
        await v.ScaleTo(0.80, 70, Easing.CubicOut);
        await v.ScaleTo(1.00, 80, Easing.CubicIn);
    }

    // ── Update helpers ────────────────────────────────────────────────

    private void UpdateBackButton(bool _) => UpdateNavPill();
    private void UpdateForwardButton(bool _) => UpdateNavPill();

    private void UpdateNavPill()
    {
        if (NavPill is null) return;
        bool back    = CanGoBack;
        bool forward = CanGoForward;
        NavPill.IsVisible                = back || forward;
        BackButtonContainer.IsVisible    = back;
        NavPillDivider.IsVisible         = back && forward;
        ForwardButtonContainer.IsVisible = forward;
    }

    private void UpdateSubtitle(string subtitle)
    {
        if (SubtitleLabel is null) return;
        SubtitleLabel.IsVisible = !string.IsNullOrEmpty(subtitle);
    }

    private void UpdateActionsVisibility(bool show)
    {
        if (SearchIconLabel is null) return;
        SearchIconLabel.IsVisible = show;
        RefreshIconLabel.IsVisible = show;
        LogoutIconLabel.IsVisible = show;
        // Hide the whole frosted-pill when no actions shown (sub-pages with back button only)
        if (ActionsContainer is not null)
            ActionsContainer.IsVisible = show || ExtraIconVisible;
        if (!show)
            SearchBarCompact.IsVisible = false;
    }

    private void UpdateSearchIconState(bool canSearch)
    {
        if (SearchIconInnerLabel is null) return;
        SearchIconLabel.IsEnabled = canSearch;
        SearchIconInnerLabel.Opacity = canSearch ? 1.0 : 0.35;
    }

    private void UpdateExtraIconVisibility(bool visible)
    {
        if (ExtraIconLabel is null) return;
        ExtraIconLabel.IsVisible = visible;
        // Ensure the actions pill is visible whenever extra icon is shown
        if (ActionsContainer is not null && visible)
            ActionsContainer.IsVisible = true;
    }
}
