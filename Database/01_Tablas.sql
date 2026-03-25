-- ========================================================================
-- SCRIPT: Creación de Tablas de Catálogos Dinámicos
-- FECHA: 2026-03-24
-- DESCRIPCION: Sistema de catálogos dinámicos para evitar hardcode
-- ========================================================================

-- Tabla de estados de venta
CREATE TABLE CATALOG_SALE_STATUSES (
    STATUS_CODE VARCHAR2(50) PRIMARY KEY,
    STATUS_NAME VARCHAR2(100) NOT NULL,
    DESCRIPTION VARCHAR2(500),
    DISPLAY_ORDER NUMBER DEFAULT 0,
    COLOR_HEX VARCHAR2(7),
    ICON VARCHAR2(50),
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP,
    UPDATED_AT TIMESTAMP
);

-- Tabla de estados de pago
CREATE TABLE CATALOG_PAYMENT_STATUSES (
    STATUS_CODE VARCHAR2(50) PRIMARY KEY,
    STATUS_NAME VARCHAR2(100) NOT NULL,
    DESCRIPTION VARCHAR2(500),
    DISPLAY_ORDER NUMBER DEFAULT 0,
    COLOR_HEX VARCHAR2(7),
    ICON VARCHAR2(50),
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP,
    UPDATED_AT TIMESTAMP
);

-- Tabla de estados de riesgo
CREATE TABLE CATALOG_RISK_STATUSES (
    STATUS_CODE VARCHAR2(50) PRIMARY KEY,
    STATUS_NAME VARCHAR2(100) NOT NULL,
    DESCRIPTION VARCHAR2(500),
    MIN_DAYS NUMBER,
    MAX_DAYS NUMBER,
    COLOR_HEX VARCHAR2(7) NOT NULL,
    ICON VARCHAR2(50),
    DISPLAY_ORDER NUMBER DEFAULT 0,
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP,
    UPDATED_AT TIMESTAMP
);

-- Tabla de temas/colores de la aplicación
CREATE TABLE CATALOG_APP_THEMES (
    THEME_CODE VARCHAR2(50) PRIMARY KEY,
    THEME_NAME VARCHAR2(100) NOT NULL,
    PRIMARY_COLOR VARCHAR2(7) NOT NULL,
    SECONDARY_COLOR VARCHAR2(7),
    ACCENT_COLOR VARCHAR2(7),
    BACKGROUND_COLOR VARCHAR2(7),
    TEXT_COLOR VARCHAR2(7),
    IS_DEFAULT NUMBER(1) DEFAULT 0,
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Tabla de plantillas de notificaciones
CREATE TABLE CATALOG_NOTIFICATION_TEMPLATES (
    TEMPLATE_CODE VARCHAR2(50) PRIMARY KEY,
    TEMPLATE_NAME VARCHAR2(100) NOT NULL,
    TEMPLATE_TYPE VARCHAR2(50), -- 'SMS', 'WHATSAPP', 'EMAIL'
    SUBJECT VARCHAR2(500),
    MESSAGE_BODY CLOB NOT NULL,
    VARIABLES VARCHAR2(1000), -- JSON con lista de variables permitidas
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP,
    UPDATED_AT TIMESTAMP
);

-- Tabla de configuraciones generales
CREATE TABLE CATALOG_APP_SETTINGS (
    SETTING_KEY VARCHAR2(100) PRIMARY KEY,
    SETTING_VALUE VARCHAR2(4000) NOT NULL,
    SETTING_TYPE VARCHAR2(50), -- 'STRING', 'NUMBER', 'BOOLEAN', 'JSON'
    DESCRIPTION VARCHAR2(500),
    CATEGORY VARCHAR2(100),
    IS_PUBLIC NUMBER(1) DEFAULT 0, -- 1 = visible para app móvil
    UPDATED_BY NUMBER,
    UPDATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Tabla de tipos de acción de visita del cobrador
CREATE TABLE CATALOG_VISIT_ACTIONS (
    ACTION_CODE VARCHAR2(50) PRIMARY KEY,
    ACTION_NAME VARCHAR2(100) NOT NULL,
    DESCRIPTION VARCHAR2(500),
    ICON VARCHAR2(50),
    COLOR_HEX VARCHAR2(7),
    REQUIRES_NOTE NUMBER(1) DEFAULT 0,
    REQUIRES_PHOTO NUMBER(1) DEFAULT 0,
    DISPLAY_ORDER NUMBER DEFAULT 0,
    IS_ACTIVE NUMBER(1) DEFAULT 1,
    CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- ========================================================================
-- DATOS INICIALES - Estados de Venta
-- ========================================================================
INSERT INTO CATALOG_SALE_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('POR_INICIAR', 'Por Iniciar', 'Venta registrada pero aún no iniciada', 1, '#6C757D', '⏳');

INSERT INTO CATALOG_SALE_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('EN_PROCESO', 'En Proceso', 'Venta activa con pagos pendientes', 2, '#28A745', '💰');

INSERT INTO CATALOG_SALE_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('LIQUIDADO', 'Liquidado', 'Venta totalmente pagada', 3, '#17A2B8', '✅');

INSERT INTO CATALOG_SALE_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('CANCELADO', 'Cancelado', 'Venta cancelada', 4, '#DC3545', '❌');

INSERT INTO CATALOG_SALE_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('ANULADO', 'Anulado', 'Venta anulada por administrador', 5, '#FFC107', '⚠️');

-- ========================================================================
-- DATOS INICIALES - Estados de Pago
-- ========================================================================
INSERT INTO CATALOG_PAYMENT_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('PENDING', 'Pendiente', 'Pago registrado esperando aprobación', 1, '#FFC107', '⏳');

INSERT INTO CATALOG_PAYMENT_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('APPROVED', 'Aprobado', 'Pago aprobado y aplicado', 2, '#28A745', '✅');

INSERT INTO CATALOG_PAYMENT_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('REJECTED', 'Rechazado', 'Pago rechazado', 3, '#DC3545', '❌');

INSERT INTO CATALOG_PAYMENT_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, DISPLAY_ORDER, COLOR_HEX, ICON) 
VALUES ('CANCELLED', 'Cancelado', 'Pago cancelado', 4, '#6C757D', '🚫');

-- ========================================================================
-- DATOS INICIALES - Estados de Riesgo
-- ========================================================================
INSERT INTO CATALOG_RISK_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, MIN_DAYS, MAX_DAYS, COLOR_HEX, ICON, DISPLAY_ORDER) 
VALUES ('VERDE', 'Verde - Al día', 'Cliente al día con sus pagos', 0, 7, '#28A745', '🟢', 1);

INSERT INTO CATALOG_RISK_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, MIN_DAYS, MAX_DAYS, COLOR_HEX, ICON, DISPLAY_ORDER) 
VALUES ('AMARILLO', 'Amarillo - Atención', 'Cliente con ligero atraso', 8, 14, '#FFC107', '🟡', 2);

INSERT INTO CATALOG_RISK_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, MIN_DAYS, MAX_DAYS, COLOR_HEX, ICON, DISPLAY_ORDER) 
VALUES ('ROJO', 'Rojo - Atrasado', 'Cliente con atraso significativo', 15, 21, '#DC3545', '🔴', 3);

INSERT INTO CATALOG_RISK_STATUSES (STATUS_CODE, STATUS_NAME, DESCRIPTION, MIN_DAYS, MAX_DAYS, COLOR_HEX, ICON, DISPLAY_ORDER) 
VALUES ('CRITICO', 'Crítico - Muy atrasado', 'Cliente en situación crítica', 22, 9999, '#6C757D', '⚫', 4);

-- ========================================================================
-- DATOS INICIALES - Tema por defecto
-- ========================================================================
INSERT INTO CATALOG_APP_THEMES (THEME_CODE, THEME_NAME, PRIMARY_COLOR, SECONDARY_COLOR, ACCENT_COLOR, BACKGROUND_COLOR, TEXT_COLOR, IS_DEFAULT) 
VALUES ('VITARAIZ_DEFAULT', 'VitaRaiz Verde', '#28A745', '#20C997', '#17A2B8', '#F0F4F8', '#333333', 1);

-- ========================================================================
-- DATOS INICIALES - Acciones de visita
-- ========================================================================
INSERT INTO CATALOG_VISIT_ACTIONS (ACTION_CODE, ACTION_NAME, DESCRIPTION, ICON, COLOR_HEX, REQUIRES_NOTE, REQUIRES_PHOTO, DISPLAY_ORDER) 
VALUES ('PAGO_RECIBIDO', 'Pago Recibido', 'Cliente realizó un pago', '💰', '#28A745', 0, 1, 1);

INSERT INTO CATALOG_VISIT_ACTIONS (ACTION_CODE, ACTION_NAME, DESCRIPTION, ICON, COLOR_HEX, REQUIRES_NOTE, REQUIRES_PHOTO, DISPLAY_ORDER) 
VALUES ('CLIENTE_NO_ENCONTRADO', 'Cliente No Encontrado', 'No se encontró al cliente en la dirección', '🏠', '#FFC107', 1, 0, 2);

INSERT INTO CATALOG_VISIT_ACTIONS (ACTION_CODE, ACTION_NAME, DESCRIPTION, ICON, COLOR_HEX, REQUIRES_NOTE, REQUIRES_PHOTO, DISPLAY_ORDER) 
VALUES ('PROMESA_PAGO', 'Promesa de Pago', 'Cliente promete pagar en fecha específica', '📅', '#17A2B8', 1, 0, 3);

INSERT INTO CATALOG_VISIT_ACTIONS (ACTION_CODE, ACTION_NAME, DESCRIPTION, ICON, COLOR_HEX, REQUIRES_NOTE, REQUIRES_PHOTO, DISPLAY_ORDER) 
VALUES ('CLIENTE_REHUSA', 'Cliente Se Rehúsa', 'Cliente se niega a pagar', '🚫', '#DC3545', 1, 0, 4);

-- ========================================================================
-- DATOS INICIALES - Configuraciones
-- ========================================================================
INSERT INTO CATALOG_APP_SETTINGS (SETTING_KEY, SETTING_VALUE, SETTING_TYPE, DESCRIPTION, CATEGORY, IS_PUBLIC) 
VALUES ('MAX_PAYMENT_ATTEMPT_DAYS', '30', 'NUMBER', 'Días máximos para intentar cobrar', 'COBRANZA', 0);

INSERT INTO CATALOG_APP_SETTINGS (SETTING_KEY, SETTING_VALUE, SETTING_TYPE, DESCRIPTION, CATEGORY, IS_PUBLIC) 
VALUES ('MIN_PAYMENT_AMOUNT', '10', 'NUMBER', 'Monto mínimo de pago aceptado', 'PAGOS', 1);

INSERT INTO CATALOG_APP_SETTINGS (SETTING_KEY, SETTING_VALUE, SETTING_TYPE, DESCRIPTION, CATEGORY, IS_PUBLIC) 
VALUES ('REQUIRE_PHOTO_FOR_PAYMENT', 'true', 'BOOLEAN', 'Requiere foto para registrar pago', 'PAGOS', 1);

INSERT INTO CATALOG_APP_SETTINGS (SETTING_KEY, SETTING_VALUE, SETTING_TYPE, DESCRIPTION, CATEGORY, IS_PUBLIC) 
VALUES ('APP_VERSION', '1.0.0', 'STRING', 'Versión actual de la aplicación', 'APP', 1);

INSERT INTO CATALOG_APP_SETTINGS (SETTING_KEY, SETTING_VALUE, SETTING_TYPE, DESCRIPTION, CATEGORY, IS_PUBLIC) 
VALUES ('WHATSAPP_REMINDER_TEMPLATE', 'PAYMENT_REMINDER', 'STRING', 'Template por defecto para recordatorios', 'NOTIFICACIONES', 0);

-- ========================================================================
-- DATOS INICIALES - Plantillas de notificaciones
-- ========================================================================
INSERT INTO CATALOG_NOTIFICATION_TEMPLATES (TEMPLATE_CODE, TEMPLATE_NAME, TEMPLATE_TYPE, SUBJECT, MESSAGE_BODY, VARIABLES) 
VALUES ('PAYMENT_REMINDER', 'Recordatorio de Pago', 'WHATSAPP', NULL, 
        'Hola {CUSTOMER_NAME}, te recordamos que tienes un pago pendiente de ${AMOUNT} con vencimiento el {DUE_DATE}. ¡Gracias por tu preferencia! - VitaRaiz',
        '["CUSTOMER_NAME","AMOUNT","DUE_DATE"]');

INSERT INTO CATALOG_NOTIFICATION_TEMPLATES (TEMPLATE_CODE, TEMPLATE_NAME, TEMPLATE_TYPE, SUBJECT, MESSAGE_BODY, VARIABLES) 
VALUES ('PAYMENT_CONFIRMATION', 'Confirmación de Pago', 'WHATSAPP', NULL,
        'Hola {CUSTOMER_NAME}, confirmamos tu pago de ${AMOUNT}. Nuevo saldo: ${BALANCE}. ¡Gracias! - VitaRaiz',
        '["CUSTOMER_NAME","AMOUNT","BALANCE"]');

-- ========================================================================
-- COMMIT DE TODOS LOS INSERTS
-- ========================================================================
COMMIT;

-- ========================================================================
-- COMENTARIOS DE LAS TABLAS
-- ========================================================================
COMMENT ON TABLE CATALOG_SALE_STATUSES IS 'Catálogo dinámico de estados de venta';
COMMENT ON TABLE CATALOG_PAYMENT_STATUSES IS 'Catálogo dinámico de estados de pago';
COMMENT ON TABLE CATALOG_RISK_STATUSES IS 'Catálogo dinámico de estados de riesgo';
COMMENT ON TABLE CATALOG_APP_THEMES IS 'Temas y colores de la aplicación';
COMMENT ON TABLE CATALOG_NOTIFICATION_TEMPLATES IS 'Plantillas de notificaciones parametrizadas';
COMMENT ON TABLE CATALOG_APP_SETTINGS IS 'Configuraciones generales de la aplicación';
COMMENT ON TABLE CATALOG_VISIT_ACTIONS IS 'Tipos de acciones durante visitas del cobrador';

-- FIN DEL SCRIPT
