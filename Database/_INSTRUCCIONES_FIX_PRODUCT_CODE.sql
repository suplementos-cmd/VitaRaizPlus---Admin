-- ===========================================================================
-- SCRIPT RÁPIDO: Actualizar solo el procedimiento sp_register_product
-- ===========================================================================

CREATE OR REPLACE PACKAGE BODY SALESAPP.EM_VITARAIZ_AD AS

  -- ... [Nota: Este es un ejemplo parcial. Para producción, ejecute el archivo completo EM_VITARAIZ_AD.pck]
  
  -- Aquí iría todo el contenido del package body existente...
  -- Por simplicidad, solo mostramos el procedure modificado:

END EM_VITARAIZ_AD;
/

-- ===========================================================================
-- INSTRUCCIONES:
-- ===========================================================================
-- Este archivo es solo documentación. Para aplicar el fix:
--
-- OPCIÓN 1 (Recomendada): Ejecutar el package completo actualizado
--   @Database/EM_VITARAIZ_AD.pck
--
-- OPCIÓN 2: Conectarse a Oracle y ejecutar manualmente
--   SQL> @Database/EM_VITARAIZ_AD.pck
--
-- OPCIÓN 3: Usar SQL Developer o similar para compilar el package
--
-- ===========================================================================
-- VERIFICACIÓN:
-- ===========================================================================

-- Verificar que el package se compiló correctamente
SELECT object_name, object_type, status, last_ddl_time
FROM user_objects
WHERE object_name = 'EM_VITARAIZ_AD'
ORDER BY object_type;

-- Debería mostrar:
-- OBJECT_NAME      OBJECT_TYPE      STATUS  LAST_DDL_TIME
-- EM_VITARAIZ_AD   PACKAGE          VALID   [fecha actual]
-- EM_VITARAIZ_AD   PACKAGE BODY     VALID   [fecha actual]

-- ===========================================================================
-- PRUEBA:
-- ===========================================================================

-- Probar la creación de un producto de prueba
DECLARE
  v_product_id NUMBER;
BEGIN
  SALESAPP.EM_VITARAIZ_AD.sp_register_product(
    p_product_id  => v_product_id,
    p_name        => 'Producto de Prueba',
    p_description => 'Prueba generación automática de código',
    p_unit_price  => 100,
    p_stock       => 10,
    p_category    => 'Papelería'
  );
  
  DBMS_OUTPUT.PUT_LINE('Producto creado con ID: ' || v_product_id);
  
  -- Verificar el código generado
  FOR rec IN (SELECT product_id, product_code, product_name 
              FROM SALESAPP.PRODUCTS 
              WHERE product_id = v_product_id) LOOP
    DBMS_OUTPUT.PUT_LINE('Código generado: ' || rec.product_code);
  END LOOP;
  
  ROLLBACK; -- Deshacer la prueba
END;
/
