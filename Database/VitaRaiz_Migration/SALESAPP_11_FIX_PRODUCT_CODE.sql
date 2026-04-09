-- ===========================================================================
-- VITARAIZ - MIGRACIÓN ESQUEMA SALESAPP
-- Parte 11: FIX PRODUCT_CODE - Generación automática
-- Generado: 2026-04-08
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- Este script actualiza el package EM_VITARAIZ_AD para generar 
-- automáticamente el campo PRODUCT_CODE durante la inserción de productos
-- ---------------------------------------------------------------------------

-- PASO 1: Re-compilar el package body con la corrección
CREATE OR REPLACE PACKAGE BODY SALESAPP.EM_VITARAIZ_AD AS

  -- [... incluir aquí todo el package body actualizado ...]
  -- O ejecutar directamente el archivo EM_VITARAIZ_AD.pck
  
  PROCEDURE sp_register_product(p_product_id  OUT NUMBER,
                                p_name        IN VARCHAR2,
                                p_description IN VARCHAR2 DEFAULT NULL,
                                p_unit_price  IN NUMBER,
                                p_stock       IN NUMBER,
                                p_category    IN VARCHAR2 DEFAULT NULL,
                                p_photo_url   IN VARCHAR2 DEFAULT NULL) IS
    v_product_id NUMBER;
    v_product_code VARCHAR2(50);
  BEGIN
    -- Obtener siguiente ID de secuencia
    SELECT seq_products.NEXTVAL INTO v_product_id FROM DUAL;
    
    -- Generar código de producto automático: PROD-000001, PROD-000002, etc.
    v_product_code := 'PROD-' || LPAD(v_product_id, 6, '0');
    
    -- Insertar producto con código generado
    INSERT INTO products
      (product_id,
       product_code,
       product_name,
       description,
       unit_price,
       stock_quantity,
       is_active,
       category,
       photo_url,
       created_at)
    VALUES
      (v_product_id,
       v_product_code,
       p_name,
       p_description,
       p_unit_price,
       NVL(p_stock, 0),
       1,
       p_category,
       p_photo_url,
       SYSTIMESTAMP);
    
    -- Devolver ID generado
    p_product_id := v_product_id;
    
    log_audit('PRODUCT',
              p_product_id,
              'INSERT',
              NULL,
              'Producto: ' || p_name || ' [' || v_product_code || ']');
    COMMIT;
    
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_product;

  -- [... resto del package body ...]

END EM_VITARAIZ_AD;
/

-- ---------------------------------------------------------------------------
-- INSTRUCCIONES DE EJECUCIÓN:
-- ---------------------------------------------------------------------------
-- 1. Ejecutar el archivo completo EM_VITARAIZ_AD.pck que ya contiene la corrección
-- 2. O ejecutar solo esta parte modificada del procedure sp_register_product
-- 
-- COMANDO RECOMENDADO:
-- @EM_VITARAIZ_AD.pck
-- ---------------------------------------------------------------------------

-- Verificación
SELECT object_name, object_type, status
FROM user_objects
WHERE object_name = 'EM_VITARAIZ_AD';

-- Nota: El procedimiento ahora genera automáticamente el PRODUCT_CODE
-- con el formato: PROD-000001, PROD-000002, etc.
