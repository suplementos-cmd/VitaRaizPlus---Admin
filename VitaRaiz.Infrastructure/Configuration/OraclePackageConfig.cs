namespace VitaRaiz.Infrastructure.Configuration;

/// <summary>
/// Configuración centralizada para el paquete Oracle principal
/// </summary>
public static class OraclePackageConfig
{
    /// <summary>
    /// Esquema de base de datos
    /// </summary>
    public const string Schema = "SALESAPP";
    
    /// <summary>
    /// Nombre del paquete principal
    /// </summary>
    public const string PackageName = "EM_VITARAIZ_AD";
    
    /// <summary>
    /// Nombre completo del paquete (Schema.PackageName)
    /// </summary>
    public static string FullPackageName => $"{Schema}.{PackageName}";
    
    /// <summary>
    /// Genera el nombre completo de un procedimiento/función del paquete
    /// </summary>
    public static string GetProcedureName(string procedureName) 
        => $"{FullPackageName}.{procedureName}";
    
    /// <summary>
    /// Genera una llamada a función Oracle que retorna valor
    /// </summary>
    public static string GetFunctionCall(string functionName, string parameters)
        => $"BEGIN :result := {FullPackageName}.{functionName}({parameters}); END;";
}
