using Microsoft.Extensions.Logging;
using VitaRaiz.Mobile.Data;
using VitaRaiz.Mobile.Services;

namespace VitaRaiz.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configurar SQLite database path
		string dbPath = Path.Combine(FileSystem.AppDataDirectory, "vitaraiz.db3");
		
		// Registrar servicios
		builder.Services.AddSingleton<LocalDatabase>(s => new LocalDatabase(dbPath));
		builder.Services.AddSingleton<ApiService>();
		builder.Services.AddSingleton<SyncService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
