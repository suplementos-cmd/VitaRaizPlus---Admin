namespace VitaRaiz.Domain.Entities;

/// <summary>
/// Entidad de empleado (extensión de User con datos laborales)
/// </summary>
public class Employee
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    
    // Datos laborales
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal CommissionRate { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string EmploymentStatus { get; set; } = "ACTIVO";
    
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
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navegación
    public User? User { get; set; }
}

/// <summary>
/// Registro de asistencia diaria
/// </summary>
public class EmployeeAttendance
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime AttendanceDate { get; set; }
    public string AttendanceType { get; set; } = string.Empty; // ASISTENCIA, FALTA, RETARDO, etc
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal? WorkedHours { get; set; }
    public string? Notes { get; set; }
    public string? Justification { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
    public string? DeviceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    
    // Navegación
    public Employee? Employee { get; set; }
}

/// <summary>
/// Nómina/Pago a empleado por periodo
/// </summary>
public class EmployeePayroll
{
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    
    // Periodo
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? PaymentDate { get; set; }
    
    // Conceptos
    public decimal BaseSalary { get; set; }
    public decimal Commissions { get; set; }
    public decimal Bonuses { get; set; }
    public decimal Overtime { get; set; }
    
    // Deducciones
    public decimal Deductions { get; set; }
    public decimal Absences { get; set; }
    
    // Totales
    public decimal GrossTotal { get; set; }
    public decimal NetTotal { get; set; }
    
    // Stats del periodo
    public int DaysWorked { get; set; }
    public int AbsencesCount { get; set; }
    public int LateCount { get; set; }
    
    // Ventas/Cobros
    public int SalesCount { get; set; }
    public decimal SalesAmount { get; set; }
    public int CollectionsCount { get; set; }
    public decimal CollectionsAmount { get; set; }
    
    public string Status { get; set; } = "PENDIENTE"; // PENDIENTE, APROBADA, PAGADA, CANCELADA
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    
    public string? Notes { get; set; }
    
    // Auditoría
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    
    // Navegación
    public Employee? Employee { get; set; }
}

/// <summary>
/// Detalle de comisiones por ventas o cobros
/// </summary>
public class EmployeeCommission
{
    public int CommissionId { get; set; }
    public int EmployeeId { get; set; }
    public int? PayrollId { get; set; }
    
    public string CommissionType { get; set; } = string.Empty; // VENTA, COBRO
    public int ReferenceId { get; set; } // SALE_ID o PAYMENT_ID
    public DateTime ReferenceDate { get; set; }
    
    public decimal BaseAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    
    public string Status { get; set; } = "PENDIENTE"; // PENDIENTE, PROCESADA, PAGADA, CANCELADA
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // Navegación
    public Employee? Employee { get; set; }
    public EmployeePayroll? Payroll { get; set; }
}

/// <summary>
/// Documento digitalizado del empleado
/// </summary>
public class EmployeeDocument
{
    public int DocumentId { get; set; }
    public int EmployeeId { get; set; }
    public int DocTypeId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public DateTime UploadDate { get; set; }
    public int? UploadedBy { get; set; }
    public string? Notes { get; set; }
    
    // Navegación
    public Employee? Employee { get; set; }
}
