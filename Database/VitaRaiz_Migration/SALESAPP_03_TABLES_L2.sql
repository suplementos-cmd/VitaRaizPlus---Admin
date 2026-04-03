-- ===========================================================================
-- VITARAIZ - MIGRACIÓN ESQUEMA SALESAPP
-- Parte 03/09: TABLAS NIVEL 2
-- Dependen de tablas base: USERS, PROFILE_THEMES, ROLE_PERMISSIONS, PRODUCTS
-- Generado: 2026-04-03
-- Prerequisito: ejecutar 01_SEQUENCES.sql y 02_TABLES_BASE.sql primero
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- USERS   (FK → ROLES, ZONES, auto-ref SUPERVISOR_ID)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.USERS (
    USER_ID       NUMBER          NOT NULL,
    USERNAME      VARCHAR2(50)    NOT NULL,
    PASSWORD_HASH VARCHAR2(255)   NOT NULL,
    FULL_NAME     VARCHAR2(100)   NOT NULL,
    EMAIL         VARCHAR2(100),
    PHONE         VARCHAR2(20),
    ROLE_ID       NUMBER          NOT NULL,
    ZONE_ID       NUMBER,
    SUPERVISOR_ID NUMBER,
    IS_ACTIVE     CHAR(1)         DEFAULT '1',
    LAST_LOGIN    TIMESTAMP(6),
    CREATED_AT    TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    UPDATED_AT    TIMESTAMP(6),
    CONSTRAINT PK_USERS         PRIMARY KEY (USER_ID),
    CONSTRAINT UQ_USERS_USERNAME UNIQUE (USERNAME),
    CONSTRAINT UQ_USERS_EMAIL    UNIQUE (EMAIL),
    CONSTRAINT FK_USERS_ROLE       FOREIGN KEY (ROLE_ID)
        REFERENCES SALESAPP.ROLES (ROLE_ID),
    CONSTRAINT FK_USERS_ZONE       FOREIGN KEY (ZONE_ID)
        REFERENCES SALESAPP.ZONES (ZONE_ID),
    CONSTRAINT FK_USERS_SUPERVISOR FOREIGN KEY (SUPERVISOR_ID)
        REFERENCES SALESAPP.USERS (USER_ID)
);

COMMENT ON TABLE  SALESAPP.USERS IS
    'Usuarios del sistema — cobradores, supervisores y administradores';
COMMENT ON COLUMN SALESAPP.USERS.ROLE_ID IS
    'Perfil del usuario (FK → ROLES). Determina permisos y tema visual.';
COMMENT ON COLUMN SALESAPP.USERS.PASSWORD_HASH IS
    'Hash BCrypt de la contraseña. Nunca almacenar en texto claro.';

-- ---------------------------------------------------------------------------
-- PROFILE_THEMES   (FK → ROLES)  1 tema por rol
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.PROFILE_THEMES (
    THEME_ID         NUMBER        NOT NULL,
    ROLE_ID          NUMBER        NOT NULL,
    THEME_NAME       VARCHAR2(50),
    PRIMARY_COLOR    VARCHAR2(7),
    SECONDARY_COLOR  VARCHAR2(7),
    ACCENT_COLOR     VARCHAR2(7),
    ICON_NAME        VARCHAR2(50),
    CREATED_AT       TIMESTAMP(6)  DEFAULT CURRENT_TIMESTAMP,
    BACKGROUND_COLOR VARCHAR2(7)   DEFAULT '#FAFAFA',
    TEXT_COLOR       VARCHAR2(7)   DEFAULT '#333333',
    IS_ACTIVE        NUMBER(1,0)   DEFAULT 1 NOT NULL,
    CONSTRAINT PK_PROFILE_THEMES    PRIMARY KEY (THEME_ID),
    CONSTRAINT UQ_PROFILE_THEMES_ROLE UNIQUE (ROLE_ID),
    CONSTRAINT CHK_PT_ACTIVE        CHECK (IS_ACTIVE IN (0,1)),
    CONSTRAINT FK_THEMES_ROLE       FOREIGN KEY (ROLE_ID)
        REFERENCES SALESAPP.ROLES (ROLE_ID)
);

COMMENT ON TABLE SALESAPP.PROFILE_THEMES  IS 'Configuración de colores y tema por rol (uno por rol)';

-- ---------------------------------------------------------------------------
-- ROLE_PERMISSIONS   (FK → ROLES, PERMISSIONS)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.ROLE_PERMISSIONS (
    ROLE_ID       NUMBER        NOT NULL,
    PERMISSION_ID NUMBER        NOT NULL,
    GRANTED_AT    TIMESTAMP(6)  DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT PK_ROLE_PERMISSIONS PRIMARY KEY (ROLE_ID, PERMISSION_ID),
    CONSTRAINT FK_RP_ROLE       FOREIGN KEY (ROLE_ID)
        REFERENCES SALESAPP.ROLES (ROLE_ID),
    CONSTRAINT FK_RP_PERMISSION FOREIGN KEY (PERMISSION_ID)
        REFERENCES SALESAPP.PERMISSIONS (PERMISSION_ID)
);

COMMENT ON TABLE SALESAPP.ROLE_PERMISSIONS IS 'Asignación de permisos a roles (N:N)';

-- ---------------------------------------------------------------------------
-- PRODUCTS   (FK → PRODUCT_CATEGORIES)
-- ---------------------------------------------------------------------------
CREATE TABLE SALESAPP.PRODUCTS (
    PRODUCT_ID     NUMBER          NOT NULL,
    PRODUCT_CODE   VARCHAR2(50)    NOT NULL,
    PRODUCT_NAME   VARCHAR2(200)   NOT NULL,
    DESCRIPTION    VARCHAR2(500),
    CATEGORY_ID    NUMBER,
    UNIT_PRICE     NUMBER(10,2)    NOT NULL,
    STOCK_QUANTITY NUMBER          DEFAULT 0,
    IS_ACTIVE      CHAR(1)         DEFAULT '1',
    CREATED_AT     TIMESTAMP(6)    DEFAULT CURRENT_TIMESTAMP,
    UPDATED_AT     TIMESTAMP(6),
    CONSTRAINT PK_PRODUCTS          PRIMARY KEY (PRODUCT_ID),
    CONSTRAINT UQ_PRODUCTS_CODE     UNIQUE (PRODUCT_CODE),
    CONSTRAINT FK_PRODUCTS_CATEGORY FOREIGN KEY (CATEGORY_ID)
        REFERENCES SALESAPP.PRODUCT_CATEGORIES (CATEGORY_ID)
);
