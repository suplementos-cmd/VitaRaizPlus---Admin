using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;

namespace VitaRaiz.API.Controllers;

/// <summary>
/// Controlador para la gestión de empleados, asistencias, nóminas y comisiones
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(IEmployeeRepository employeeRepository, ILogger<EmployeesController> logger)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            return userId;
        return null;
    }

    // ========================================================================
    // GESTIÓN DE EMPLEADOS
    // ========================================================================

    /// <summary>
    /// Obtener lista de empleados con filtros opcionales
    /// </summary>
    /// <param name="status">Filtrar por estado: ACTIVO, BAJA, SUSPENDIDO</param>
    /// <param name="department">Filtrar por departamento</param>
    /// <param name="searchTerm">Búsqueda por nombre o código de empleado</param>
    [HttpGet]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? status = null,
        [FromQuery] string? department = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var employees = await _employeeRepository.GetEmployeesAsync(status, department, searchTerm);
            _logger.LogInformation("[EmployeesController] Returning {Count} employees", employees.Count);
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting employees");
            return StatusCode(500, new { message = "Error al obtener empleados", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener un empleado por ID con todos sus datos
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        try
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            
            if (employee == null)
                return NotFound(new { message = "Empleado no encontrado" });

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting employee {Id}", id);
            return StatusCode(500, new { message = "Error al obtener empleado", error = ex.Message });
        }
    }

    /// <summary>
    /// Registrar un nuevo empleado
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.EmployeeCode))
                return BadRequest(new { message = "El código de empleado es requerido" });

            var employeeId = await _employeeRepository.RegisterEmployeeAsync(dto, GetCurrentUserId());
            
            _logger.LogInformation("[EmployeesController] Employee created with ID: {Id}, Code: {Code}", employeeId, dto.EmployeeCode);
            
            return CreatedAtAction(nameof(GetEmployeeById), new { id = employeeId }, new { employeeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error creating employee");
            return StatusCode(500, new { message = "Error al crear empleado", error = ex.Message });
        }
    }

    /// <summary>
    /// Actualizar datos de un empleado
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto dto)
    {
        try
        {
            await _employeeRepository.UpdateEmployeeAsync(id, dto, GetCurrentUserId());
            
            _logger.LogInformation("[EmployeesController] Employee {Id} updated", id);
            
            return Ok(new { message = "Empleado actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error updating employee {Id}", id);
            return StatusCode(500, new { message = "Error al actualizar empleado", error = ex.Message });
        }
    }

    /// <summary>
    /// Dar de baja a un empleado (terminación de empleo)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "AdminFull")]
    public async Task<IActionResult> TerminateEmployee(
        int id,
        [FromQuery] string? reason = null,
        [FromQuery] DateTime? terminationDate = null)
    {
        try
        {
            await _employeeRepository.TerminateEmployeeAsync(id, terminationDate, reason, GetCurrentUserId());
            
            _logger.LogInformation("[EmployeesController] Employee {Id} terminated. Reason: {Reason}", id, reason);
            
            return Ok(new { message = "Empleado dado de baja exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error terminating employee {Id}", id);
            return StatusCode(500, new { message = "Error al dar de baja empleado", error = ex.Message });
        }
    }

    // ========================================================================
    // CONTROL DE ASISTENCIAS
    // ========================================================================

    /// <summary>
    /// Registrar asistencia de un empleado
    /// </summary>
    [HttpPost("{id:int}/attendance")]
    public async Task<IActionResult> RegisterAttendance(int id, [FromBody] CreateAttendanceDto dto)
    {
        try
        {
            // Validar que el ID del empleado coincida
            if (dto.EmployeeId != id)
                return BadRequest(new { message = "El ID del empleado no coincide" });

            var attendanceId = await _employeeRepository.RegisterAttendanceAsync(dto);
            
            _logger.LogInformation("[EmployeesController] Attendance registered for employee {Id}: {Type}", id, dto.AttendanceType);
            
            return CreatedAtAction(nameof(GetEmployeeById), new { id }, new { attendanceId, message = "Asistencia registrada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error registering attendance for employee {Id}", id);
            return StatusCode(500, new { message = "Error al registrar asistencia", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener asistencias de un empleado
    /// </summary>
    [HttpGet("{id:int}/attendance")]
    public async Task<IActionResult> GetAttendanceByEmployee(
        int id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var attendances = await _employeeRepository.GetAttendanceByEmployeeAsync(id, startDate, endDate);
            
            return Ok(attendances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting attendance for employee {Id}", id);
            return StatusCode(500, new { message = "Error al obtener asistencias", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener resumen mensual de asistencias de un empleado
    /// </summary>
    [HttpGet("{id:int}/attendance/summary")]
    public async Task<IActionResult> GetAttendanceSummary(
        int id,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        try
        {
            var summary = await _employeeRepository.GetAttendanceSummaryAsync(id, year, month);
            
            if (summary == null)
                return NotFound(new { message = "No se encontraron datos de asistencia" });

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting attendance summary for employee {Id}", id);
            return StatusCode(500, new { message = "Error al obtener resumen de asistencias", error = ex.Message });
        }
    }

    // ========================================================================
    // NÓMINA
    // ========================================================================

    /// <summary>
    /// Generar nómina para un empleado en un periodo
    /// </summary>
    [HttpPost("{id:int}/payroll")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> GeneratePayroll(int id, [FromBody] GeneratePayrollDto dto)
    {
        try
        {
            if (dto.EmployeeId != id)
                return BadRequest(new { message = "El ID del empleado no coincide" });

            var payrollId = await _employeeRepository.GeneratePayrollAsync(
                id, 
                dto.PeriodStart, 
                dto.PeriodEnd, 
                GetCurrentUserId());
            
            _logger.LogInformation("[EmployeesController] Payroll {PayrollId} generated for employee {Id}", payrollId, id);
            
            return CreatedAtAction(nameof(GetPayrollByEmployee), new { id }, new { payrollId, message = "Nómina generada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error generating payroll for employee {Id}", id);
            return StatusCode(500, new { message = "Error al generar nómina", error = ex.Message });
        }
    }

    /// <summary>
    /// Aprobar una nómina
    /// </summary>
    [HttpPut("payroll/{payrollId:int}/approve")]
    [Authorize(Roles = "AdminFull")]
    public async Task<IActionResult> ApprovePayroll(int payrollId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Usuario no autenticado" });

            await _employeeRepository.ApprovePayrollAsync(payrollId, userId.Value);
            
            _logger.LogInformation("[EmployeesController] Payroll {PayrollId} approved by user {UserId}", payrollId, userId);
            
            return Ok(new { message = "Nómina aprobada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error approving payroll {PayrollId}", payrollId);
            return StatusCode(500, new { message = "Error al aprobar nómina", error = ex.Message });
        }
    }

    /// <summary>
    /// Marcar nómina como pagada
    /// </summary>
    [HttpPut("payroll/{payrollId:int}/pay")]
    [Authorize(Roles = "AdminFull")]
    public async Task<IActionResult> PayPayroll(int payrollId, [FromBody] PayPayrollDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Usuario no autenticado" });

            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
                return BadRequest(new { message = "El método de pago es requerido" });

            await _employeeRepository.PayPayrollAsync(
                payrollId, 
                dto.PaymentDate, 
                dto.PaymentMethod, 
                dto.PaymentReference, 
                userId.Value);
            
            _logger.LogInformation("[EmployeesController] Payroll {PayrollId} paid by user {UserId}", payrollId, userId);
            
            return Ok(new { message = "Nómina marcada como pagada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error paying payroll {PayrollId}", payrollId);
            return StatusCode(500, new { message = "Error al marcar nómina como pagada", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener nóminas de un empleado
    /// </summary>
    [HttpGet("{id:int}/payroll")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetPayrollByEmployee(int id, [FromQuery] int? year = null)
    {
        try
        {
            var payrolls = await _employeeRepository.GetPayrollByEmployeeAsync(id, year);
            
            return Ok(payrolls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting payroll for employee {Id}", id);
            return StatusCode(500, new { message = "Error al obtener nóminas", error = ex.Message });
        }
    }

    // ========================================================================
    // COMISIONES
    // ========================================================================

    /// <summary>
    /// Registrar comisión por venta (normalmente llamado automáticamente)
    /// </summary>
    [HttpPost("{id:int}/commissions/from-sale")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> RegisterCommissionFromSale(int id, [FromBody] RegisterCommissionFromSaleDto dto)
    {
        try
        {
            if (dto.EmployeeId != id)
                return BadRequest(new { message = "El ID del empleado no coincide" });

            var commissionId = await _employeeRepository.RegisterCommissionFromSaleAsync(
                id, 
                dto.SaleId, 
                dto.CommissionRate);
            
            _logger.LogInformation("[EmployeesController] Commission {CommissionId} registered for employee {Id} from sale {SaleId}", 
                commissionId, id, dto.SaleId);
            
            return CreatedAtAction(nameof(GetEmployeeById), new { id }, new { commissionId, message = "Comisión registrada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error registering commission from sale for employee {Id}", id);
            return StatusCode(500, new { message = "Error al registrar comisión", error = ex.Message });
        }
    }

    /// <summary>
    /// Registrar comisión por cobro (normalmente llamado automáticamente)
    /// </summary>
    [HttpPost("{id:int}/commissions/from-payment")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> RegisterCommissionFromPayment(int id, [FromBody] RegisterCommissionFromPaymentDto dto)
    {
        try
        {
            if (dto.EmployeeId != id)
                return BadRequest(new { message = "El ID del empleado no coincide" });

            var commissionId = await _employeeRepository.RegisterCommissionFromPaymentAsync(
                id, 
                dto.PaymentId, 
                dto.CommissionRate);
            
            _logger.LogInformation("[EmployeesController] Commission {CommissionId} registered for employee {Id} from payment {PaymentId}", 
                commissionId, id, dto.PaymentId);
            
            return CreatedAtAction(nameof(GetEmployeeById), new { id }, new { commissionId, message = "Comisión registrada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error registering commission from payment for employee {Id}", id);
            return StatusCode(500, new { message = "Error al registrar comisión", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener comisiones pendientes (sin procesar en nómina)
    /// </summary>
    [HttpGet("commissions/pending")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetCommissionsPending([FromQuery] int? employeeId = null)
    {
        try
        {
            var commissions = await _employeeRepository.GetCommissionsPendingAsync(employeeId);
            
            return Ok(commissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting pending commissions");
            return StatusCode(500, new { message = "Error al obtener comisiones pendientes", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener comisiones pendientes de un empleado específico
    /// </summary>
    [HttpGet("{id:int}/commissions/pending")]
    public async Task<IActionResult> GetEmployeeCommissionsPending(int id)
    {
        try
        {
            var commissions = await _employeeRepository.GetCommissionsPendingAsync(id);
            
            return Ok(commissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting pending commissions for employee {Id}", id);
            return StatusCode(500, new { message = "Error al obtener comisiones pendientes", error = ex.Message });
        }
    }

    // ========================================================================
    // DOCUMENTOS
    // ========================================================================

    /// <summary>
    /// Subir documento de empleado
    /// </summary>
    [HttpPost("{id:int}/documents")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> UploadDocument(int id, [FromBody] UploadEmployeeDocumentDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            dto.EmployeeId = id;
            
            var documentId = await _employeeRepository.UploadEmployeeDocumentAsync(dto, userId);
            
            _logger.LogInformation("[EmployeesController] Document uploaded for employee {Id} by user {UserId}", id, userId);
            
            return CreatedAtAction(nameof(GetDocumentById), new { id, documentId }, new { documentId, message = "Documento subido exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error uploading document for employee {Id}", id);
            return StatusCode(500, new { message = "Error al subir documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener documentos de un empleado
    /// </summary>
    [HttpGet("{id:int}/documents")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetEmployeeDocuments(int id, [FromQuery] int? docTypeId = null)
    {
        try
        {
            var documents = await _employeeRepository.GetEmployeeDocumentsAsync(id, docTypeId);
            
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting documents for employee {Id}", id);
            return StatusCode(500, new { message = "Error al obtener documentos", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener un documento específico
    /// </summary>
    [HttpGet("{id:int}/documents/{documentId:int}")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetDocumentById(int id, int documentId)
    {
        try
        {
            var document = await _employeeRepository.GetDocumentByIdAsync(documentId);
            
            if (document == null)
                return NotFound(new { message = "Documento no encontrado" });
            
            if (document.EmployeeId != id)
                return BadRequest(new { message = "El documento no pertenece a este empleado" });
            
            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error al obtener documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Descargar archivo de documento
    /// </summary>
    [HttpGet("{id:int}/documents/{documentId:int}/download")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> DownloadDocument(int id, int documentId)
    {
        try
        {
            var document = await _employeeRepository.GetDocumentByIdAsync(documentId);
            
            if (document == null)
                return NotFound(new { message = "Documento no encontrado" });
            
            if (document.EmployeeId != id)
                return BadRequest(new { message = "El documento no pertenece a este empleado" });

            // Verificar si el archivo existe
            if (!System.IO.File.Exists(document.FilePath))
                return NotFound(new { message = "Archivo físico no encontrado" });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);
            return File(fileBytes, document.MimeType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error downloading document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error al descargar documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Eliminar documento de empleado
    /// </summary>
    [HttpDelete("{id:int}/documents/{documentId:int}")]
    [Authorize(Roles = "AdminFull,Admin")]
    public async Task<IActionResult> DeleteDocument(int id, int documentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var document = await _employeeRepository.GetDocumentByIdAsync(documentId);
            
            if (document == null)
                return NotFound(new { message = "Documento no encontrado" });
            
            if (document.EmployeeId != id)
                return BadRequest(new { message = "El documento no pertenece a este empleado" });

            await _employeeRepository.DeleteEmployeeDocumentAsync(documentId, userId);
            
            // Opcional: Eliminar archivo físico
            if (System.IO.File.Exists(document.FilePath))
            {
                System.IO.File.Delete(document.FilePath);
            }
            
            _logger.LogInformation("[EmployeesController] Document {DocumentId} deleted by user {UserId}", documentId, userId);
            
            return Ok(new { message = "Documento eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error deleting document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Error al eliminar documento", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener tipos de documentos disponibles
    /// </summary>
    [HttpGet("documents/types")]
    [Authorize(Roles = "AdminFull,Admin,Manager")]
    public async Task<IActionResult> GetDocumentTypes()
    {
        try
        {
            var types = await _employeeRepository.GetDocumentTypesAsync();
            
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmployeesController] Error getting document types");
            return StatusCode(500, new { message = "Error al obtener tipos de documentos", error = ex.Message });
        }
    }
}

// ========================================================================
// REQUEST DTOs ADICIONALES
// ========================================================================

public class PayPayrollDto
{
    public DateTime? PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
}

public class RegisterCommissionFromSaleDto
{
    public int EmployeeId { get; set; }
    public int SaleId { get; set; }
    public decimal CommissionRate { get; set; }
}

public class RegisterCommissionFromPaymentDto
{
    public int EmployeeId { get; set; }
    public int PaymentId { get; set; }
    public decimal CommissionRate { get; set; }
}
