-- ===========================================================================
-- VITARAIZ - PROCEDIMIENTOS DEL MÓDULO HR PARA AGREGAR AL PACKAGE
-- Agregar estos procedimientos al package EM_VITARAIZ_AD
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- ESPECIFICACIÓN (AGREGAR AL HEADER DEL PACKAGE)
-- ---------------------------------------------------------------------------

  /**
  * ============================================
  * MÓDULO DE RECURSOS HUMANOS (HR)
  * ============================================
  */

  -- GESTIÓN DE EMPLEADOS
  
  PROCEDURE sp_register_employee(
    p_employee_id       OUT NUMBER,
    p_user_id           IN NUMBER,
    p_employee_code     IN VARCHAR2,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT 0,
    p_commission_rate   IN NUMBER DEFAULT 0,
    p_hire_date         IN DATE DEFAULT SYSDATE,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_created_by        IN NUMBER DEFAULT NULL
  );

  PROCEDURE sp_update_employee(
    p_employee_id       IN NUMBER,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT NULL,
    p_commission_rate   IN NUMBER DEFAULT NULL,
    p_employment_status IN VARCHAR2 DEFAULT NULL,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  );

  PROCEDURE sp_terminate_employee(
    p_employee_id       IN NUMBER,
    p_termination_date  IN DATE DEFAULT SYSDATE,
    p_reason            IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  );

  PROCEDURE sp_get_employees(
    p_status           IN VARCHAR2 DEFAULT NULL,
    p_department       IN VARCHAR2 DEFAULT NULL,
    p_search_term      IN VARCHAR2 DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  PROCEDURE sp_get_employee_by_id(
    p_employee_id      IN NUMBER,
    p_cursor           OUT SYS_REFCURSOR
  );

  -- CONTROL DE ASISTENCIAS
  
  PROCEDURE sp_register_attendance(
    p_attendance_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_attendance_date  IN DATE DEFAULT SYSDATE,
    p_attendance_type  IN VARCHAR2,
    p_check_in_time    IN TIMESTAMP DEFAULT SYSTIMESTAMP,
    p_check_out_time   IN TIMESTAMP DEFAULT NULL,
    p_gps_lat          IN NUMBER DEFAULT NULL,
    p_gps_lon          IN NUMBER DEFAULT NULL,
    p_notes            IN VARCHAR2 DEFAULT NULL
  );

  PROCEDURE sp_get_attendance_by_employee(
    p_employee_id      IN NUMBER,
    p_start_date       IN DATE DEFAULT NULL,
    p_end_date         IN DATE DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  PROCEDURE sp_get_attendance_summary(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT EXTRACT(YEAR FROM SYSDATE),
    p_month            IN NUMBER DEFAULT EXTRACT(MONTH FROM SYSDATE),
    p_cursor           OUT SYS_REFCURSOR
  );

  -- NÓMINA Y PAGOS
  
  PROCEDURE sp_generate_payroll(
    p_payroll_id       OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE,
    p_created_by       IN NUMBER DEFAULT NULL
  );

  PROCEDURE sp_approve_payroll(
    p_payroll_id       IN NUMBER,
    p_approved_by      IN NUMBER
  );

  PROCEDURE sp_pay_payroll(
    p_payroll_id       IN NUMBER,
    p_payment_date     IN DATE DEFAULT SYSDATE,
    p_payment_method   IN VARCHAR2,
    p_payment_ref      IN VARCHAR2 DEFAULT NULL,
    p_paid_by          IN NUMBER
  );

  PROCEDURE sp_get_payroll_by_employee(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  -- COMISIONES
  
  PROCEDURE sp_register_commission_from_sale(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_sale_id          IN NUMBER,
    p_commission_rate  IN NUMBER
  );

  PROCEDURE sp_register_commission_from_payment(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_payment_id       IN NUMBER,
    p_commission_rate  IN NUMBER
  );

  PROCEDURE sp_get_commissions_pending(
    p_employee_id      IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  -- FUNCIONES AUXILIARES
  
  FUNCTION fn_get_employee_seniority_years(p_employee_id IN NUMBER) RETURN NUMBER;
  
  FUNCTION fn_get_employee_total_commissions(
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE
  ) RETURN NUMBER;

  FUNCTION fn_calculate_worked_hours(
    p_check_in         IN TIMESTAMP,
    p_check_out        IN TIMESTAMP
  ) RETURN NUMBER;

-- ---------------------------------------------------------------------------
-- IMPLEMENTACIÓN (AGREGAR AL BODY DEL PACKAGE)
-- ---------------------------------------------------------------------------

  -- ========================================================================
  -- GESTIÓN DE EMPLEADOS
  -- ========================================================================

  PROCEDURE sp_register_employee(
    p_employee_id       OUT NUMBER,
    p_user_id           IN NUMBER,
    p_employee_code     IN VARCHAR2,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT 0,
    p_commission_rate   IN NUMBER DEFAULT 0,
    p_hire_date         IN DATE DEFAULT SYSDATE,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_created_by        IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    INSERT INTO employees (
      employee_id, user_id, employee_code, job_title, department,
      base_salary, commission_rate, hire_date, employment_status,
      bank_name, bank_account, created_by, created_at
    ) VALUES (
      seq_employees.NEXTVAL, p_user_id, p_employee_code, p_job_title, p_department,
      NVL(p_base_salary, 0), NVL(p_commission_rate, 0), NVL(p_hire_date, SYSDATE), 'ACTIVO',
      p_bank_name, p_bank_account, p_created_by, SYSTIMESTAMP
    ) RETURNING employee_id INTO p_employee_id;
    
    log_audit('EMPLOYEE', p_employee_id, 'INSERT', p_created_by, 
              'Empleado registrado: ' || p_employee_code);
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_employee;

  PROCEDURE sp_update_employee(
    p_employee_id       IN NUMBER,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT NULL,
    p_commission_rate   IN NUMBER DEFAULT NULL,
    p_employment_status IN VARCHAR2 DEFAULT NULL,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    UPDATE employees
    SET job_title         = COALESCE(p_job_title, job_title),
        department        = COALESCE(p_department, department),
        base_salary       = COALESCE(p_base_salary, base_salary),
        commission_rate   = COALESCE(p_commission_rate, commission_rate),
        employment_status = COALESCE(p_employment_status, employment_status),
        bank_name         = COALESCE(p_bank_name, bank_name),
        bank_account      = COALESCE(p_bank_account, bank_account),
        updated_by        = p_updated_by,
        updated_at        = SYSTIMESTAMP
    WHERE employee_id = p_employee_id;
    
    log_audit('EMPLOYEE', p_employee_id, 'UPDATE', p_updated_by, 'Empleado actualizado');
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_employee;

  PROCEDURE sp_terminate_employee(
    p_employee_id       IN NUMBER,
    p_termination_date  IN DATE DEFAULT SYSDATE,
    p_reason            IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    UPDATE employees
    SET employment_status = 'BAJA',
        termination_date  = NVL(p_termination_date, SYSDATE),
        notes             = COALESCE(notes, '') || CHR(10) || 
                           'BAJA: ' || TO_CHAR(SYSDATE, 'DD/MM/YYYY') || 
                           CASE WHEN p_reason IS NOT NULL THEN ' - ' || p_reason ELSE '' END,
        updated_by        = p_updated_by,
        updated_at        = SYSTIMESTAMP
    WHERE employee_id = p_employee_id;
    
    -- También desactivar el usuario
    UPDATE users SET is_active = '0' 
    WHERE user_id = (SELECT user_id FROM employees WHERE employee_id = p_employee_id);
    
    log_audit('EMPLOYEE', p_employee_id, 'TERMINATE', p_updated_by, p_reason);
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_terminate_employee;

  PROCEDURE sp_get_employees(
    p_status           IN VARCHAR2 DEFAULT NULL,
    p_department       IN VARCHAR2 DEFAULT NULL,
    p_search_term      IN VARCHAR2 DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        e.employee_id        AS "employeeId",
        e.employee_code      AS "employeeCode",
        e.user_id            AS "userId",
        u.username           AS "username",
        u.full_name          AS "fullName",
        u.email              AS "email",
        u.phone              AS "phone",
        r.role_name          AS "roleName",
        z.zone_name          AS "zoneName",
        e.job_title          AS "jobTitle",
        e.department         AS "department",
        e.base_salary        AS "baseSalary",
        e.commission_rate    AS "commissionRate",
        e.hire_date          AS "hireDate",
        e.termination_date   AS "terminationDate",
        e.employment_status  AS "employmentStatus",
        TRUNC(MONTHS_BETWEEN(SYSDATE, e.hire_date) / 12) AS "yearsOfService",
        MOD(TRUNC(MONTHS_BETWEEN(SYSDATE, e.hire_date)), 12) AS "monthsOfService",
        e.bank_name          AS "bankName",
        e.bank_account       AS "bankAccount",
        e.emergency_contact_1 AS "emergencyContact1",
        e.emergency_phone_1   AS "emergencyPhone1"
      FROM employees e
      JOIN users u ON e.user_id = u.user_id
      JOIN roles r ON u.role_id = r.role_id
      LEFT JOIN zones z ON u.zone_id = z.zone_id
      WHERE (p_status IS NULL OR e.employment_status = p_status)
        AND (p_department IS NULL OR UPPER(e.department) = UPPER(p_department))
        AND (p_search_term IS NULL OR 
             UPPER(u.full_name) LIKE '%' || UPPER(p_search_term) || '%' OR
             UPPER(e.employee_code) LIKE '%' || UPPER(p_search_term) || '%')
      ORDER BY u.full_name;
  END sp_get_employees;

  PROCEDURE sp_get_employee_by_id(
    p_employee_id      IN NUMBER,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        e.employee_id, e.employee_code, e.user_id, u.username, u.full_name,
        u.email, u.phone, r.role_name, z.zone_name,
        e.job_title, e.department, e.base_salary, e.commission_rate,
        e.hire_date, e.termination_date, e.employment_status,
        TRUNC(MONTHS_BETWEEN(SYSDATE, e.hire_date) / 12) AS years_of_service,
        e.bank_name, e.bank_account, e.clabe,
        e.emergency_contact_1, e.emergency_phone_1, e.emergency_relation_1,
        e.emergency_contact_2, e.emergency_phone_2, e.emergency_relation_2,
        e.beneficiary_name, e.beneficiary_relation,
        e.address, e.gps_latitude, e.gps_longitude, e.notes
      FROM employees e
      JOIN users u ON e.user_id = u.user_id
      JOIN roles r ON u.role_id = r.role_id
      LEFT JOIN zones z ON u.zone_id = z.zone_id
      WHERE e.employee_id = p_employee_id;
  END sp_get_employee_by_id;

  -- ========================================================================
  -- ASISTENCIAS
  -- ========================================================================

  PROCEDURE sp_register_attendance(
    p_attendance_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_attendance_date  IN DATE DEFAULT SYSDATE,
    p_attendance_type  IN VARCHAR2,
    p_check_in_time    IN TIMESTAMP DEFAULT SYSTIMESTAMP,
    p_check_out_time   IN TIMESTAMP DEFAULT NULL,
    p_gps_lat          IN NUMBER DEFAULT NULL,
    p_gps_lon          IN NUMBER DEFAULT NULL,
    p_notes            IN VARCHAR2 DEFAULT NULL
  ) IS
    v_worked_hours NUMBER;
  BEGIN
    -- Calcular horas trabajadas
    v_worked_hours := fn_calculate_worked_hours(p_check_in_time, p_check_out_time);
    
    INSERT INTO employee_attendance (
      attendance_id, employee_id, attendance_date, attendance_type,
      check_in_time, check_out_time, worked_hours,
      gps_latitude, gps_longitude, notes, created_at
    ) VALUES (
      seq_employee_attendance.NEXTVAL, p_employee_id, 
      TRUNC(NVL(p_attendance_date, SYSDATE)), p_attendance_type,
      p_check_in_time, p_check_out_time, v_worked_hours,
      p_gps_lat, p_gps_lon, p_notes, SYSTIMESTAMP
    ) RETURNING attendance_id INTO p_attendance_id;
    
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_attendance;

  PROCEDURE sp_get_attendance_by_employee(
    p_employee_id      IN NUMBER,
    p_start_date       IN DATE DEFAULT NULL,
    p_end_date         IN DATE DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        attendance_id       AS "attendanceId",
        employee_id         AS "employeeId",
        attendance_date     AS "attendanceDate",
        attendance_type     AS "attendanceType",
        check_in_time       AS "checkInTime",
        check_out_time      AS "checkOutTime",
        worked_hours        AS "workedHours",
        notes               AS "notes"
      FROM employee_attendance
      WHERE employee_id = p_employee_id
        AND (p_start_date IS NULL OR attendance_date >= p_start_date)
        AND (p_end_date IS NULL OR attendance_date <= p_end_date)
      ORDER BY attendance_date DESC;
  END sp_get_attendance_by_employee;

  PROCEDURE sp_get_attendance_summary(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT EXTRACT(YEAR FROM SYSDATE),
    p_month            IN NUMBER DEFAULT EXTRACT(MONTH FROM SYSDATE),
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        COUNT(CASE WHEN attendance_type = 'ASISTENCIA' THEN 1 END) AS "asistencias",
        COUNT(CASE WHEN attendance_type = 'RETARDO' THEN 1 END) AS "retardos",
        COUNT(CASE WHEN attendance_type = 'FALTA' THEN 1 END) AS "faltas",
        COUNT(CASE WHEN attendance_type = 'FALTA_JUSTIFICADA' THEN 1 END) AS "faltasJustificadas",
        SUM(NVL(worked_hours, 0)) AS "totalHoras"
      FROM employee_attendance
      WHERE employee_id = p_employee_id
        AND EXTRACT(YEAR FROM attendance_date) = p_year
        AND EXTRACT(MONTH FROM attendance_date) = p_month;
  END sp_get_attendance_summary;

  -- ========================================================================
  -- NÓMINA
  -- ========================================================================

  PROCEDURE sp_generate_payroll(
    p_payroll_id       OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE,
    p_created_by       IN NUMBER DEFAULT NULL
  ) IS
    v_base_salary      NUMBER;
    v_commission_rate  NUMBER;
    v_commissions      NUMBER := 0;
    v_days_worked      NUMBER := 0;
    v_absences_count   NUMBER := 0;
    v_late_count       NUMBER := 0;
    v_sales_count      NUMBER := 0;
    v_sales_amount     NUMBER := 0;
    v_collections_count NUMBER := 0;
    v_collections_amount NUMBER := 0;
    v_gross_total      NUMBER;
    v_net_total        NUMBER;
  BEGIN
    -- Obtener datos del empleado
    SELECT base_salary, commission_rate
    INTO v_base_salary, v_commission_rate
    FROM employees
    WHERE employee_id = p_employee_id;
    
    -- Calcular días trabajados y asistencias
    SELECT 
      COUNT(CASE WHEN attendance_type IN ('ASISTENCIA', 'RETARDO') THEN 1 END),
      COUNT(CASE WHEN attendance_type = 'FALTA' THEN 1 END),
      COUNT(CASE WHEN attendance_type = 'RETARDO' THEN 1 END)
    INTO v_days_worked, v_absences_count, v_late_count
    FROM employee_attendance
    WHERE employee_id = p_employee_id
      AND attendance_date BETWEEN p_period_start AND p_period_end;
    
    -- Calcular comisiones pendientes
    v_commissions := fn_get_employee_total_commissions(p_employee_id, p_period_start, p_period_end);
    
    -- Totales
    v_gross_total := v_base_salary + v_commissions;
    v_net_total   := v_gross_total;  -- Aquí se aplicarían deducciones
    
    -- Insertar nómina
    INSERT INTO employee_payroll (
      payroll_id, employee_id, period_start, period_end,
      base_salary, commissions, gross_total, net_total,
      days_worked, absences_count, late_count,
      sales_count, collections_count,
      status, created_by, created_at
    ) VALUES (
      seq_employee_payroll.NEXTVAL, p_employee_id, p_period_start, p_period_end,
      v_base_salary, v_commissions, v_gross_total, v_net_total,
      v_days_worked, v_absences_count, v_late_count,
      v_sales_count, v_collections_count,
      'PENDIENTE', p_created_by, SYSTIMESTAMP
    ) RETURNING payroll_id INTO p_payroll_id;
    
    -- Marcar comisiones como procesadas
    UPDATE employee_commissions
    SET payroll_id = p_payroll_id,
        status = 'PROCESADA',
        processed_at = SYSTIMESTAMP
    WHERE employee_id = p_employee_id
      AND status = 'PENDIENTE'
      AND reference_date BETWEEN p_period_start AND p_period_end;
    
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_generate_payroll;

  PROCEDURE sp_approve_payroll(
    p_payroll_id       IN NUMBER,
    p_approved_by      IN NUMBER
  ) IS
  BEGIN
    UPDATE employee_payroll
    SET status = 'APROBADA',
        approved_by = p_approved_by,
        approved_at = SYSTIMESTAMP
    WHERE payroll_id = p_payroll_id;
    
    COMMIT;
  END sp_approve_payroll;

  PROCEDURE sp_pay_payroll(
    p_payroll_id       IN NUMBER,
    p_payment_date     IN DATE DEFAULT SYSDATE,
    p_payment_method   IN VARCHAR2,
    p_payment_ref      IN VARCHAR2 DEFAULT NULL,
    p_paid_by          IN NUMBER
  ) IS
  BEGIN
    UPDATE employee_payroll
    SET status = 'PAGADA',
        payment_date = NVL(p_payment_date, SYSDATE),
        payment_method = p_payment_method,
        payment_reference = p_payment_ref,
        paid_by = p_paid_by,
        paid_at = SYSTIMESTAMP
    WHERE payroll_id = p_payroll_id;
    
    -- Marcar comisiones como pagadas
    UPDATE employee_commissions
    SET status = 'PAGADA'
    WHERE payroll_id = p_payroll_id;
    
    COMMIT;
  END sp_pay_payroll;

  PROCEDURE sp_get_payroll_by_employee(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        payroll_id AS "payrollId",
        period_start AS "periodStart",
        period_end AS "periodEnd",
        payment_date AS "paymentDate",
        base_salary AS "baseSalary",
        commissions AS "commissions",
        gross_total AS "grossTotal",
        net_total AS "netTotal",
        days_worked AS "daysWorked",
        absences_count AS "absencesCount",
        late_count AS "lateCount",
        status AS "status"
      FROM employee_payroll
      WHERE employee_id = p_employee_id
        AND (p_year IS NULL OR EXTRACT(YEAR FROM period_start) = p_year)
      ORDER BY period_start DESC;
  END sp_get_payroll_by_employee;

  -- ========================================================================
  -- COMISIONES
  -- ========================================================================

  PROCEDURE sp_register_commission_from_sale(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_sale_id          IN NUMBER,
    p_commission_rate  IN NUMBER
  ) IS
    v_base_amount NUMBER;
  BEGIN
    SELECT total_amount INTO v_base_amount
    FROM sales WHERE sale_id = p_sale_id;
    
    INSERT INTO employee_commissions (
      commission_id, employee_id, commission_type, reference_id,
      reference_date, base_amount, commission_rate, commission_amount,
      status, created_at
    )
    SELECT 
      seq_employee_commissions.NEXTVAL, p_employee_id, 'VENTA', p_sale_id,
      sale_date, v_base_amount, p_commission_rate, 
      v_base_amount * (p_commission_rate / 100),
      'PENDIENTE', SYSTIMESTAMP
    FROM sales WHERE sale_id = p_sale_id
    RETURNING commission_id INTO p_commission_id;
    
    COMMIT;
  END sp_register_commission_from_sale;

  PROCEDURE sp_register_commission_from_payment(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_payment_id       IN NUMBER,
    p_commission_rate  IN NUMBER
  ) IS
    v_base_amount NUMBER;
  BEGIN
    SELECT amount INTO v_base_amount
    FROM payments WHERE payment_id = p_payment_id;
    
    INSERT INTO employee_commissions (
      commission_id, employee_id, commission_type, reference_id,
      reference_date, base_amount, commission_rate, commission_amount,
      status, created_at
    )
    SELECT 
      seq_employee_commissions.NEXTVAL, p_employee_id, 'COBRO', p_payment_id,
      payment_date, v_base_amount, p_commission_rate,
      v_base_amount * (p_commission_rate / 100),
      'PENDIENTE', SYSTIMESTAMP
    FROM payments WHERE payment_id = p_payment_id
    RETURNING commission_id INTO p_commission_id;
    
    COMMIT;
  END sp_register_commission_from_payment;

  PROCEDURE sp_get_commissions_pending(
    p_employee_id      IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        commission_id AS "commissionId",
        employee_id AS "employeeId",
        commission_type AS "commissionType",
        reference_id AS "referenceId",
        reference_date AS "referenceDate",
        base_amount AS "baseAmount",
        commission_rate AS "commissionRate",
        commission_amount AS "commissionAmount",
        status AS "status"
      FROM employee_commissions
      WHERE status = 'PENDIENTE'
        AND (p_employee_id IS NULL OR employee_id = p_employee_id)
      ORDER BY reference_date;
  END sp_get_commissions_pending;

  -- ========================================================================
  -- FUNCIONES
  -- ========================================================================

  FUNCTION fn_get_employee_seniority_years(p_employee_id IN NUMBER) 
    RETURN NUMBER IS
    v_years NUMBER;
  BEGIN
    SELECT TRUNC(MONTHS_BETWEEN(SYSDATE, hire_date) / 12)
    INTO v_years
    FROM employees
    WHERE employee_id = p_employee_id;
    
    RETURN v_years;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_get_employee_seniority_years;

  FUNCTION fn_get_employee_total_commissions(
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE
  ) RETURN NUMBER IS
    v_total NUMBER;
  BEGIN
    SELECT NVL(SUM(commission_amount), 0)
    INTO v_total
    FROM employee_commissions
    WHERE employee_id = p_employee_id
      AND status = 'PENDIENTE'
      AND reference_date BETWEEN p_period_start AND p_period_end;
    
    RETURN v_total;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_get_employee_total_commissions;

  FUNCTION fn_calculate_worked_hours(
    p_check_in         IN TIMESTAMP,
    p_check_out        IN TIMESTAMP
  ) RETURN NUMBER IS
    v_hours NUMBER;
  BEGIN
    IF p_check_in IS NULL OR p_check_out IS NULL THEN
      RETURN 0;
    END IF;
    
    v_hours := EXTRACT(DAY FROM (p_check_out - p_check_in)) * 24 +
               EXTRACT(HOUR FROM (p_check_out - p_check_in)) +
               EXTRACT(MINUTE FROM (p_check_out - p_check_in)) / 60;
    
    RETURN ROUND(v_hours, 2);
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_calculate_worked_hours;

