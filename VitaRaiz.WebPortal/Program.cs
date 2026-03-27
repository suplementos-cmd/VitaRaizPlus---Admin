using VitaRaiz.WebPortal.Components;
using VitaRaiz.WebPortal.Services;
using Microsoft.AspNetCore.Components.Authorization;
using NLog;
using NLog.Web;

// ═══ NLog Initialization ═══
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("================================================================================");
logger.Info($"VitaRaiz WebPortal Starting - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
logger.Info("================================================================================");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure NLog for dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Autenticación y autorización
    builder.Services.AddAuthentication("Cookies")
        .AddCookie("Cookies", options =>
        {
            options.Cookie.Name = "VitaRaiz.Auth";
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ApiService>();

    // Registro ÚNICO del AuthenticationStateProvider - una sola instancia sirve ambas inyecciones
    builder.Services.AddScoped<CustomAuthenticationStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
        sp.GetRequiredService<CustomAuthenticationStateProvider>());

    // Cascading authentication state
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
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

    logger.Info("WebPortal configured successfully. Starting application...");
    
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
