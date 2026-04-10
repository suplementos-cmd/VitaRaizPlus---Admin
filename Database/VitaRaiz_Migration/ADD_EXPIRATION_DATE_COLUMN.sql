-- ========================================
-- Script para agregar columna EXPIRATION_DATE
-- a la tabla EMPLOYEES_DOCUMENTS
-- ========================================

PROMPT Agregando columna EXPIRATION_DATE a tabla EMPLOYEES_DOCUMENTS...

-- Verificar si la columna ya existe
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*)
    INTO v_count
    FROM user_tab_columns
    WHERE table_name = 'EMPLOYEES_DOCUMENTS'
      AND column_name = 'EXPIRATION_DATE';
    
    IF v_count = 0 THEN
        -- La columna no existe, agregarla
        EXECUTE IMMEDIATE 'ALTER TABLE SALESAPP.EMPLOYEES_DOCUMENTS ADD (EXPIRATION_DATE DATE)';
        DBMS_OUTPUT.PUT_LINE('Columna EXPIRATION_DATE agregada exitosamente.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('La columna EXPIRATION_DATE ya existe.');
    END IF;
END;
/

PROMPT Columna agregada/verificada exitosamente.
COMMIT;
