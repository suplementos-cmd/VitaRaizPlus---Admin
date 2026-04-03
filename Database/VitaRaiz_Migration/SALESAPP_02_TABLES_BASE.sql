-- ===========================================================================
-- VITARAIZ - MIGRACIÓN ESQUEMA SALESAPP
-- Parte 02/09: TABLAS BASE (sin dependencias de FK externas, o solo auto-referencia)
-- Orden de creación: ZONES, ROLES, PERMISSIONS, CATALOG_SALE_STATUSES,
--   CATALOG_RISK_STATUSES, CATALOG_APP_THEMES, CATALOG_APP_SETTINGS,
--   CATALOG_NOTIFICATION_TEMPLATES, PRODUCT_CATEGORIES, WHATSAPP_TEMPLATES,
--   CATALOG_PAYMENT_STATUSES (auto-FK en PARENT_STATUS_ID)
-- Generado: 2026-04-03
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- ZONES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.ZONES (
    ZONE_ID     NUMBER          NOT NULL,
    ZONE_NAME   VARCHAR2(100)   NOT NULL,
    ZONE_CODE   VARCHAR2(10),
    DESCRIPTION VARCHAR2(200),
    IS_ACTIVE   CHAR(1)         DEFAULT '1',
    CREATED_AT  TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT PK_ZONES PRIMARY KEY (ZONE_ID),
    CONSTRAINT UQ_ZONES_CODE UNIQUE (ZONE_CODE)
);

-- ---------------------------------------------------------------------------
-- ROLES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.ROLES (
    ROLE_ID               NUMBER          NOT NULL,
    ROLE_NAME             VARCHAR2(50)    NOT NULL,
    ROLE_DESCRIPTION      VARCHAR2(200),
    DEFAULT_THEME_COLOR   VARCHAR2(7),
    CREATED_AT            TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    DEFAULT_THEME_LIGHT   VARCHAR2(7)     DEFAULT '#C8E6C9',
    DEFAULT_THEME_LIGHTER VARCHAR2(7)     DEFAULT '#E8F5E9',
    DESCRIPTION           VARCHAR2(500),
    CONSTRAINT PK_ROLES PRIMARY KEY (ROLE_ID),
    CONSTRAINT UQ_ROLES_NAME UNIQUE (ROLE_NAME),
    CONSTRAINT CHK_ROLE_NAME CHECK (ROLE_NAME IN (
        'Vendedora',
        'SupervisoraJunior',
        'SupervisoraSenior',
        'Cobrador',
        'SupervisorCobradorJunior',
        'SupervisorCobradorSenior',
        'AdminJunior',
        'AdminFull'
    ))
);

-- ---------------------------------------------------------------------------
-- PERMISSIONS
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.PERMISSIONS (
    PERMISSION_ID   NUMBER          NOT NULL,
    PERMISSION_NAME VARCHAR2(100)   NOT NULL,
    MODULE          VARCHAR2(50),
    DESCRIPTION     VARCHAR2(200),
    CREATED_AT      TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT PK_PERMISSIONS PRIMARY KEY (PERMISSION_ID),
    CONSTRAINT UQ_PERMISSION_NAME UNIQUE (PERMISSION_NAME)
);

-- ---------------------------------------------------------------------------
-- CATALOG_SALE_STATUSES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_SALE_STATUSES (
    STATUS_CODE   VARCHAR2(50)    NOT NULL,
    STATUS_NAME   VARCHAR2(100)   NOT NULL,
    DESCRIPTION   VARCHAR2(500),
    DISPLAY_ORDER NUMBER          DEFAULT 0,
    COLOR_HEX     VARCHAR2(7),
    ICON          VARCHAR2(50),
    IS_ACTIVE     NUMBER(1,0)     DEFAULT 1,
    CREATED_AT    TIMESTAMP(6)    DEFAULT SYSTIMESTAMP,
    UPDATED_AT    TIMESTAMP(6),
    CONSTRAINT PK_CATALOG_SALE_STATUSES PRIMARY KEY (STATUS_CODE)
);

-- ---------------------------------------------------------------------------
-- CATALOG_RISK_STATUSES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_RISK_STATUSES (
    STATUS_CODE   VARCHAR2(50)    NOT NULL,
    STATUS_NAME   VARCHAR2(100)   NOT NULL,
    DESCRIPTION   VARCHAR2(500),
    MIN_DAYS      NUMBER,
    MAX_DAYS      NUMBER,
    COLOR_HEX     VARCHAR2(7)     NOT NULL,
    ICON          VARCHAR2(50),
    DISPLAY_ORDER NUMBER          DEFAULT 0,
    IS_ACTIVE     NUMBER(1,0)     DEFAULT 1,
    CREATED_AT    TIMESTAMP(6)    DEFAULT SYSTIMESTAMP,
    UPDATED_AT    TIMESTAMP(6),
    CONSTRAINT PK_CATALOG_RISK_STATUSES PRIMARY KEY (STATUS_CODE)
);

-- ---------------------------------------------------------------------------
-- CATALOG_APP_THEMES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_APP_THEMES (
    THEME_CODE        VARCHAR2(50)    NOT NULL,
    THEME_NAME        VARCHAR2(100)   NOT NULL,
    PRIMARY_COLOR     VARCHAR2(7)     NOT NULL,
    SECONDARY_COLOR   VARCHAR2(7),
    ACCENT_COLOR      VARCHAR2(7),
    BACKGROUND_COLOR  VARCHAR2(7),
    TEXT_COLOR        VARCHAR2(7),
    IS_DEFAULT        NUMBER(1,0)     DEFAULT 0,
    IS_ACTIVE         NUMBER(1,0)     DEFAULT 1,
    CREATED_AT        TIMESTAMP(6)    DEFAULT SYSTIMESTAMP,
    CONSTRAINT PK_CATALOG_APP_THEMES PRIMARY KEY (THEME_CODE)
);

-- ---------------------------------------------------------------------------
-- CATALOG_APP_SETTINGS
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_APP_SETTINGS (
    SETTING_KEY   VARCHAR2(100)   NOT NULL,
    SETTING_VALUE VARCHAR2(4000)  NOT NULL,
    SETTING_TYPE  VARCHAR2(50),
    DESCRIPTION   VARCHAR2(500),
    CATEGORY      VARCHAR2(100),
    IS_PUBLIC     NUMBER(1,0)     DEFAULT 0,
    UPDATED_BY    NUMBER,
    UPDATED_AT    TIMESTAMP(6)    DEFAULT SYSTIMESTAMP,
    CONSTRAINT PK_CATALOG_APP_SETTINGS PRIMARY KEY (SETTING_KEY)
);

-- ---------------------------------------------------------------------------
-- CATALOG_NOTIFICATION_TEMPLATES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_NOTIFICATION_TEMPLATES (
    TEMPLATE_CODE VARCHAR2(50)    NOT NULL,
    TEMPLATE_NAME VARCHAR2(100)   NOT NULL,
    TEMPLATE_TYPE VARCHAR2(50),
    SUBJECT       VARCHAR2(500),
    MESSAGE_BODY  CLOB            NOT NULL,
    VARIABLES     VARCHAR2(1000),
    IS_ACTIVE     NUMBER(1,0)     DEFAULT 1,
    CREATED_AT    TIMESTAMP(6)    DEFAULT SYSTIMESTAMP,
    UPDATED_AT    TIMESTAMP(6),
    CONSTRAINT PK_CATALOG_NOTIF_TMPL PRIMARY KEY (TEMPLATE_CODE)
);

-- ---------------------------------------------------------------------------
-- PRODUCT_CATEGORIES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.PRODUCT_CATEGORIES (
    CATEGORY_ID   NUMBER          NOT NULL,
    CATEGORY_NAME VARCHAR2(100)   NOT NULL,
    DESCRIPTION   VARCHAR2(200),
    CREATED_AT    TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT PK_PRODUCT_CATEGORIES PRIMARY KEY (CATEGORY_ID)
);

-- ---------------------------------------------------------------------------
-- WHATSAPP_TEMPLATES
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.WHATSAPP_TEMPLATES (
    TEMPLATE_ID   NUMBER          NOT NULL,
    TEMPLATE_NAME VARCHAR2(100)   NOT NULL,
    TEMPLATE_BODY VARCHAR2(4000)  NOT NULL,
    VARIABLES     VARCHAR2(500),
    IS_ACTIVE     CHAR(1)         DEFAULT '1',
    CREATED_AT    TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT PK_WHATSAPP_TEMPLATES PRIMARY KEY (TEMPLATE_ID),
    CONSTRAINT UQ_WA_TEMPLATE_NAME UNIQUE (TEMPLATE_NAME)
);

-- ---------------------------------------------------------------------------
-- CATALOG_PAYMENT_STATUSES
-- Auto-referencia en PARENT_STATUS_ID (Oracle permite FK circular en misma tabla)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.CATALOG_PAYMENT_STATUSES (
    STATUS_ID        NUMBER          NOT NULL,
    STATUS_KEY       VARCHAR2(50)    NOT NULL,
    STATUS_TYPE      VARCHAR2(20)    NOT NULL,
    STATUS_NAME      VARCHAR2(100)   NOT NULL,
    DESCRIPTION      VARCHAR2(500),
    PARENT_STATUS_ID NUMBER,
    DISPLAY_ORDER    NUMBER          DEFAULT 0  NOT NULL,
    COLOR_HEX        VARCHAR2(10),
    ICON             VARCHAR2(100),
    REQUIRES_NOTE    NUMBER(1,0)     DEFAULT 0  NOT NULL,
    REQUIRES_PHOTO   NUMBER(1,0)     DEFAULT 0  NOT NULL,
    IS_ACTIVE        NUMBER(1,0)     DEFAULT 1  NOT NULL,
    CREATED_AT       TIMESTAMP(6)    DEFAULT SYSTIMESTAMP NOT NULL,
    UPDATED_AT       TIMESTAMP(6),
    CONSTRAINT PK_CPS_STATUS_ID   PRIMARY KEY (STATUS_ID),
    CONSTRAINT UQ_CPS_STATUS_CODE UNIQUE (STATUS_KEY),
    CONSTRAINT CHK_CPS_TYPE   CHECK (STATUS_TYPE    IN ('WORKFLOW','VISIT_ACTION')),
    CONSTRAINT CHK_CPS_NOTE   CHECK (REQUIRES_NOTE  IN (0,1)),
    CONSTRAINT CHK_CPS_PHOTO  CHECK (REQUIRES_PHOTO IN (0,1)),
    CONSTRAINT CHK_CPS_ACTIVE CHECK (IS_ACTIVE      IN (0,1)),
    CONSTRAINT FK_CPS_PARENT_ID FOREIGN KEY (PARENT_STATUS_ID)
        REFERENCES SALESAPP.CATALOG_PAYMENT_STATUSES (STATUS_ID)
);

COMMENT ON TABLE SALESAPP.CATALOG_PAYMENT_STATUSES IS
    'Catálogo unificado: estados de aprobación (WORKFLOW) y acciones de visita del cobrador (VISIT_ACTION)';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.STATUS_ID IS
    'Clave primaria numérica estable. Usar en FKs y constantes de código.';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.STATUS_KEY IS
    'Alias de texto inmutable (ex STATUS_CODE). Solo para uso interno del package.';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.STATUS_NAME IS
    'Nombre de presentación al usuario final.';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.STATUS_TYPE IS
    'WORKFLOW = estado de aprobación del pago | VISIT_ACTION = acción de visita del cobrador';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.PARENT_STATUS_ID IS
    'NULL = estado raíz | FK numérica al STATUS_ID del padre (sub-estado).';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.REQUIRES_NOTE IS
    '1 = obligar campo nota al registrar este estado.';
COMMENT ON COLUMN SALESAPP.CATALOG_PAYMENT_STATUSES.REQUIRES_PHOTO IS
    '1 = obligar foto al registrar este estado.';
