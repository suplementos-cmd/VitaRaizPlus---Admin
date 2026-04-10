namespace VitaRaiz.Application.DTOs;

/// <summary>
/// DTO de empleado con datos completos
/// </summary>
public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    
    // Datos del usuario
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? RoleName { get; set; }
    public string? ZoneName { get; set; }
    
    // Datos laborales
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionRate { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string EmploymentStatus { get; set; } = "ACTIVO";
    
    // Antigüedad
    public int YearsOfService { get; set; }
    public int MonthsOfService { get; set; }
    
    // Datos bancarios
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? Clabe { get; set; }
    
    // Contactos de emergencia
    public string? EmergencyContact1 { get; set; }
    public string? EmergencyPhone1 { get; set; }
    public string? EmergencyRelation1 { get; set; }
    public string? EmergencyContact2 { get; set; }
    public string? EmergencyPhone2 { get; set; }
    public string? EmergencyRelation2 { get; set; }
    
    // Beneficiario
    public string? BeneficiaryName { get; set; }
    public string? BeneficiaryRelation { get; set; }
    
    // Ubicación
    public string? Address { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    
    // Notas
    public string? Notes { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para crear/actualizar empleado
/// </summary>
public class CreateEmployeeDto
{
    public int UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionRate { get; set; }
    public DateTime HireDate { get; set; } = DateTime.Now;
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? Clabe { get; set; }
    public string? EmergencyContact1 { get; set; }
    public string? EmergencyPhone1 { get; set; }
    public string? EmergencyRelation1 { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// DTO para actualizar empleado
/// </summary>
public class UpdateEmployeeDto
{
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? CommissionRate { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
}

/// <summary>
/// DTO de asistencia
/// </summary>
public class AttendanceDto
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string AttendanceType { get; set; } = string.Empty;
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? WorkedHours { get; set; }
    public string? Notes { get; set; }
    public string? Justification { get; set; }
}

/// <summary>
/// DTO para registrar asistencia
/// </summary>
public class CreateAttendanceDto
{
    public int EmployeeId { get; set; }
    public DateTime AttendanceDate { get; set; } = DateTime.Now;
    public string AttendanceType { get; set; } = "ASISTENCIA";
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO de resumen de asistencias
/// </summary>
public class AttendanceSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Asistencias { get; set; }
    public int Retardos { get; set; }
    public int Faltas { get; set; }
    public int FaltasJustificadas { get; set; }
    public decimal TotalHoras { get; set; }
}

/// <summary>
/// DTO de nómina
/// </summary>
public class PayrollDto
{
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? PaymentDate { get; set; }
    
    public decimal BaseSalary { get; set; }
    public decimal Commissions { get; set; }
    public decimal Bonuses { get; set; }
    public decimal Deductions { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal NetTotal { get; set; }
    
    public int DaysWorked { get; set; }
    public int AbsencesCount { get; set; }
    public int LateCount { get; set; }
    
    public int SalesCount { get; set; }
    public decimal SalesAmount { get; set; }
    public int CollectionsCount { get; set; }
    public decimal CollectionsAmount { get; set; }
    
    public string Status { get; set; } = "PENDIENTE";
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
}

/// <summary>
/// DTO para generar nómina
/// </summary>
public class GeneratePayrollDto
{
    public int EmployeeId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// DTO de comisión
/// </summary>
public class CommissionDto
{
    public int CommissionId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string CommissionType { get; set; } = string.Empty;
    public int ReferenceId { get; set; }
    public DateTime ReferenceDate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = "PENDIENTE";
}

/// <summary>
/// DTO de documento de empleado
/// </summary>
public class EmployeeDocumentDto
{
    public int DocumentId { get; set; }
    public int EmployeeId { get; set; }
    public int DocTypeId { get; set; }
    public string DocTypeCode { get; set; } = string.Empty;
    public string DocTypeName { get; set; } = string.Empty;
    public string? DocTypeDescription { get; set; }
    public bool IsRequired { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = "application/pdf";
    public DateTime UploadDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Status { get; set; } = "VIGENTE"; // VIGENTE, POR_VENCER, VENCIDO
    public string? Notes { get; set; }
    public int? UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime? UploadTimestamp { get; set; }
}

/// <summary>
/// DTO para subir documento
/// </summary>
public class UploadEmployeeDocumentDto
{
    public int EmployeeId { get; set; }
    public int DocTypeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = "application/pdf";
    public DateTime? ExpirationDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO de tipo de documento
/// </summary>
public class DocumentTypeDto
{
    public int DocTypeId { get; set; }
    public string DocTypeCode { get; set; } = string.Empty;
    public string DocTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

