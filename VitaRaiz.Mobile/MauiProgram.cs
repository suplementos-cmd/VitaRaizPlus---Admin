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
		builder.Services.AddSingleton<PermissionsService>();
		
		// Registrar páginas con DI
		builder.Services.AddTransient<SaleDetailPage>();
		builder.Services.AddTransient<CreateSalePage>();
		builder.Services.AddTransient<RegistrarCobroPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Elimina el fondo y underline nativo de Material Design en Android
		// y la línea de foco nativa de WinUI en Windows, para que Entry/Picker/DatePicker/Editor
		// no dibujen encima del Border decorativo de MAUI.
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("VitaRaizNoUnderline", (handler, _) =>
		{
#if ANDROID
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
			handler.PlatformView.Background = null;
#elif WINDOWS
			var tb = handler.PlatformView;
			var clearBorder = () =>
			{
				tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
				tb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			};
			clearBorder();
			// WinUI re-applies the focus underline via ControlTemplate on GotFocus — suppress it.
			tb.GotFocus  += (_, _) => clearBorder();
			tb.LostFocus += (_, _) => clearBorder();
#endif
		});
		Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("VitaRaizNoUnderline", (handler, _) =>
		{
#if ANDROID
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
			handler.PlatformView.Background = null;
#elif WINDOWS
			var tb = handler.PlatformView;
			var clearBorder = () =>
			{
				tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
				tb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			};
			clearBorder();
			tb.GotFocus  += (_, _) => clearBorder();
			tb.LostFocus += (_, _) => clearBorder();
#endif
		});
		Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("VitaRaizNoUnderline", (handler, _) =>
		{
#if ANDROID
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
			handler.PlatformView.Background = null;
#elif WINDOWS
			var tb = handler.PlatformView;
			var clearBorder = () =>
			{
				tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
				tb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			};
			clearBorder();
			tb.GotFocus  += (_, _) => clearBorder();
			tb.LostFocus += (_, _) => clearBorder();
#endif
		});
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("VitaRaizNoUnderline", (handler, _) =>
		{
#if ANDROID
			handler.PlatformView.BackgroundTintList =
				Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
			handler.PlatformView.Background = null;
#elif WINDOWS
			var tb = handler.PlatformView;
			var clearBorder = () =>
			{
				tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
				tb.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
			};
			clearBorder();
			tb.GotFocus  += (_, _) => clearBorder();
			tb.LostFocus += (_, _) => clearBorder();
#endif
		});

		var app = builder.Build();
		
		var logger = AppLogger.Get();
		logger.Info("MAUI Application configured successfully");
		
		return app;
	}
}
