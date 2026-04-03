namespace VitaRaiz.Mobile.Controls;

/// <summary>
/// Master layout wrapper shared by every page.
/// Provides a fixed AppHeader (top), a body content slot (middle),
/// and an optional BottomNavBar (bottom).
///
/// Mark the content-property so inner XAML children are automatically
/// assigned to <see cref="Body"/>.
/// </summary>
[ContentProperty(nameof(Body))]
public partial class PageFrame : ContentView
{
    // ── Bindable Properties ──────────────────────────────────────────

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(PageFrame), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(PageFrame), string.Empty);

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly BindableProperty CanGoBackProperty =
        BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(PageFrame), false);

    public bool CanGoBack
    {
        get => (bool)GetValue(CanGoBackProperty);
        set => SetValue(CanGoBackProperty, value);
    }

    public static readonly BindableProperty ShowActionsProperty =
        BindableProperty.Create(nameof(ShowActions), typeof(bool), typeof(PageFrame), true);

    public bool ShowActions
    {
        get => (bool)GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    public static readonly BindableProperty CanSearchProperty =
        BindableProperty.Create(nameof(CanSearch), typeof(bool), typeof(PageFrame), true);

    public bool CanSearch
    {
        get => (bool)GetValue(CanSearchProperty);
        set => SetValue(CanSearchProperty, value);
    }

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(PageFrame), string.Empty,
            BindingMode.TwoWay);

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly BindableProperty ExtraIconProperty =
        BindableProperty.Create(nameof(ExtraIcon), typeof(string), typeof(PageFrame), string.Empty);

    public string ExtraIcon
    {
        get => (string)GetValue(ExtraIconProperty);
        set => SetValue(ExtraIconProperty, value);
    }

    public static readonly BindableProperty ExtraIconVisibleProperty =
        BindableProperty.Create(nameof(ExtraIconVisible), typeof(bool), typeof(PageFrame), false);

    public bool ExtraIconVisible
    {
        get => (bool)GetValue(ExtraIconVisibleProperty);
        set => SetValue(ExtraIconVisibleProperty, value);
    }

    public static readonly BindableProperty CanGoForwardProperty =
        BindableProperty.Create(nameof(CanGoForward), typeof(bool), typeof(PageFrame), false);

    /// <summary>Shows the forward (›) arrow in the header nav pill.</summary>
    public bool CanGoForward
    {
        get => (bool)GetValue(CanGoForwardProperty);
        set => SetValue(CanGoForwardProperty, value);
    }

    public static readonly BindableProperty ShowBottomNavProperty =
        BindableProperty.Create(nameof(ShowBottomNav), typeof(bool), typeof(PageFrame), false);

    /// <summary>Shows the BottomNavBar. Set to true for the three main tab pages.</summary>
    public bool ShowBottomNav
    {
        get => (bool)GetValue(ShowBottomNavProperty);
        set => SetValue(ShowBottomNavProperty, value);
    }

    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(nameof(ActiveTab), typeof(string), typeof(PageFrame), "Home");

    /// <summary>Highlights the matching tab in the bottom nav bar.</summary>
    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public static readonly BindableProperty BodyProperty =
        BindableProperty.Create(nameof(Body), typeof(View), typeof(PageFrame), null,
            propertyChanged: (b, _, n) =>
            {
                System.Diagnostics.Debug.WriteLine($"[PageFrame] Body propertyChanged → {(n as View)?.GetType().Name ?? "null"}");
                ((PageFrame)b).ApplyBody((View?)n);
            });

    /// <summary>The page body content. Set via the ContentProperty attribute on the class.</summary>
    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    // ── Events proxied from AppHeader ────────────────────────────────

    public event EventHandler? BackClicked;
    public event EventHandler? ForwardClicked;
    public event EventHandler? RefreshClicked;
    public event EventHandler? LogoutClicked;
    public event EventHandler? SearchCompleted;
    public event EventHandler? ExtraIconClicked;

    // ── Constructor ──────────────────────────────────────────────────

    /// <summary>
    /// True after InitializeComponent() has run and the inner layout is set up.
    /// Used by the Content property override to distinguish internal init from
    /// user-supplied body content.
    /// </summary>
    private bool _layoutInitialized;

    public PageFrame()
    {
        InitializeComponent();
        // MAUI's compiled XAML routes the inner Grid through Body (not Content) because of
        // [ContentProperty(nameof(Body))].  At that point BodySlot isn't assigned yet, so
        // ApplyBody discards it and base.Content is never set — causing a blank screen.
        // Explicitly wire the layout here, after InitializeComponent has fully run.
        System.Diagnostics.Debug.WriteLine($"[PageFrame] ctor post-init: RootGrid={(RootGrid is null ? "NULL" : "OK")}, BodySlot={(BodySlot is null ? "NULL" : "OK")}");
        base.Content = RootGrid;
        _layoutInitialized = true;
        System.Diagnostics.Debug.WriteLine("[PageFrame] ctor: base.Content = RootGrid ✓, _layoutInitialized = true");
    }

    /// <summary>
    /// Intercepts any post-init ContentView.Content assignment and routes it to
    /// BodySlot instead. This ensures the AppHeader + BottomNavBar layout is never
    /// accidentally replaced even if MAUI's XAML parser bypasses [ContentProperty].
    /// </summary>
    public new View? Content
    {
        get => base.Content;
        set
        {
            if (!_layoutInitialized)
                base.Content = value;   // InitializeComponent sets the layout grid
            else
                ApplyBody(value);       // Any later assignment is the page body
        }
    }

    // ── Body helper ──────────────────────────────────────────────────

    private void ApplyBody(View? body)
    {
        System.Diagnostics.Debug.WriteLine($"[PageFrame] ApplyBody: body={body?.GetType().Name ?? "null"}, BodySlot={(BodySlot is null ? "NULL" : "OK")}, init={_layoutInitialized}");
        if (BodySlot is null) return;
        BodySlot.Content = body;
        System.Diagnostics.Debug.WriteLine("[PageFrame] ApplyBody: BodySlot.Content assigned ✓");
    }

    // ── Internal relay handlers ──────────────────────────────────────

    private void OnHeaderBackClicked(object? sender, EventArgs e) =>
        BackClicked?.Invoke(this, e);

    private void OnHeaderForwardClicked(object? sender, EventArgs e) =>
        ForwardClicked?.Invoke(this, e);

    private void OnHeaderRefreshClicked(object? sender, EventArgs e) =>
        RefreshClicked?.Invoke(this, e);

    private void OnHeaderLogoutClicked(object? sender, EventArgs e) =>
        LogoutClicked?.Invoke(this, e);

    private void OnHeaderSearchCompleted(object? sender, EventArgs e) =>
        SearchCompleted?.Invoke(this, e);

    private void OnHeaderExtraIconClicked(object? sender, EventArgs e) =>
        ExtraIconClicked?.Invoke(this, e);
}
