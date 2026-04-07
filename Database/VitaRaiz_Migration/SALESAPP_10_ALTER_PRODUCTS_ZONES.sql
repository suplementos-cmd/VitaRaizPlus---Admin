-- ===========================================================================
-- VITARAIZ - MIGRACIÓN ESQUEMA SALESAPP
-- Parte 10: ALTER TABLE para nuevas columnas en PRODUCTS y ZONES
-- Ejecutar solo si la base de datos ya existe
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- PRODUCTS: agregar CATEGORY (texto directo) y PHOTO_URL
-- ---------------------------------------------------------------------------
ALTER TABLE SALESAPP.PRODUCTS ADD (
    CATEGORY  VARCHAR2(100),
    PHOTO_URL VARCHAR2(500)
);

COMMENT ON COLUMN SALESAPP.PRODUCTS.CATEGORY IS 'Categoría del producto (texto libre)';
COMMENT ON COLUMN SALESAPP.PRODUCTS.PHOTO_URL IS 'Ruta relativa de la foto del producto (ej: /uploads/products/100.jpg)';

-- ---------------------------------------------------------------------------
-- ZONES: NO requiere ALTER TABLE — ZONE_CODE e IS_ACTIVE ya existen
--    CREATE TABLE SALESAPP.ZONES incluye ZONE_CODE VARCHAR2(10) y IS_ACTIVE CHAR(1)
-- ---------------------------------------------------------------------------
