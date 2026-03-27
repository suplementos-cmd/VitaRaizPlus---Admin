using NLog;
using System.Runtime.CompilerServices;

namespace VitaRaiz.Mobile.Services;

/// <summary>
/// Centralized logging service using NLog.
/// Provides structured logging with automatic caller information.
/// Thread-safe and performant.
/// </summary>
public static class AppLogger
{
    private static readonly Dictionary<string, Logger> _loggers = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Get logger for calling class automatically
    /// </summary>
    public static Logger Get([CallerFilePath] string filePath = "")
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
        
        if (!_loggers.ContainsKey(className))
        {
            lock (_lock)
            {
                if (!_loggers.ContainsKey(className))
                {
                    _loggers[className] = LogManager.GetLogger(className);
                }
            }
        }
        
        return _loggers[className];
    }

    /// <summary>
    /// Initialize NLog configuration
    /// </summary>
    public static void Initialize()
    {
        System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        System.Diagnostics.Debug.WriteLine("║  APPLOGGER INITIALIZATION START                               ║");
        System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        
        try
        {
            // Ensure log directory exists in MAUI AppDataDirectory
            var logDir = Path.Combine(FileSystem.AppDataDirectory, "Logs");
            System.Diagnostics.Debug.WriteLine($"[AppLogger] Creating log directory: {logDir}");
            Directory.CreateDirectory(logDir);
            System.Diagnostics.Debug.WriteLine($"[AppLogger] Log directory created/verified OK");

            // Use PROGRAMMATIC configuration (más confiable que archivo XML en MAUI)
            var config = new NLog.Config.LoggingConfiguration();
            System.Diagnostics.Debug.WriteLine("[AppLogger] LoggingConfiguration created");
            
            // File Target - Log completo con rotación diaria
            var logFilePath = Path.Combine(logDir, $"vitaraiz-{DateTime.Now:yyyy-MM-dd}.log");
            System.Diagnostics.Debug.WriteLine($"[AppLogger] Log file path: {logFilePath}");
            
            var fileTarget = new NLog.Targets.FileTarget("fileTarget")
            {
                FileName = logFilePath,
                Layout = "${longdate}|${level:uppercase=true:padding=-5}|${logger:shortName=true}|${message}${onexception:inner=${newline}${exception:format=tostring}}",
                ArchiveFileName = Path.Combine(logDir, $"vitaraiz-{DateTime.Now:yyyy-MM-dd}.{{#}}.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                MaxArchiveFiles = 7,
                ConcurrentWrites = true,
                KeepFileOpen = false
            };
            System.Diagnostics.Debug.WriteLine("[AppLogger] FileTarget configured");
            
            // Debug Target - Ventana de depuración de Visual Studio
            var debugTarget = new NLog.Targets.DebugTarget("debugTarget")
            {
                Layout = "${time}|${level:uppercase=true:padding=-5}|${logger:shortName=true}|${message}"
            };
            System.Diagnostics.Debug.WriteLine("[AppLogger] DebugTarget configured");
            
            // Console Target - Para logs importantes
            var consoleTarget = new NLog.Targets.ConsoleTarget("consoleTarget")
            {
                Layout = "${time}|${level:uppercase=true:padding=-5}|${logger:shortName=true}|${message}"
            };
            System.Diagnostics.Debug.WriteLine("[AppLogger] ConsoleTarget configured");

            // Rules: TODO desde Trace (nivel más bajo) para capturar absolutamente todo
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, debugTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
            System.Diagnostics.Debug.WriteLine("[AppLogger] Rules added: Trace->Fatal to all targets");
            
            LogManager.Configuration = config;
            System.Diagnostics.Debug.WriteLine("[AppLogger] LogManager.Configuration assigned");

            var logger = Get();
            System.Diagnostics.Debug.WriteLine("[AppLogger] Logger instance retrieved");
            
            logger.Info("================================================================================");
            logger.Info($"VitaRaiz Mobile Application Started - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logger.Info($"Log File: {logFilePath}");
            logger.Info($"Platform: {DeviceInfo.Platform} {DeviceInfo.VersionString}");
            logger.Info($"Device: {DeviceInfo.Model} - {DeviceInfo.Manufacturer}");
            logger.Info("================================================================================");
            System.Diagnostics.Debug.WriteLine("[AppLogger] Initial log entries written");
            
            // Force flush to disk
            LogManager.Flush();
            System.Diagnostics.Debug.WriteLine("[AppLogger] LogManager.Flush() completed");
            
            System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  APPLOGGER INITIALIZATION SUCCESS                             ║");
            System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  APPLOGGER INITIALIZATION FAILED!!!                           ║");
            System.Diagnostics.Debug.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[AppLogger] CRITICAL - Failed to initialize: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[AppLogger] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[AppLogger] StackTrace: {ex.StackTrace}");
            throw; // Re-throw para que sea visible
        }
    }
    
    /// <summary>
    /// Shutdown NLog (call on app exit)
    /// </summary>
    public static void Shutdown()
    {
        LogManager.Shutdown();
    }

    /// <summary>
    /// Log exception with full details
    /// </summary>
    public static void LogException(this Logger logger, Exception ex, string context, 
        [CallerMemberName] string memberName = "")
    {
        logger.Error(ex, $"[{memberName}] {context}");
        logger.Error($"Exception Type: {ex.GetType().FullName}");
        logger.Error($"Message: {ex.Message}");
        logger.Error($"StackTrace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            logger.Error($"InnerException: {ex.InnerException.Message}");
            logger.Error($"InnerStackTrace: {ex.InnerException.StackTrace}");
        }
    }

    /// <summary>
    /// Log method entry with parameters
    /// </summary>
    public static void LogEntry(this Logger logger, string message = "", 
        [CallerMemberName] string memberName = "")
    {
        logger.Debug($"[{memberName}] ENTRY {message}");
    }

    /// <summary>
    /// Log method exit
    /// </summary>
    public static void LogExit(this Logger logger, string message = "", 
        [CallerMemberName] string memberName = "")
    {
        logger.Debug($"[{memberName}] EXIT {message}");
    }
}
