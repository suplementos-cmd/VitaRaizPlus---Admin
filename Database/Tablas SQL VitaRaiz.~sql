--Tablas sistema VitaRaiz    user pas  Base datos : salesapp/SalesApp2026@XEPDB1 
SELECT * FROM all_objects_ae a where a.owner ='SALESAPP' and A.object_type='SEQUENCE'; 

--EM_VITARAIZ_AD;
SELECT * FROM payments;
SELECT * FROM v_collector_customers_detail;
SELECT * FROM ROLES                   ;
SELECT * FROM PROFILE_THEMES           ;
SELECT * FROM ZONES                    ;
SELECT * FROM USERS  ;--      FOR UPDATE            ; 
SELECT * FROM PERMISSIONS              ;
SELECT * FROM ROLE_PERMISSIONS         ;
SELECT * FROM USER_SESSIONS            ;
SELECT * FROM CUSTOMERS                ;
SELECT * FROM CUSTOMER_INCIDENTS       ;
SELECT * FROM BLACKLIST_PROPOSALS      ;
SELECT * FROM PRODUCT_CATEGORIES       ;
SELECT * FROM PRODUCTS                 ;
SELECT * FROM SALES                    ;
SELECT * FROM SALE_ITEMS               ;
SELECT * FROM SALE_PHOTOS              ;
SELECT * FROM SALE_STATUS_HISTORY      ;
SELECT * FROM PAYMENTS                 ;
SELECT * FROM PAYMENT_PHOTOS           ;
SELECT * FROM COLLECTOR_VISIT_ACTIONS  ;
SELECT * FROM COLLECTOR_ROUTES         ;
SELECT * FROM SYNC_QUEUE               ;
SELECT * FROM SYNC_CONFLICTS           ;

SELECT * FROM USER_THEME_PREFERENCES   ;
SELECT * FROM WHATSAPP_MESSAGES        ;
SELECT * FROM WHATSAPP_TEMPLATES       ;
SELECT * FROM AUDIT_LOGS               ; 

SELECT * FROM CATALOG_SALE_STATUSES  ;
SELECT * FROM CATALOG_PAYMENT_STATUSES;-- fOR UPDATE;
SELECT * FROM CATALOG_VISIT_ACTIONS  ;  
select * from PAYMENTS;
SELECT * FROM CATALOG_RISK_STATUSES  ;
SELECT * FROM CATALOG_APP_THEMES  ;
SELECT * FROM CATALOG_NOTIFICATION_TEMPLATES ;
SELECT * FROM CATALOG_APP_SETTINGS  ;


-- Cambiar color del estado "EN_PROCESO" de verde a azul
UPDATE CATALOG_SALE_STATUSES 
SET COLOR_HEX = '#007BFF' 
WHERE STATUS_CODE = 'EN_PROCESO';
COMMIT;


-- Cambiar color del estado "EN_PROCESO" de verde a azul
UPDATE CATALOG_APP_THEMES  
SET PRIMARY_COLOR = '#007BFF' 
WHERE THEME_CODE = 'VITARAIZ_DEFAULT';
COMMIT;

-- La app móvil verá el cambio en la próxima sincronización
  SELECT COUNT(*) FROM CUSTOMERS;
SELECT COUNT(*) FROM SALES;
SELECT COUNT(*) FROM PAYMENTS;
SELECT COUNT(*) FROM PRODUCTS;

--
DECLARE
--
    p_table_name    VARCHAR2(20) :='WHATSAPP_TEMPLATES';
    p_column_name   VARCHAR2(20) :='TEMPLATE_BODY';
    v_sql   CLOB;
BEGIN
    v_sql := '
        UPDATE ' || p_table_name || '
        SET ' || p_column_name || ' =
            CONVERT(
                UTL_RAW.CAST_TO_VARCHAR2(
                    UTL_RAW.CAST_TO_RAW(' || p_column_name || ')
                ),
                ''WE8MSWIN1252'',
                ''AL32UTF8''
            )
        WHERE ' || p_column_name || ' IS NOT NULL';

    EXECUTE IMMEDIATE v_sql;

    COMMIT;
END;
/
     --Pendiente Editar cobro: ok 80% 
     --Estatus se vea reflejado en venta
     --Importe cuadre  
     --Al compartir venta * Whatsap envié dealle de venta Coodenada, imagen, nombre, producto zona.    
     --empleado retardos, días trabajados , antiguedad
     
     
