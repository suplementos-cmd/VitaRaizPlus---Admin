-- Script simple para agregar EXPIRATION_DATE
-- Ejecutar desde SQL*Plus o PL/SQL Developer

-- Agregar columna
ALTER TABLE SALESAPP.EMPLOYEES_DOCUMENTS ADD EXPIRATION_DATE DATE;

-- Verificar
SELECT column_name, data_type, nullable
FROM user_tab_columns
WHERE table_name = 'EMPLOYEES_DOCUMENTS'
ORDER BY column_id;

COMMIT;
