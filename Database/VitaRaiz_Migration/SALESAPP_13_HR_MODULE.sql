-- ===========================================================================
-- VITARAIZ - MÓDULO DE RECURSOS HUMANOS (HR)
-- Script de creación de tablas para gestión de empleados
-- Fecha: 2026-04-09
-- ===========================================================================
-- Este módulo extiende la tabla USERS con información laboral completa:
-- - Datos de empleado (sueldo, antigüedad, documentos)
-- - Control de asistencias (faltas, retardos, asistencias)
-- - Nómina y pagos
-- - Comisiones por ventas/cobros
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- SECUENCIAS
-- ---------------------------------------------------------------------------
CREATE SEQUENCE SALESAPP.SEQ_EMPLOYEES
    START WITH 1000
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SALESAPP.SEQ_EMPLOYEE_ATTENDANCE
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SALESAPP.SEQ_EMPLOYEE_PAYROLL
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SALESAPP.SEQ_EMPLOYEE_COMMISSIONS
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

CREATE SEQUENCE SALESAPP.SEQ_EMPLOYEE_DOCUMENTS
    START WITH 1
    INCREMENT BY 1
    NOCACHE
    NOCYCLE;

-- ---------------------------------------------------------------------------
-- CATÁLOGO: TIPOS DE DOCUMENTOS DE EMPLEADOS
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (
    DOC_TYPE_ID   NUMBER          NOT NULL,
    DOC_TYPE_CODE VARCHAR2(50)    NOT NULL,
    DOC_TYPE_NAME VARCHAR2(100)   NOT NULL,
    DESCRIPTION   VARCHAR2(200),
    IS_REQUIRED   CHAR(1)         DEFAULT '0',
    DISPLAY_ORDER NUMBER          DEFAULT 0,
    IS_ACTIVE     CHAR(1)         DEFAULT '1',
    CONSTRAINT PK_CATALOG_EMPLOYEE_DOC_TYPES PRIMARY KEY (DOC_TYPE_ID),
    CONSTRAINT UQ_CATALOG_EMPLOYEE_DOC_CODE UNIQUE (DOC_TYPE_CODE)
);

-- Datos iniciales
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(1, 'FOTO_EMPLEADO', 'Fotografía del Empleado', 'Foto reciente del empleado', '1', 1);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(2, 'INE', 'Identificación Oficial (INE)', 'Copia de credencial INE vigente', '1', 2);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(3, 'SOLICITUD_EMPLEO', 'Solicitud de Empleo', 'Formato de solicitud lleno', '1', 3);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(4, 'COMPROBANTE_ESTUDIOS', 'Comprobante de Estudios', 'Certificado o título', '0', 4);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(5, 'COMPROBANTE_DOMICILIO', 'Comprobante de Domicilio', 'Recibo de servicios', '0', 5);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(6, 'CURP', 'CURP', 'Copia de CURP', '0', 6);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(7, 'RFC', 'RFC', 'Cédula de RFC', '0', 7);
INSERT INTO SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID, DOC_TYPE_CODE, DOC_TYPE_NAME, DESCRIPTION, IS_REQUIRED, DISPLAY_ORDER) VALUES
(8, 'CONTRATO', 'Contrato Laboral', 'Contrato firmado', '1', 8);

COMMIT;

-- ---------------------------------------------------------------------------
-- TABLA: EMPLOYEES (Datos laborales del empleado)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.EMPLOYEES (
    EMPLOYEE_ID           NUMBER          NOT NULL,
    USER_ID               NUMBER          NOT NULL,  -- FK a USERS
    EMPLOYEE_CODE         VARCHAR2(20)    NOT NULL,  -- Código único de empleado
    
    -- Datos laborales
    JOB_TITLE             VARCHAR2(100),             -- Puesto
    DEPARTMENT            VARCHAR2(100),             -- Departamento
    BASE_SALARY           NUMBER(10,2)   DEFAULT 0,
    COMMISSION_RATE       NUMBER(5,2)    DEFAULT 0,  -- % comisión (ej: 5.00 = 5%)
    HIRE_DATE             DATE           NOT NULL,
    TERMINATION_DATE      DATE,
    EMPLOYMENT_STATUS     VARCHAR2(20)   DEFAULT 'ACTIVO',  -- ACTIVO, BAJA, SUSPENDIDO
    
    -- Datos bancarios
    BANK_NAME             VARCHAR2(100),
    BANK_ACCOUNT          VARCHAR2(50),
    CLABE                 VARCHAR2(18),
    
    -- Contacto de emergencia 1
    EMERGENCY_CONTACT_1   VARCHAR2(100),
    EMERGENCY_PHONE_1     VARCHAR2(20),
    EMERGENCY_RELATION_1  VARCHAR2(50),
    
    -- Contacto de emergencia 2
    EMERGENCY_CONTACT_2   VARCHAR2(100),
    EMERGENCY_PHONE_2     VARCHAR2(20),
    EMERGENCY_RELATION_2  VARCHAR2(50),
    
    -- Beneficiario
    BENEFICIARY_NAME      VARCHAR2(100),
    BENEFICIARY_RELATION  VARCHAR2(50),
    
    -- Ubicación
    ADDRESS               VARCHAR2(300),
    GPS_LATITUDE          NUMBER(10,7),
    GPS_LONGITUDE         NUMBER(10,7),
    
    -- Observaciones
    NOTES                 CLOB,
    
    -- Auditoría
    CREATED_BY            NUMBER,
    CREATED_AT            TIMESTAMP(6)   DEFAULT CURRENT_TIMESTAMP,
    UPDATED_BY            NUMBER,
    UPDATED_AT            TIMESTAMP(6),
    
    CONSTRAINT PK_EMPLOYEES        PRIMARY KEY (EMPLOYEE_ID),
    CONSTRAINT UQ_EMPLOYEES_CODE   UNIQUE (EMPLOYEE_CODE),
    CONSTRAINT UQ_EMPLOYEES_USER   UNIQUE (USER_ID),
    CONSTRAINT FK_EMPLOYEES_USER   FOREIGN KEY (USER_ID)
        REFERENCES SALESAPP.USERS (USER_ID),
    CONSTRAINT CHK_EMPLOYEES_STATUS CHECK (EMPLOYMENT_STATUS IN ('ACTIVO', 'BAJA', 'SUSPENDIDO', 'INCAPACIDAD'))
);

COMMENT ON TABLE  SALESAPP.EMPLOYEES IS 'Datos laborales de empleados (extensión de USERS)';
COMMENT ON COLUMN SALESAPP.EMPLOYEES.EMPLOYEE_CODE IS 'Código único de empleado (ej: EMP001)';
COMMENT ON COLUMN SALESAPP.EMPLOYEES.COMMISSION_RATE IS 'Porcentaje de comisión para ventas/cobros';
COMMENT ON COLUMN SALESAPP.EMPLOYEES.EMPLOYMENT_STATUS IS 'ACTIVO, BAJA, SUSPENDIDO, INCAPACIDAD';

-- ---------------------------------------------------------------------------
-- TABLA: EMPLOYEE_DOCUMENTS (Documentos escaneados)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.EMPLOYEES_DOCUMENTS (
    DOCUMENT_ID      NUMBER          NOT NULL,
    EMPLOYEE_ID      NUMBER          NOT NULL,
    DOC_TYPE_ID      NUMBER          NOT NULL,
    FILE_PATH        VARCHAR2(500)   NOT NULL,
    FILE_NAME        VARCHAR2(200),
    FILE_SIZE        NUMBER,
    MIME_TYPE        VARCHAR2(100),
    UPLOAD_DATE      TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    UPLOADED_BY      NUMBER,
    NOTES            VARCHAR2(500),
    
    CONSTRAINT PK_EMPLOYEE_DOCUMENTS     PRIMARY KEY (DOCUMENT_ID),
    CONSTRAINT FK_EMPLOYEE_DOCS_EMP      FOREIGN KEY (EMPLOYEE_ID)
        REFERENCES SALESAPP.EMPLOYEES (EMPLOYEE_ID),
    CONSTRAINT FK_EMPLOYEE_DOCS_TYPE     FOREIGN KEY (DOC_TYPE_ID)
        REFERENCES SALESAPP.CATALOG_EMPLOYEE_DOC_TYPES (DOC_TYPE_ID)
);

COMMENT ON TABLE SALESAPP.EMPLOYEES_DOCUMENTS IS 'Documentos digitalizados de empleados (INE, contratos, etc)';

-- ---------------------------------------------------------------------------
-- TABLA: EMPLOYEE_ATTENDANCE (Control de asistencias)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.EMPLOYEE_ATTENDANCE (
    ATTENDANCE_ID    NUMBER          NOT NULL,
    EMPLOYEE_ID      NUMBER          NOT NULL,
    ATTENDANCE_DATE  DATE            NOT NULL,
    ATTENDANCE_TYPE  VARCHAR2(30)    NOT NULL,  -- ASISTENCIA, FALTA, RETARDO, FALTA_JUSTIFICADA
    CHECK_IN_TIME    TIMESTAMP(6),
    CHECK_OUT_TIME   TIMESTAMP(6),
    WORKED_HOURS     NUMBER(5,2),               -- Horas trabajadas
    NOTES            VARCHAR2(500),
    JUSTIFICATION    VARCHAR2(500),             -- Para faltas justificadas
    GPS_LATITUDE     NUMBER(10,7),
    GPS_LONGITUDE    NUMBER(10,7),
    DEVICE_ID        VARCHAR2(100),
    
    CREATED_AT       TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    CREATED_BY       NUMBER,
    
    CONSTRAINT PK_EMPLOYEE_ATTENDANCE     PRIMARY KEY (ATTENDANCE_ID),
    CONSTRAINT FK_ATTENDANCE_EMPLOYEE     FOREIGN KEY (EMPLOYEE_ID)
        REFERENCES SALESAPP.EMPLOYEES (EMPLOYEE_ID),
    CONSTRAINT CHK_ATTENDANCE_TYPE        CHECK (ATTENDANCE_TYPE IN 
        ('ASISTENCIA', 'FALTA', 'RETARDO', 'FALTA_JUSTIFICADA', 'PERMISO', 'VACACIONES', 'INCAPACIDAD'))
);

-- Índice para búsquedas rápidas por empleado y fecha
CREATE INDEX IDX_ATTENDANCE_EMP_DATE ON SALESAPP.EMPLOYEE_ATTENDANCE(EMPLOYEE_ID, ATTENDANCE_DATE);

COMMENT ON TABLE  SALESAPP.EMPLOYEE_ATTENDANCE IS 'Registro diario de asistencias, faltas y retardos';
COMMENT ON COLUMN SALESAPP.EMPLOYEE_ATTENDANCE.WORKED_HOURS IS 'Horas realmente trabajadas en el día';

-- ---------------------------------------------------------------------------
-- TABLA: EMPLOYEE_PAYROLL (Nómina/Pagos)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.EMPLOYEE_PAYROLL (
    PAYROLL_ID       NUMBER          NOT NULL,
    EMPLOYEE_ID      NUMBER          NOT NULL,
    
    -- Periodo
    PERIOD_START     DATE            NOT NULL,
    PERIOD_END       DATE            NOT NULL,
    PAYMENT_DATE     DATE,
    
    -- Conceptos
    BASE_SALARY      NUMBER(10,2)    DEFAULT 0,
    COMMISSIONS      NUMBER(10,2)    DEFAULT 0,
    BONUSES          NUMBER(10,2)    DEFAULT 0,
    OVERTIME         NUMBER(10,2)    DEFAULT 0,
    
    -- Deducciones
    DEDUCTIONS       NUMBER(10,2)    DEFAULT 0,
    ABSENCES         NUMBER(10,2)    DEFAULT 0,  -- Descuentos por faltas
    
    -- Totales
    GROSS_TOTAL      NUMBER(10,2)    DEFAULT 0,  -- Total bruto
    NET_TOTAL        NUMBER(10,2)    DEFAULT 0,  -- Total neto
    
    -- Stats del periodo
    DAYS_WORKED      NUMBER          DEFAULT 0,
    ABSENCES_COUNT   NUMBER          DEFAULT 0,
    LATE_COUNT       NUMBER          DEFAULT 0,
    
    -- Ventas/Cobros (para comisiones)
    SALES_COUNT      NUMBER          DEFAULT 0,  -- # ventas realizadas
    SALES_AMOUNT     NUMBER(10,2)    DEFAULT 0,  -- Monto total vendido
    COLLECTIONS_COUNT NUMBER         DEFAULT 0,  -- # cobros realizados
    COLLECTIONS_AMOUNT NUMBER(10,2)  DEFAULT 0,  -- Monto total cobrado
    
    STATUS           VARCHAR2(20)    DEFAULT 'PENDIENTE',  -- PENDIENTE, PAGADO, CANCELADO
    PAYMENT_METHOD   VARCHAR2(50),                         -- TRANSFERENCIA, EFECTIVO, CHEQUE
    PAYMENT_REFERENCE VARCHAR2(100),
    
    NOTES            CLOB,
    
    CREATED_BY       NUMBER,
    CREATED_AT       TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    APPROVED_BY      NUMBER,
    APPROVED_AT      TIMESTAMP(6),
    PAID_BY          NUMBER,
    PAID_AT          TIMESTAMP(6),
    
    CONSTRAINT PK_EMPLOYEE_PAYROLL       PRIMARY KEY (PAYROLL_ID),
    CONSTRAINT FK_PAYROLL_EMPLOYEE       FOREIGN KEY (EMPLOYEE_ID)
        REFERENCES SALESAPP.EMPLOYEES (EMPLOYEE_ID),
    CONSTRAINT CHK_PAYROLL_STATUS        CHECK (STATUS IN ('PENDIENTE', 'APROBADA', 'PAGADA', 'CANCELADA'))
);

-- Índice para búsquedas por periodo
CREATE INDEX IDX_PAYROLL_PERIOD ON SALESAPP.EMPLOYEE_PAYROLL(PERIOD_START, PERIOD_END);
CREATE INDEX IDX_PAYROLL_EMPLOYEE ON SALESAPP.EMPLOYEE_PAYROLL(EMPLOYEE_ID, PERIOD_START);

COMMENT ON TABLE  SALESAPP.EMPLOYEE_PAYROLL IS 'Nómina y pagos a empleados por periodo';
COMMENT ON COLUMN SALESAPP.EMPLOYEE_PAYROLL.COMMISSIONS IS 'Total de comisiones por ventas/cobros del periodo';

-- ---------------------------------------------------------------------------
-- TABLA: EMPLOYEE_COMMISSIONS (Detalle de comisiones)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.EMPLOYEE_COMMISSIONS (
    COMMISSION_ID    NUMBER          NOT NULL,
    EMPLOYEE_ID      NUMBER          NOT NULL,
    PAYROLL_ID       NUMBER,                    -- Se asigna cuando se procesa la nómina
    
    COMMISSION_TYPE  VARCHAR2(20)    NOT NULL,  -- VENTA, COBRO
    REFERENCE_ID     NUMBER          NOT NULL,  -- SALE_ID o PAYMENT_ID
    REFERENCE_DATE   DATE            NOT NULL,
    
    BASE_AMOUNT      NUMBER(10,2)    NOT NULL,  -- Monto base para calcular comisión
    COMMISSION_RATE  NUMBER(5,2)     NOT NULL,  -- % comisión aplicado
    COMMISSION_AMOUNT NUMBER(10,2)   NOT NULL,  -- Monto de comisión
    
    STATUS           VARCHAR2(20)    DEFAULT 'PENDIENTE',  -- PENDIENTE, PROCESADA, PAGADA
    
    CREATED_AT       TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    PROCESSED_AT     TIMESTAMP(6),
    
    CONSTRAINT PK_EMPLOYEE_COMMISSIONS    PRIMARY KEY (COMMISSION_ID),
    CONSTRAINT FK_COMMISSIONS_EMPLOYEE    FOREIGN KEY (EMPLOYEE_ID)
        REFERENCES SALESAPP.EMPLOYEES (EMPLOYEE_ID),
    CONSTRAINT FK_COMMISSIONS_PAYROLL     FOREIGN KEY (PAYROLL_ID)
        REFERENCES SALESAPP.EMPLOYEE_PAYROLL (PAYROLL_ID),
    CONSTRAINT CHK_COMMISSIONS_TYPE       CHECK (COMMISSION_TYPE IN ('VENTA', 'COBRO')),
    CONSTRAINT CHK_COMMISSIONS_STATUS     CHECK (STATUS IN ('PENDIENTE', 'PROCESADA', 'PAGADA', 'CANCELADA'))
);

-- Índices
CREATE INDEX IDX_COMMISSIONS_EMPLOYEE ON SALESAPP.EMPLOYEE_COMMISSIONS(EMPLOYEE_ID);
CREATE INDEX IDX_COMMISSIONS_PAYROLL ON SALESAPP.EMPLOYEE_COMMISSIONS(PAYROLL_ID);
CREATE INDEX IDX_COMMISSIONS_REF ON SALESAPP.EMPLOYEE_COMMISSIONS(COMMISSION_TYPE, REFERENCE_ID);

COMMENT ON TABLE  SALESAPP.EMPLOYEE_COMMISSIONS IS 'Detalle de comisiones por ventas y cobros';
COMMENT ON COLUMN SALESAPP.EMPLOYEE_COMMISSIONS.REFERENCE_ID IS 'ID de venta (SALES) o pago (PAYMENTS)';

-- ---------------------------------------------------------------------------
-- VISTAS
-- ---------------------------------------------------------------------------

-- Vista: Empleados activos con datos completos
CREATE OR REPLACE VIEW SALESAPP.VW_EMPLOYEES_ACTIVE AS
SELECT 
    e.EMPLOYEE_ID,
    e.EMPLOYEE_CODE,
    e.USER_ID,
    u.USERNAME,
    u.FULL_NAME,
    u.EMAIL,
    u.PHONE,
    r.ROLE_NAME,
    z.ZONE_NAME,
    e.JOB_TITLE,
    e.DEPARTMENT,
    e.BASE_SALARY,
    e.COMMISSION_RATE,
    e.HIRE_DATE,
    TRUNC(MONTHS_BETWEEN(SYSDATE, e.HIRE_DATE) / 12) AS YEARS_OF_SERVICE,
    MOD(TRUNC(MONTHS_BETWEEN(SYSDATE, e.HIRE_DATE)), 12) AS MONTHS_OF_SERVICE,
    e.EMPLOYMENT_STATUS,
    e.BANK_NAME,
    e.BANK_ACCOUNT,
    e.EMERGENCY_CONTACT_1,
    e.EMERGENCY_PHONE_1
FROM SALESAPP.EMPLOYEES e
JOIN SALESAPP.USERS u ON e.USER_ID = u.USER_ID
JOIN SALESAPP.ROLES r ON u.ROLE_ID = r.ROLE_ID
LEFT JOIN SALESAPP.ZONES z ON u.ZONE_ID = z.ZONE_ID
WHERE e.EMPLOYMENT_STATUS = 'ACTIVO'
  AND u.IS_ACTIVE = '1';

-- Vista: Resumen de asistencias por empleado (mes actual)
CREATE OR REPLACE VIEW SALESAPP.VW_ATTENDANCE_SUMMARY_CURRENT AS
SELECT 
    e.EMPLOYEE_ID,
    e.EMPLOYEE_CODE,
    u.FULL_NAME,
    COUNT(CASE WHEN a.ATTENDANCE_TYPE = 'ASISTENCIA' THEN 1 END) AS ASISTENCIAS,
    COUNT(CASE WHEN a.ATTENDANCE_TYPE = 'RETARDO' THEN 1 END) AS RETARDOS,
    COUNT(CASE WHEN a.ATTENDANCE_TYPE = 'FALTA' THEN 1 END) AS FALTAS,
    COUNT(CASE WHEN a.ATTENDANCE_TYPE = 'FALTA_JUSTIFICADA' THEN 1 END) AS FALTAS_JUSTIFICADAS,
    SUM(NVL(a.WORKED_HOURS, 0)) AS TOTAL_HORAS
FROM SALESAPP.EMPLOYEES e
JOIN SALESAPP.USERS u ON e.USER_ID = u.USER_ID
LEFT JOIN SALESAPP.EMPLOYEE_ATTENDANCE a ON e.EMPLOYEE_ID = a.EMPLOYEE_ID
    AND TRUNC(a.ATTENDANCE_DATE, 'MM') = TRUNC(SYSDATE, 'MM')
WHERE e.EMPLOYMENT_STATUS = 'ACTIVO'
GROUP BY e.EMPLOYEE_ID, e.EMPLOYEE_CODE, u.FULL_NAME;

COMMIT;

-- ===========================================================================
-- FIN DEL SCRIPT
-- ===========================================================================
