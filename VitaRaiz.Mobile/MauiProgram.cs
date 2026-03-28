using Microsoft.Extensions.Logging;
using VitaRaiz.Mobile.Data;
using VitaRaiz.Mobile.Pages;
using VitaRaiz.Mobile.Services;
using NLog;
using NLog.Extensions.Logging;

namespace VitaRaiz.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// Initialize NLog FIRST before anything else
		AppLogger.Initialize();
		
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configure logging with NLog
		builder.Logging.ClearProviders();
		builder.Logging.AddNLog();

		// Configurar SQLite database path
		string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
		
		// Registrar servicios
		builder.Services.AddSingleton<LocalDatabase>(s => new LocalDatabase(dbPath));
		builder.Services.AddSingleton<ApiService>();
		builder.Services.AddSingleton<CatalogService>();
		builder.Services.AddSingleton<ThemeService>();
		builder.Services.AddSingleton<SyncService>();
		
		// Registrar páginas con DI
		builder.Services.AddTransient<SaleDetailPage>();
		builder.Services.AddTransient<CreateSalePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		
		var logger = AppLogger.Get();
		logger.Info("MAUI Application configured successfully");
		
		return app;
	}
}
