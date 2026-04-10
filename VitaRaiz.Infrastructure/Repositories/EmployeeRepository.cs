using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using VitaRaiz.Application.DTOs;
using VitaRaiz.Application.Interfaces;
using VitaRaiz.Infrastructure.Data;

namespace VitaRaiz.Infrastructure.Repositories;

public class EmployeeRepository : BaseOracleRepository, IEmployeeRepository
{
    public EmployeeRepository(VitaRaizDbContext context) : base(context)
    {
    }

    // ========================================================================
    // GESTIÓN DE EMPLEADOS
    // ========================================================================

    public async Task<int> RegisterEmployeeAsync(CreateEmployeeDto dto, int? createdBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_employee");

        var employeeIdParam = new OracleParameter("p_employee_id", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(employeeIdParam);
        command.Parameters.Add(new OracleParameter("p_user_id", dto.UserId));
        command.Parameters.Add(new OracleParameter("p_employee_code", dto.EmployeeCode));
        command.Parameters.Add(new OracleParameter("p_job_title", dto.JobTitle ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_department", dto.Department ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_base_salary", dto.BaseSalary));
        command.Parameters.Add(new OracleParameter("p_commission_rate", dto.CommissionRate));
        command.Parameters.Add(new OracleParameter("p_hire_date", dto.HireDate));
        command.Parameters.Add(new OracleParameter("p_bank_name", dto.BankName ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_bank_account", dto.BankAccount ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_created_by", createdBy ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(employeeIdParam.Value.ToString());
    }

    public async Task UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto dto, int? updatedBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_update_employee");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_job_title", dto.JobTitle ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_department", dto.Department ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_base_salary", dto.BaseSalary ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_commission_rate", dto.CommissionRate ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_employment_status", dto.EmploymentStatus ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_bank_name", dto.BankName ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_bank_account", dto.BankAccount ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_updated_by", updatedBy ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();
    }

    public async Task TerminateEmployeeAsync(int employeeId, DateTime? terminationDate = null, string? reason = null, int? updatedBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_terminate_employee");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_termination_date", terminationDate ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_reason", reason ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_updated_by", updatedBy ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync(string? status = null, string? department = null, string? searchTerm = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_employees");

        command.Parameters.Add(new OracleParameter("p_status", status ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_department", department ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_search_term", searchTerm ?? (object)DBNull.Value));

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var employees = new List<EmployeeDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            employees.Add(new EmployeeDto
            {
                EmployeeId = reader.GetInt32("employeeId"),
                UserId = reader.GetInt32("userId"),
                EmployeeCode = reader.GetString("employeeCode"),
                Username = reader.GetString("username"),
                FullName = reader.GetString("fullName"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                RoleName = reader.IsDBNull("roleName") ? null : reader.GetString("roleName"),
                ZoneName = reader.IsDBNull("zoneName") ? null : reader.GetString("zoneName"),
                JobTitle = reader.IsDBNull("jobTitle") ? null : reader.GetString("jobTitle"),
                Department = reader.IsDBNull("department") ? null : reader.GetString("department"),
                BaseSalary = reader.GetDecimal("baseSalary"),
                CommissionRate = reader.GetDecimal("commissionRate"),
                HireDate = reader.GetDateTime("hireDate"),
                TerminationDate = reader.IsDBNull("terminationDate") ? null : reader.GetDateTime("terminationDate"),
                EmploymentStatus = reader.GetString("employmentStatus"),
                YearsOfService = reader.GetInt32("yearsOfService"),
                MonthsOfService = reader.GetInt32("monthsOfService"),
                BankName = reader.IsDBNull("bankName") ? null : reader.GetString("bankName"),
                BankAccount = reader.IsDBNull("bankAccount") ? null : reader.GetString("bankAccount"),
                EmergencyContact1 = reader.IsDBNull("emergencyContact1") ? null : reader.GetString("emergencyContact1"),
                EmergencyPhone1 = reader.IsDBNull("emergencyPhone1") ? null : reader.GetString("emergencyPhone1")
            });
        }

        return employees;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_employee_by_id");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        if (await reader.ReadAsync())
        {
            return new EmployeeDto
            {
                EmployeeId = reader.GetInt32(0),
                EmployeeCode = reader.GetString(1),
                UserId = reader.GetInt32(2),
                Username = reader.GetString(3),
                FullName = reader.GetString(4),
                Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                RoleName = reader.IsDBNull(7) ? null : reader.GetString(7),
                ZoneName = reader.IsDBNull(8) ? null : reader.GetString(8),
                JobTitle = reader.IsDBNull(9) ? null : reader.GetString(9),
                Department = reader.IsDBNull(10) ? null : reader.GetString(10),
                BaseSalary = reader.GetDecimal(11),
                CommissionRate = reader.GetDecimal(12),
                HireDate = reader.GetDateTime(13),
                TerminationDate = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
                EmploymentStatus = reader.GetString(15),
                YearsOfService = reader.GetInt32(16),
                BankName = reader.IsDBNull(17) ? null : reader.GetString(17),
                BankAccount = reader.IsDBNull(18) ? null : reader.GetString(18),
                Clabe = reader.IsDBNull(19) ? null : reader.GetString(19),
                EmergencyContact1 = reader.IsDBNull(20) ? null : reader.GetString(20),
                EmergencyPhone1 = reader.IsDBNull(21) ? null : reader.GetString(21),
                EmergencyRelation1 = reader.IsDBNull(22) ? null : reader.GetString(22),
                EmergencyContact2 = reader.IsDBNull(23) ? null : reader.GetString(23),
                EmergencyPhone2 = reader.IsDBNull(24) ? null : reader.GetString(24),
                EmergencyRelation2 = reader.IsDBNull(25) ? null : reader.GetString(25),
                BeneficiaryName = reader.IsDBNull(26) ? null : reader.GetString(26),
                BeneficiaryRelation = reader.IsDBNull(27) ? null : reader.GetString(27),
                Address = reader.IsDBNull(28) ? null : reader.GetString(28),
                GpsLatitude = reader.IsDBNull(29) ? null : reader.GetDecimal(29),
                GpsLongitude = reader.IsDBNull(30) ? null : reader.GetDecimal(30),
                Notes = reader.IsDBNull(31) ? null : reader.GetString(31)
            };
        }

        return null;
    }

    // ========================================================================
    // CONTROL DE ASISTENCIAS
    // ========================================================================

    public async Task<int> RegisterAttendanceAsync(CreateAttendanceDto dto)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_attendance");

        var attendanceIdParam = new OracleParameter("p_attendance_id", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(attendanceIdParam);
        command.Parameters.Add(new OracleParameter("p_employee_id", dto.EmployeeId));
        command.Parameters.Add(new OracleParameter("p_attendance_date", dto.AttendanceDate));
        command.Parameters.Add(new OracleParameter("p_attendance_type", dto.AttendanceType));
        command.Parameters.Add(new OracleParameter("p_check_in_time", dto.CheckInTime ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_check_out_time", dto.CheckOutTime ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lat", dto.GpsLatitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_gps_lon", dto.GpsLongitude ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_notes", dto.Notes ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(attendanceIdParam.Value.ToString());
    }

    public async Task<List<AttendanceDto>> GetAttendanceByEmployeeAsync(int employeeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_attendance_by_employee");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_start_date", startDate ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_end_date", endDate ?? (object)DBNull.Value));

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var attendances = new List<AttendanceDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            attendances.Add(new AttendanceDto
            {
                AttendanceId = reader.GetInt32("attendanceId"),
                EmployeeId = reader.GetInt32("employeeId"),
                AttendanceDate = reader.GetDateTime("attendanceDate"),
                AttendanceType = reader.GetString("attendanceType"),
                CheckInTime = reader.IsDBNull("checkInTime") ? null : reader.GetDateTime("checkInTime"),
                CheckOutTime = reader.IsDBNull("checkOutTime") ? null : reader.GetDateTime("checkOutTime"),
                WorkedHours = reader.IsDBNull("workedHours") ? null : reader.GetDecimal("workedHours"),
                Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
            });
        }

        return attendances;
    }

    public async Task<AttendanceSummaryDto?> GetAttendanceSummaryAsync(int employeeId, int? year = null, int? month = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_attendance_summary");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_year", year ?? DateTime.Now.Year));
        command.Parameters.Add(new OracleParameter("p_month", month ?? DateTime.Now.Month));

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        if (await reader.ReadAsync())
        {
            return new AttendanceSummaryDto
            {
                Asistencias = reader.GetInt32("asistencias"),
                Retardos = reader.GetInt32("retardos"),
                Faltas = reader.GetInt32("faltas"),
                FaltasJustificadas = reader.GetInt32("faltasJustificadas"),
                TotalHoras = reader.GetDecimal("totalHoras")
            };
        }

        return null;
    }

    // ========================================================================
    // NÓMINA
    // ========================================================================

    public async Task<int> GeneratePayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd, int? createdBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_generate_payroll");

        var payrollIdParam = new OracleParameter("p_payroll_id", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(payrollIdParam);
        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_period_start", periodStart));
        command.Parameters.Add(new OracleParameter("p_period_end", periodEnd));
        command.Parameters.Add(new OracleParameter("p_created_by", createdBy ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(payrollIdParam.Value.ToString());
    }

    public async Task ApprovePayrollAsync(int payrollId, int approvedBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_approve_payroll");

        command.Parameters.Add(new OracleParameter("p_payroll_id", payrollId));
        command.Parameters.Add(new OracleParameter("p_approved_by", approvedBy));

        await command.ExecuteNonQueryAsync();
    }

    public async Task PayPayrollAsync(int payrollId, DateTime? paymentDate, string paymentMethod, string? paymentRef, int paidBy)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_pay_payroll");

        command.Parameters.Add(new OracleParameter("p_payroll_id", payrollId));
        command.Parameters.Add(new OracleParameter("p_payment_date", paymentDate ?? DateTime.Now));
        command.Parameters.Add(new OracleParameter("p_payment_method", paymentMethod));
        command.Parameters.Add(new OracleParameter("p_payment_ref", paymentRef ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_paid_by", paidBy));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<PayrollDto>> GetPayrollByEmployeeAsync(int employeeId, int? year = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_payroll_by_employee");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_year", year ?? (object)DBNull.Value));

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var payrolls = new List<PayrollDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            payrolls.Add(new PayrollDto
            {
                PayrollId = reader.GetInt32("payrollId"),
                PeriodStart = reader.GetDateTime("periodStart"),
                PeriodEnd = reader.GetDateTime("periodEnd"),
                PaymentDate = reader.IsDBNull("paymentDate") ? null : reader.GetDateTime("paymentDate"),
                BaseSalary = reader.GetDecimal("baseSalary"),
                Commissions = reader.GetDecimal("commissions"),
                GrossTotal = reader.GetDecimal("grossTotal"),
                NetTotal = reader.GetDecimal("netTotal"),
                DaysWorked = reader.GetInt32("daysWorked"),
                AbsencesCount = reader.GetInt32("absencesCount"),
                LateCount = reader.GetInt32("lateCount"),
                Status = reader.GetString("status")
            });
        }

        return payrolls;
    }

    // ========================================================================
    // COMISIONES
    // ========================================================================

    public async Task<int> RegisterCommissionFromSaleAsync(int employeeId, int saleId, decimal commissionRate)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_commission_from_sale");

        var commissionIdParam = new OracleParameter("p_commission_id", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(commissionIdParam);
        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_sale_id", saleId));
        command.Parameters.Add(new OracleParameter("p_commission_rate", commissionRate));

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(commissionIdParam.Value.ToString());
    }

    public async Task<int> RegisterCommissionFromPaymentAsync(int employeeId, int paymentId, decimal commissionRate)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_register_commission_from_payment");

        var commissionIdParam = new OracleParameter("p_commission_id", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(commissionIdParam);
        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_payment_id", paymentId));
        command.Parameters.Add(new OracleParameter("p_commission_rate", commissionRate));

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(commissionIdParam.Value.ToString());
    }

    public async Task<List<CommissionDto>> GetCommissionsPendingAsync(int? employeeId = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_commissions_pending");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId ?? (object)DBNull.Value));

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var commissions = new List<CommissionDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            commissions.Add(new CommissionDto
            {
                CommissionId = reader.GetInt32("commissionId"),
                EmployeeId = reader.GetInt32("employeeId"),
                CommissionType = reader.GetString("commissionType"),
                ReferenceId = reader.GetInt32("referenceId"),
                ReferenceDate = reader.GetDateTime("referenceDate"),
                BaseAmount = reader.GetDecimal("baseAmount"),
                CommissionRate = reader.GetDecimal("commissionRate"),
                CommissionAmount = reader.GetDecimal("commissionAmount"),
                Status = reader.GetString("status")
            });
        }

        return commissions;
    }

    // ========================================================================
    // DOCUMENTOS
    // ========================================================================

    public async Task<int> UploadEmployeeDocumentAsync(UploadEmployeeDocumentDto dto, int? uploadedBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_upload_employee_document");

        var documentIdParam = new OracleParameter("p_document_id", OracleDbType.Int32) { Direction = ParameterDirection.Output };
        command.Parameters.Add(documentIdParam);
        command.Parameters.Add(new OracleParameter("p_employee_id", dto.EmployeeId));
        command.Parameters.Add(new OracleParameter("p_doc_type_id", dto.DocTypeId));
        command.Parameters.Add(new OracleParameter("p_file_name", dto.FileName));
        command.Parameters.Add(new OracleParameter("p_file_path", dto.FilePath));
        command.Parameters.Add(new OracleParameter("p_file_size", dto.FileSize));
        command.Parameters.Add(new OracleParameter("p_mime_type", dto.MimeType));
        command.Parameters.Add(new OracleParameter("p_expiration_date", dto.ExpirationDate ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_notes", dto.Notes ?? (object)DBNull.Value));
        command.Parameters.Add(new OracleParameter("p_uploaded_by", uploadedBy ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(((OracleDecimal)documentIdParam.Value).Value);
    }

    public async Task<List<EmployeeDocumentDto>> GetEmployeeDocumentsAsync(int employeeId, int? docTypeId = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_employee_documents");

        command.Parameters.Add(new OracleParameter("p_employee_id", employeeId));
        command.Parameters.Add(new OracleParameter("p_doc_type_id", docTypeId ?? (object)DBNull.Value));
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var documents = new List<EmployeeDocumentDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            documents.Add(new EmployeeDocumentDto
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("document_id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                DocTypeId = reader.GetInt32(reader.GetOrdinal("doc_type_id")),
                DocTypeCode = reader.GetString(reader.GetOrdinal("doc_type_code")),
                DocTypeName = reader.GetString(reader.GetOrdinal("doc_type_name")),
                DocTypeDescription = reader.IsDBNull(reader.GetOrdinal("doc_type_description")) ? null : reader.GetString(reader.GetOrdinal("doc_type_description")),
                IsRequired = reader.GetString(reader.GetOrdinal("is_required")) == "Y",
                FileName = reader.GetString(reader.GetOrdinal("file_name")),
                FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
                MimeType = reader.GetString(reader.GetOrdinal("mime_type")),
                UploadDate = reader.GetDateTime(reader.GetOrdinal("upload_date")),
                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? null : reader.GetDateTime(reader.GetOrdinal("expiration_date")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploaded_by")) ? null : reader.GetInt32(reader.GetOrdinal("uploaded_by")),
                UploadedByName = reader.IsDBNull(reader.GetOrdinal("uploaded_by_name")) ? null : reader.GetString(reader.GetOrdinal("uploaded_by_name")),
                UploadTimestamp = reader.IsDBNull(reader.GetOrdinal("upload_timestamp")) ? null : reader.GetDateTime(reader.GetOrdinal("upload_timestamp"))
            });
        }

        return documents;
    }

    public async Task<EmployeeDocumentDto?> GetDocumentByIdAsync(int documentId)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_document_by_id");

        command.Parameters.Add(new OracleParameter("p_document_id", documentId));
        
        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        if (await reader.ReadAsync())
        {
            return new EmployeeDocumentDto
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("document_id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
                DocTypeId = reader.GetInt32(reader.GetOrdinal("doc_type_id")),
                DocTypeCode = reader.GetString(reader.GetOrdinal("doc_type_code")),
                DocTypeName = reader.GetString(reader.GetOrdinal("doc_type_name")),
                FileName = reader.GetString(reader.GetOrdinal("file_name")),
                FilePath = reader.GetString(reader.GetOrdinal("file_path")),
                FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
                MimeType = reader.GetString(reader.GetOrdinal("mime_type")),
                UploadDate = reader.GetDateTime(reader.GetOrdinal("upload_date")),
                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? null : reader.GetDateTime(reader.GetOrdinal("expiration_date")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploaded_by")) ? null : reader.GetInt32(reader.GetOrdinal("uploaded_by")),
                UploadedByName = reader.IsDBNull(reader.GetOrdinal("uploaded_by_name")) ? null : reader.GetString(reader.GetOrdinal("uploaded_by_name")),
                UploadTimestamp = reader.IsDBNull(reader.GetOrdinal("upload_timestamp")) ? null : reader.GetDateTime(reader.GetOrdinal("upload_timestamp"))
            };
        }

        return null;
    }

    public async Task DeleteEmployeeDocumentAsync(int documentId, int? deletedBy = null)
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_delete_employee_document");

        command.Parameters.Add(new OracleParameter("p_document_id", documentId));
        command.Parameters.Add(new OracleParameter("p_deleted_by", deletedBy ?? (object)DBNull.Value));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<DocumentTypeDto>> GetDocumentTypesAsync()
    {
        var connection = await GetOpenConnectionAsync();
        using var command = CreatePackageProcedureCommand(connection, "sp_get_document_types");

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        var types = new List<DocumentTypeDto>();
        using var reader = ((OracleRefCursor)cursorParam.Value).GetDataReader();

        while (await reader.ReadAsync())
        {
            types.Add(new DocumentTypeDto
            {
                DocTypeId = reader.GetInt32(reader.GetOrdinal("doc_type_id")),
                DocTypeCode = reader.GetString(reader.GetOrdinal("doc_type_code")),
                DocTypeName = reader.GetString(reader.GetOrdinal("doc_type_name")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                IsRequired = reader.GetString(reader.GetOrdinal("is_required")) == "Y",
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("display_order"))
            });
        }

        return types;
    }
}
