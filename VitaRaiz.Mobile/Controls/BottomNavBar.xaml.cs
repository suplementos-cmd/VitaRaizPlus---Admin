namespace VitaRaiz.Mobile.Controls;

public partial class BottomNavBar : ContentView
{
    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(nameof(ActiveTab), typeof(string), typeof(BottomNavBar), "Home",
            propertyChanged: (b, _, n) => ((BottomNavBar)b).ApplyActiveTab((string)n));

    /// <summary>
    /// The currently active tab. Accepted values: "Home", "Sales", "Customers".
    /// </summary>
    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        ApplyActiveTab(ActiveTab);
    }

    private void ApplyActiveTab(string activeTab)
    {
        Color themeColor    = GetThemeColor();
        Color inactiveColor = GetInactiveColor();

        // Reset all
        SetTabStyle(IconInicio,   TextInicio,   PillInicio,   TabInicio,   false, themeColor, inactiveColor);
        SetTabStyle(IconVentas,   TextVentas,   PillVentas,   TabVentas,   false, themeColor, inactiveColor);
        SetTabStyle(IconClientes, TextClientes, PillClientes, TabClientes, false, themeColor, inactiveColor);

        // Activate
        switch (activeTab)
        {
            case "Home":      SetTabStyle(IconInicio,   TextInicio,   PillInicio,   TabInicio,   true, themeColor, inactiveColor); break;
            case "Sales":     SetTabStyle(IconVentas,   TextVentas,   PillVentas,   TabVentas,   true, themeColor, inactiveColor); break;
            case "Customers": SetTabStyle(IconClientes, TextClientes, PillClientes, TabClientes, true, themeColor, inactiveColor); break;
        }
    }

    private static Color GetThemeColor()
    {
        if (Application.Current?.Resources.TryGetValue("ThemeColor", out var value) == true && value is Color c)
            return c;
        return Color.FromArgb("#9C27B0"); // fallback
    }

    private static Color GetInactiveColor()
    {
        if (Application.Current?.Resources.TryGetValue("TextMuted", out var value) == true && value is Color c)
            return c;
        return Color.FromArgb("#9CA3AF"); // fallback
    }

    private static void SetTabStyle(Label icon, Label text, BoxView pill, View tab, bool active, Color themeColor, Color inactiveColor)
    {
        var color = active ? themeColor : inactiveColor;
        icon.TextColor = color;
        text.TextColor = color;
        text.FontAttributes = active ? FontAttributes.Bold : FontAttributes.None;
        text.FontSize = active ? 9.5 : 9;
        pill.IsVisible = active;
        tab.Scale = 1.0;
        // Show/hide the themed flat background (sibling BoxView named PillBgXxx)
        if (tab is Grid g)
        {
            foreach (var child in g.Children)
                if (child is BoxView bv && bv != pill) { bv.IsVisible = active; break; }
        }
    }

    private async void OnTabInicio(object? sender, EventArgs e)
    {
        await AnimateTabPress(TabInicio);
        if (ActiveTab != "Home")
            await Shell.Current.GoToAsync("//HomePage");
    }

    private async void OnTabVentas(object? sender, EventArgs e)
    {
        await AnimateTabPress(TabVentas);
        if (ActiveTab != "Sales")
            await Shell.Current.GoToAsync("//SalesPage");
    }

    private async void OnTabClientes(object? sender, EventArgs e)
    {
        await AnimateTabPress(TabClientes);
        if (ActiveTab != "Customers")
            await Shell.Current.GoToAsync("//CustomersPage");
    }

    private static async Task AnimateTabPress(View tab)
    {
        await tab.ScaleTo(0.88, 70, Easing.CubicOut);
        await tab.ScaleTo(1.00, 80, Easing.CubicIn);
    }
}