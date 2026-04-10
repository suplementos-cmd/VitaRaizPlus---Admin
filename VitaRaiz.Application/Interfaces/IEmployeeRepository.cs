using VitaRaiz.Application.DTOs;

namespace VitaRaiz.Application.Interfaces;

/// <summary>
/// Repositorio para gestión de empleados, asistencias, nóminas y comisiones
/// </summary>
public interface IEmployeeRepository
{
    // ========================================================================
    // GESTIÓN DE EMPLEADOS
    // ========================================================================
    
    /// <summary>Registra un nuevo empleado en el sistema</summary>
    Task<int> RegisterEmployeeAsync(CreateEmployeeDto dto, int? createdBy = null);
    
    /// <summary>Actualiza datos de un empleado existente</summary>
    Task UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto dto, int? updatedBy = null);
    
    /// <summary>Da de baja a un empleado</summary>
    Task TerminateEmployeeAsync(int employeeId, DateTime? terminationDate = null, string? reason = null, int? updatedBy = null);
    
    /// <summary>Obtiene lista de empleados con filtros opcionales</summary>
    Task<List<EmployeeDto>> GetEmployeesAsync(string? status = null, string? department = null, string? searchTerm = null);
    
    /// <summary>Obtiene un empleado por ID</summary>
    Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId);
    
    // ========================================================================
    // CONTROL DE ASISTENCIAS
    // ========================================================================
    
    /// <summary>Registra asistencia de un empleado</summary>
    Task<int> RegisterAttendanceAsync(CreateAttendanceDto dto);
    
    /// <summary>Obtiene asistencias de un empleado en un rango de fechas</summary>
    Task<List<AttendanceDto>> GetAttendanceByEmployeeAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>Obtiene resumen de asistencias de un empleado por año/mes</summary>
    Task<AttendanceSummaryDto?> GetAttendanceSummaryAsync(int employeeId, int? year = null, int? month = null);
    
    // ========================================================================
    // NÓMINA
    // ========================================================================
    
    /// <summary>Genera nómina para un empleado en un periodo</summary>
    Task<int> GeneratePayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd, int? createdBy = null);
    
    /// <summary>Aprueba una nómina generada</summary>
    Task ApprovePayrollAsync(int payrollId, int approvedBy);
    
    /// <summary>Marca nómina como pagada</summary>
    Task PayPayrollAsync(int payrollId, DateTime? paymentDate, string paymentMethod, string? paymentRef, int paidBy);
    
    /// <summary>Obtiene nóminas de un empleado</summary>
    Task<List<PayrollDto>> GetPayrollByEmployeeAsync(int employeeId, int? year = null);
    
    // ========================================================================
    // COMISIONES
    // ========================================================================
    
    /// <summary>Registra comisión por venta realizada</summary>
    Task<int> RegisterCommissionFromSaleAsync(int employeeId, int saleId, decimal commissionRate);
    
    /// <summary>Registra comisión por cobro realizado</summary>
    Task<int> RegisterCommissionFromPaymentAsync(int employeeId, int paymentId, decimal commissionRate);
    
    /// <summary>Obtiene comisiones pendientes de procesar</summary>
    Task<List<CommissionDto>> GetCommissionsPendingAsync(int? employeeId = null);
    
    // ========================================================================
    // DOCUMENTOS
    // ========================================================================
    
    /// <summary>Sube un documento de empleado</summary>
    Task<int> UploadEmployeeDocumentAsync(UploadEmployeeDocumentDto dto, int? uploadedBy = null);
    
    /// <summary>Obtiene documentos de un empleado</summary>
    Task<List<EmployeeDocumentDto>> GetEmployeeDocumentsAsync(int employeeId, int? docTypeId = null);
    
    /// <summary>Obtiene un documento específico por ID</summary>
    Task<EmployeeDocumentDto?> GetDocumentByIdAsync(int documentId);
    
    /// <summary>Elimina un documento</summary>
    Task DeleteEmployeeDocumentAsync(int documentId, int? deletedBy = null);
    
    /// <summary>Obtiene tipos de documentos disponibles</summary>
    Task<List<DocumentTypeDto>> GetDocumentTypesAsync();
}
