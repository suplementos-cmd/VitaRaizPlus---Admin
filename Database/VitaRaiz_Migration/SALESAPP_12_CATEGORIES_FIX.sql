-- ===========================================================================
-- VITARAIZ - CATEGORÍAS DE PRODUCTOS
-- Script para configurar categorías según módulos de inventario
-- Fecha: 2026-04-08
-- ===========================================================================

-- Limpiar categorías existentes (si es necesario)
-- DELETE FROM SALESAPP.PRODUCT_CATEGORIES WHERE CATEGORY_ID <= 10;

-- Insertar/Actualizar categorías para el sistema de inventario
MERGE INTO SALESAPP.PRODUCT_CATEGORIES pc
USING (
  SELECT 1 AS CATEGORY_ID, 'Suplementos' AS CATEGORY_NAME, 'Suplementos alimenticios y nutricionales' AS DESCRIPTION FROM DUAL UNION ALL
  SELECT 2, 'Papelería', 'Material de papelería y documentación' FROM DUAL UNION ALL
  SELECT 3, 'Uniformes', 'Uniformes y prendas de vestir' FROM DUAL UNION ALL
  SELECT 4, 'Muestras', 'Muestras de productos y promocionales' FROM DUAL UNION ALL
  SELECT 5, 'Kits de Ventas', 'Kits y paquetes para ventas' FROM DUAL UNION ALL
  SELECT 6, 'Kits de Cobros', 'Kits y materiales para gestión de cobros' FROM DUAL
) src
ON (pc.CATEGORY_ID = src.CATEGORY_ID)
WHEN MATCHED THEN
  UPDATE SET 
    pc.CATEGORY_NAME = src.CATEGORY_NAME,
    pc.DESCRIPTION = src.DESCRIPTION
WHEN NOT MATCHED THEN
  INSERT (CATEGORY_ID, CATEGORY_NAME, DESCRIPTION, CREATED_AT)
  VALUES (src.CATEGORY_ID, src.CATEGORY_NAME, src.DESCRIPTION, SYSTIMESTAMP);

COMMIT;

-- Verificar categorías insertadas
SELECT CATEGORY_ID, CATEGORY_NAME, DESCRIPTION
FROM SALESAPP.PRODUCT_CATEGORIES
ORDER BY CATEGORY_ID;

-- Resultado esperado:
-- CATEGORY_ID  CATEGORY_NAME      DESCRIPTION
-- -----------  -----------------  ----------------------------------------
-- 1            Suplementos        Suplementos alimenticios y nutricionales
-- 2            Papelería          Material de papelería y documentación
-- 3            Uniformes          Uniformes y prendas de vestir
-- 4            Muestras           Muestras de productos y promocionales
-- 5            Kits de Ventas     Kits y paquetes para ventas
-- 6            Kits de Cobros     Kits y materiales para gestión de cobros
