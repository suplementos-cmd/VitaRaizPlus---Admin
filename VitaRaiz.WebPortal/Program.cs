using VitaRaiz.WebPortal.Components;
using VitaRaiz.WebPortal.Services;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using ApexCharts;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("===== VitaRaiz WebPortal Starting {0} =====", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var apiBase = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5299";
    builder.Services.AddHttpClient("VitaRaizApi", client =>
    {
        client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddAuthentication("Cookies")
        .AddCookie("Cookies", opts =>
        {
            opts.Cookie.Name = "VitaRaiz.Auth";
            opts.LoginPath = "/login";
            opts.LogoutPath = "/logout";
            opts.ExpireTimeSpan = TimeSpan.FromHours(8);
            opts.SlidingExpiration = true;
        });
    builder.Services.AddAuthorizationCore(options =>
    {
        // Permission-based policies — actual enforcement is server-side (API).
        // These registrations prevent the circuit crash when AuthorizeView uses Policy="...".
        var permissionNames = new[]
        {
            "CanCreateCustomer", "CanEditCustomer", "CanViewCustomers",
            "CanCreateSale",     "CanViewOwnSales", "CanViewAllSales", "CanAnnulSale",
            "CanCreatePayment",  "CanViewPayments", "CanApprovePayment",
            "CanViewReports",    "CanManageUsers",  "CanManageRoles",   "CanManageCatalogs",
        };
        foreach (var name in permissionNames)
            options.AddPolicy(name, policy => policy.RequireAuthenticatedUser());
    });
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddScoped<ApiService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<PermissionsService>();
    builder.Services.AddScoped<ThemeService>();
    builder.Services.AddScoped<NotificationService>();
    builder.Services.AddScoped<CustomAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(
        sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());

    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
        config.SnackbarConfiguration.ShowTransitionDuration = 300;
        config.SnackbarConfiguration.HideTransitionDuration = 300;
        config.SnackbarConfiguration.VisibleStateDuration = 3500;
        config.SnackbarConfiguration.MaxDisplayedSnackbars = 4;
    });

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    logger.Info("WebPortal ready.");
    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "WebPortal terminated unexpectedly");
    throw;
}
finally
{
    LogManager.Shutdown();
}
