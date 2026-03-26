using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using VitaRaiz.Infrastructure.Configuration;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.API.Controllers;

/// <summary>
/// Controlador de diagnóstico - SIN SQL hardcodeado, usa solo repositorios y package SALESAPP.EM_VITARAIZ_AD
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DebugController> _logger;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICatalogRepository _catalogRepository;

    public DebugController(
        IConfiguration configuration, 
        ILogger<DebugController> logger,
        ICustomerRepository customerRepository,
        ISaleRepository saleRepository,
        IPaymentRepository paymentRepository,
        ICatalogRepository catalogRepository)
    {
        _configuration = configuration;
        _logger = logger;
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _paymentRepository = paymentRepository;
        _catalogRepository = catalogRepository;
    }

    /// <summary>
    /// Prueba conexión a Oracle - Sin autenticación
    /// </summary>
    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");
            _logger.LogInformation("[DebugController] Testing Oracle connection");

            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();
            
            var result = new
            {
                Status = "Connected Successfully",
                ServerVersion = connection.ServerVersion,
                Database = connection.Database,
                State = connection.State.ToString(),
                Schema = OraclePackageConfig.Schema,
                Package = OraclePackageConfig.FullPackageName
            };

            _logger.LogInformation("[DebugController] Oracle connection successful");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DebugController] Failed to connect to Oracle database");
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Prueba repositorio de clientes - Sin autenticación
    /// Usa Entity Framework sobre tablas Oracle
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomersDebug()
    {
        try
        {
            _logger.LogInformation("[DebugController] Testing customers via repository");
            
            var customers = await _customerRepository.GetCustomersAsync(null, null, null, null);
            
            _logger.LogInformation("[DebugController] Repository returned {Count} customers", customers.Count);
            return Ok(new { Count = customers.Count, Customers = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DebugController] Failed to query customers via repository");
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Prueba repositorio de ventas - Sin autenticación
    /// Usa Entity Framework sobre tablas Oracle
    /// </summary>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesDebug()
    {
        try
        {
            _logger.LogInformation("[DebugController] Testing sales via repository");
            
            var sales = await _saleRepository.GetSalesAsync(null, null, null, null, null);
            
            _logger.LogInformation("[DebugController] Repository returned {Count} sales", sales.Count);
            return Ok(new { Count = sales.Count, Sales = sales });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DebugController] Failed to query sales via repository");
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Prueba ventas activas - Sin autenticación
    /// Usa repositorio con lógica de negocio para filtrar EN_PROCESO
    /// </summary>
    [HttpGet("sales-active")]
    public async Task<IActionResult> GetActiveSalesDebug()
    {
        try
        {
            _logger.LogInformation("[DebugController] Testing active sales via repository");
            
            var sales = await _saleRepository.GetActiveSalesAsync(null);
            
            _logger.LogInformation("[DebugController] Repository returned {Count} active sales", sales.Count);
            return Ok(new { Count = sales.Count, Sales = sales });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DebugController] Failed to query active sales via repository");
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Prueba repositorio de pagos - Sin autenticación
    /// Usa Entity Framework sobre tablas Oracle
    /// </summary>
    [HttpGet("payments")]
    public async Task<IActionResult> GetPaymentsDebug()
    {
        try
        {
            _logger.LogInformation("[DebugController] Testing payments via repository");
            
            var payments = await _paymentRepository.GetPaymentsAsync(null, null, null, null, null, null);
            
            _logger.LogInformation("[DebugController] Repository returned {Count} payments", payments.Count);
            return Ok(new { Count = payments.Count, Payments = payments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DebugController] Failed to query payments via repository");
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Prueba función de catálogos del package Oracle - Sin autenticación
    /// Llama a: SALESAPP.EM_VITARAIZ_AD.fn_get_sale_statuses()
    /// </summary>
    [HttpGet("catalogs")]
    public async Task<IActionResult> TestCatalogFunction()
    {
        try
        {
            _logger.LogInformation("[DebugController] Testing catalog via package {Package}", OraclePackageConfig.FullPackageName);
            
            var statuses = await _catalogRepository.GetSaleStatusesAsync();
            
            _logger.LogInformation("[DebugController] Catalog function returned {Count} statuses", statuses.Count);
            return Ok(new 
            { 
                Count = statuses.Count, 
                PackageUsed = OraclePackageConfig.FullPackageName,
                Statuses = statuses 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DebugController] Failed to execute catalog function via package {Package}", OraclePackageConfig.FullPackageName);
            return StatusCode(500, new 
            { 
                Error = ex.Message, 
                StackTrace = ex.StackTrace,
                PackageAttempted = OraclePackageConfig.FullPackageName
            });
        }
    }

    /// <summary>
    /// Prueba si el JWT token está llegando correctamente - Sin autenticación
    /// </summary>
    [HttpGet("test-jwt")]
    public IActionResult TestJwt()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var hasClaims = User.Identity?.IsAuthenticated ?? false;
        
        var result = new
        {
            HasAuthorizationHeader = !string.IsNullOrEmpty(authHeader),
            AuthorizationHeaderPreview = authHeader.Length > 30 ? authHeader.Substring(0, 30) + "..." : authHeader,
            IsAuthenticated = hasClaims,
            Username = User.Identity?.Name ?? "Anonymous",
            ClaimsCount = User.Claims.Count(),
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        };
        
        _logger.LogInformation("[DebugController] JWT Test - Has Auth: {HasAuth}, Authenticated: {IsAuth}", 
            !string.IsNullOrEmpty(authHeader), hasClaims);
        return Ok(result);
    }
}
