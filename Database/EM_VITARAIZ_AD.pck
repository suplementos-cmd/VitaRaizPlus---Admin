CREATE OR REPLACE PACKAGE EM_VITARAIZ_AD AS
  -- ========================================================================
  -- CONSTANTES Y TIPOS
  -- ========================================================================
  -- Estados de venta
  C_STATUS_POR_INICIAR CONSTANT VARCHAR2(20) := 'POR_INICIAR';
  C_STATUS_EN_PROCESO  CONSTANT VARCHAR2(20) := 'EN_PROCESO';
  C_STATUS_LIQUIDADO   CONSTANT VARCHAR2(20) := 'LIQUIDADO';
  C_STATUS_ANULADO     CONSTANT VARCHAR2(20) := 'ANULADO';
  C_STATUS_CANCELADO   CONSTANT VARCHAR2(20) := 'CANCELADO';

  -- Estados de riesgo
  C_RISK_VERDE    CONSTANT VARCHAR2(10) := 'VERDE';
  C_RISK_AMARILLO CONSTANT VARCHAR2(10) := 'AMARILLO';
  C_RISK_ROJO     CONSTANT VARCHAR2(10) := 'ROJO';
  C_RISK_CRITICO  CONSTANT VARCHAR2(10) := 'CRITICO';

  -- ========================================================================
  -- IDs NUMÉRICOS DE ESTADO DE PAGO (PAYMENTS.STATUS = CATALOG_PAYMENT_STATUSES.STATUS_ID)
  -- Usar siempre estas constantes en vez de los textos de STATUS_KEY
  -- ========================================================================
  C_PAY_PENDING   CONSTANT NUMBER := 1;  -- PENDING
  C_PAY_CONFIRMED CONSTANT NUMBER := 2;  -- CONFIRMADO
  C_PAY_REJECTED  CONSTANT NUMBER := 3;  -- RECHAZADO
  C_PAY_CANCELLED CONSTANT NUMBER := 4;  -- CANCELLED
  -- IDs de acción de visita del cobrador (PAYMENTS.COLLECTION_ACTION_ID)
  C_ACT_PAGO_RECIBIDO CONSTANT NUMBER := 10;  -- PAGO_RECIBIDO
  C_ACT_NO_ENCONTRADO CONSTANT NUMBER := 20;  -- CLIENTE_NO_ENCONTRADO
  C_ACT_PROMESA_PAGO  CONSTANT NUMBER := 30;  -- PROMESA_PAGO
  C_ACT_REHUSA        CONSTANT NUMBER := 40;  -- CLIENTE_REHUSA

  -- Tipo para resultado de operaciones
  TYPE t_result_record IS RECORD(
    success BOOLEAN,
    message VARCHAR2(500),
    id      NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE GESTION DE PAGOS
  -- ========================================================================
  PROCEDURE sp_register_payment(p_payment_id   OUT NUMBER,
                                p_sale_id      IN NUMBER,
                                p_collector_id IN NUMBER,
                                p_amount       IN NUMBER,
                                p_gps_lat      IN NUMBER,
                                p_gps_lon      IN NUMBER,
                                p_device_id    IN VARCHAR2,
                                p_photo_path   IN VARCHAR2 DEFAULT NULL,
                                p_notes        IN VARCHAR2 DEFAULT NULL,
                                p_action_id    IN NUMBER   DEFAULT NULL,
                                p_sub_id       IN NUMBER   DEFAULT NULL);

  /**
  * Aprueba un pago registrado
  */
  PROCEDURE sp_approve_payment(p_payment_id  IN NUMBER,
                               p_approved_by IN NUMBER);

  /**
  * Rechaza un pago registrado
  */
  PROCEDURE sp_reject_payment(p_payment_id  IN NUMBER,
                              p_rejected_by IN NUMBER,
                              p_reason      IN VARCHAR2);

  /**
  * Anula un pago registrado
  */
  PROCEDURE sp_cancel_payment(p_payment_id IN NUMBER,
                              p_user_id    IN NUMBER,
                              p_reason     IN VARCHAR2);

  /**
  * Genera liquidacion de una venta
  */
  PROCEDURE sp_liquidate_sale(p_sale_id IN NUMBER, p_user_id IN NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE GESTION DE VENTAS
  -- ========================================================================
  
  /**
  * Registra una nueva venta
  */
  PROCEDURE sp_register_sale(p_sale_id               OUT NUMBER,
                             p_customer_id           IN NUMBER,
                             p_seller_id             IN NUMBER,
                             p_payment_term_days     IN NUMBER,
                             p_payment_term          IN VARCHAR2 DEFAULT 'SEMANAL',
                             p_collection_day        IN VARCHAR2 DEFAULT NULL,
                             p_first_collection_date IN DATE DEFAULT NULL,
                             p_down_payment          IN NUMBER DEFAULT 0,
                             p_notes                 IN VARCHAR2 DEFAULT NULL);

  /**
  * Agrega un detalle a una venta
  */
  PROCEDURE sp_add_sale_detail(p_sale_id    IN NUMBER,
                               p_product_id IN NUMBER,
                               p_quantity   IN NUMBER,
                               p_unit_price IN NUMBER);

  /**
  * Agrega una foto a una venta
  */
  PROCEDURE sp_add_sale_photo(p_photo_id     OUT NUMBER,
                              p_sale_id      IN NUMBER,
                              p_photo_type   IN VARCHAR2,
                              p_file_path    IN VARCHAR2,
                              p_gps_lat      IN NUMBER DEFAULT NULL,
                              p_gps_lon      IN NUMBER DEFAULT NULL,
                              p_file_size    IN NUMBER DEFAULT NULL,
                              p_uploaded_by  IN NUMBER DEFAULT NULL);

  /**
  * Obtiene las fotos de una venta
  */
  PROCEDURE sp_get_sale_photos(p_sale_id IN NUMBER,
                               p_cursor  OUT SYS_REFCURSOR);

  /**
  * Elimina una foto de venta
  */
  PROCEDURE sp_delete_sale_photo(p_photo_id IN NUMBER);

  /**
  * Anula una venta 
  */
  PROCEDURE sp_cancel_sale(p_sale_id IN NUMBER,
                           p_user_id IN NUMBER DEFAULT NULL,
                           p_reason  IN VARCHAR2 DEFAULT NULL);

  /**
  * Reasigna un cobrador a una venta 
  */
  PROCEDURE sp_reassign_collector(p_sale_id          IN NUMBER,
                                  p_new_collector_id IN NUMBER,
                                  p_user_id          IN NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE GESTION DE CLIENTES
  -- ========================================================================

  /**
  * Registra un nuevo cliente
  */
  PROCEDURE sp_register_customer(p_customer_id    OUT NUMBER,
                                 p_name           IN VARCHAR2,
                                 p_phone          IN VARCHAR2 DEFAULT NULL,
                                 p_email          IN VARCHAR2 DEFAULT NULL,
                                 p_address        IN VARCHAR2 DEFAULT NULL,
                                 p_zone_id        IN NUMBER DEFAULT NULL,
                                 p_gps_lat        IN VARCHAR2 DEFAULT NULL,
                                 p_gps_lon        IN VARCHAR2 DEFAULT NULL,
                                 p_is_gold        IN NUMBER DEFAULT 0,
                                 p_is_blacklisted IN NUMBER DEFAULT 0,
                                 p_created_by     IN NUMBER DEFAULT NULL);

  /**
  * Actualiza un cliente existente
  */
  PROCEDURE sp_update_customer(p_customer_id    IN NUMBER,
                               p_name           IN VARCHAR2,
                               p_phone          IN VARCHAR2 DEFAULT NULL,
                               p_email          IN VARCHAR2 DEFAULT NULL,
                               p_address        IN VARCHAR2 DEFAULT NULL,
                               p_zone_id        IN NUMBER DEFAULT NULL,
                               p_gps_lat        IN VARCHAR2 DEFAULT NULL,
                               p_gps_lon        IN VARCHAR2 DEFAULT NULL,
                               p_is_gold        IN NUMBER DEFAULT 0,
                               p_is_blacklisted IN NUMBER DEFAULT 0);

  /**
  * Elimina un cliente (desactivación lógica)
  */
  PROCEDURE sp_delete_customer(p_customer_id IN NUMBER);

  /**
  * Propone un cliente para lista negra 
  */
  PROCEDURE sp_propose_blacklist(p_customer_id IN NUMBER,
                                 p_proposed_by IN NUMBER,
                                 p_reason      IN VARCHAR2);

  /**
  * Aprueba o rechaza propuesta de lista negra 
  */
  PROCEDURE sp_approve_blacklist(p_proposal_id IN NUMBER,
                                 p_approved_by IN NUMBER,
                                 p_is_approved IN CHAR);

  /**
  * Marca un cliente como "buenos cliente" (gold) 
  */
  PROCEDURE sp_mark_gold_customer(p_customer_id IN NUMBER,
                                  p_user_id     IN NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE GESTION DE PRODUCTOS
  -- ========================================================================

  /**
  * Registra un nuevo producto
  */
  PROCEDURE sp_register_product(p_product_id  OUT NUMBER,
                                p_name        IN VARCHAR2,
                                p_description IN VARCHAR2 DEFAULT NULL,
                                p_unit_price  IN NUMBER,
                                p_stock       IN NUMBER,
                                p_category_id IN NUMBER DEFAULT NULL,
                                p_photo_url   IN VARCHAR2 DEFAULT NULL);

  /**
  * Actualiza un producto existente
  */
  PROCEDURE sp_update_product(p_product_id  IN NUMBER,
                              p_name        IN VARCHAR2,
                              p_description IN VARCHAR2 DEFAULT NULL,
                              p_unit_price  IN NUMBER,
                              p_stock       IN NUMBER,
                              p_is_active   IN NUMBER DEFAULT 1,
                              p_category_id IN NUMBER DEFAULT NULL,
                              p_photo_url   IN VARCHAR2 DEFAULT NULL);

  /**
  * Actualiza solo la foto del producto
  */
  PROCEDURE sp_update_product_photo(p_product_id IN NUMBER,
                                    p_photo_url  IN VARCHAR2);

  /**
  * Elimina un producto (desactivación lógica)
  */
  PROCEDURE sp_delete_product(p_product_id IN NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE GESTION DE ZONAS
  -- ========================================================================

  /**
  * Registra una nueva zona
  */
  PROCEDURE sp_register_zone(p_zone_id     OUT NUMBER,
                             p_name        IN VARCHAR2,
                             p_zone_code   IN VARCHAR2 DEFAULT NULL,
                             p_description IN VARCHAR2 DEFAULT NULL,
                             p_is_active   IN NUMBER DEFAULT 1);

  /**
  * Actualiza una zona existente
  */
  PROCEDURE sp_update_zone(p_zone_id     IN NUMBER,
                           p_name        IN VARCHAR2,
                           p_zone_code   IN VARCHAR2 DEFAULT NULL,
                           p_description IN VARCHAR2 DEFAULT NULL,
                           p_is_active   IN NUMBER DEFAULT 1);

  /**
  * Elimina una zona
  */
  PROCEDURE sp_delete_zone(p_zone_id IN NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE AUTENTICACION Y USUARIOS
  -- ========================================================================

  /**
  * Valida credenciales de usuario y registra login
  * @return user_id si es exitoso, NULL si falla
  */
  FUNCTION fn_authenticate_user(p_username IN VARCHAR2,
                                p_password IN VARCHAR2) RETURN NUMBER;
  /**
  * Obtiene información del usuario para generar token de autenticación 
  */
  PROCEDURE sp_get_user_info(p_user_id   IN NUMBER,
                             p_username  OUT VARCHAR2,
                             p_role_name OUT VARCHAR2,
                             p_role_id   OUT NUMBER);

  /**
  * Registra último login del usuario
  */
  PROCEDURE sp_update_last_login(p_user_id IN NUMBER);

  /**
  * Obtiene usuarios con filtros opcionales
  */
  PROCEDURE sp_get_users(p_search_term IN VARCHAR2 DEFAULT NULL,
                         p_role_id     IN NUMBER DEFAULT NULL,
                         p_is_active   IN NUMBER DEFAULT NULL,
                         p_cursor      OUT SYS_REFCURSOR);

  /**
  * Obtiene un usuario por ID
  */
  PROCEDURE sp_get_user_by_id(p_user_id IN NUMBER,
                              p_cursor  OUT SYS_REFCURSOR);

  /**
  * Registra un nuevo usuario
  */
  PROCEDURE sp_register_user(p_user_id       OUT NUMBER,
                             p_username      IN VARCHAR2,
                             p_full_name     IN VARCHAR2,
                             p_email         IN VARCHAR2 DEFAULT NULL,
                             p_password      IN VARCHAR2,
                             p_role_id       IN NUMBER,
                             p_zone_id       IN NUMBER DEFAULT NULL,
                             p_is_active     IN NUMBER DEFAULT 1,
                             p_created_by    IN NUMBER DEFAULT NULL);

  /**
  * Actualiza un usuario existente
  */
  PROCEDURE sp_update_user(p_user_id    IN NUMBER,
                           p_full_name  IN VARCHAR2,
                           p_email      IN VARCHAR2 DEFAULT NULL,
                           p_role_id    IN NUMBER,
                           p_zone_id    IN NUMBER DEFAULT NULL,
                           p_is_active  IN NUMBER DEFAULT 1,
                           p_updated_by IN NUMBER DEFAULT NULL);

  /**
  * Elimina (desactiva) un usuario
  */
  PROCEDURE sp_delete_user(p_user_id    IN NUMBER,
                           p_deleted_by IN NUMBER DEFAULT NULL);

  /**
  * Cambia contraseña validando la actual
  * @return '1' si se actualizó, '0' si contraseña actual incorrecta o usuario no existe
  */
  FUNCTION fn_change_user_password(p_user_id          IN NUMBER,
                                   p_current_password IN VARCHAR2,
                                   p_new_password     IN VARCHAR2) RETURN CHAR;

  /**
  * Obtiene todos los roles
  */
  PROCEDURE sp_get_roles(p_cursor OUT SYS_REFCURSOR);

  /**
  * Actualiza datos de rol
  */
  PROCEDURE sp_update_role(p_role_id             IN NUMBER,
                           p_role_name           IN VARCHAR2,
                           p_description         IN VARCHAR2 DEFAULT NULL,
                           p_default_theme_color IN VARCHAR2 DEFAULT NULL,
                           p_updated_by          IN NUMBER DEFAULT NULL);

  /**
  * Obtiene permisos por rol
  */
  PROCEDURE sp_get_role_permissions(p_role_id IN NUMBER,
                                    p_cursor  OUT SYS_REFCURSOR);

  /**
  * Obtiene catálogo completo de permisos
  */
  PROCEDURE sp_get_permissions(p_cursor OUT SYS_REFCURSOR);

  /**
  * Reemplaza permisos del rol usando lista CSV de permission_name
  */
  PROCEDURE sp_set_role_permissions(p_role_id         IN NUMBER,
                                    p_permissions_csv IN CLOB,
                                    p_updated_by      IN NUMBER DEFAULT NULL);

  -- ========================================================================
  -- PROCEDIMIENTOS DE RUTAS Y COBRANZA
  -- ========================================================================

  /**
  * Registra accion de visita del cobrador 
  */
  PROCEDURE sp_register_visit_action(p_sale_id      IN NUMBER,
                                     p_collector_id IN NUMBER,
                                     p_action_type  IN VARCHAR2,
                                     p_gps_lat      IN NUMBER,
                                     p_gps_lon      IN NUMBER,
                                     p_notes        IN VARCHAR2 DEFAULT NULL);

  /**
  * Genera ruta diaria optimizada para cobrador 
  */
  PROCEDURE sp_generate_daily_route(p_collector_id IN NUMBER,
                                    p_route_date   IN DATE DEFAULT SYSDATE);

  /**
  * Cierra ruta diaria y registra totales 
  */
  PROCEDURE sp_close_daily_route(p_route_id        IN NUMBER,
                                 p_total_collected IN NUMBER);

  -- ========================================================================
  -- PROCEDIMIENTOS DE SINCRONIZACION OFFLINE
  -- ========================================================================

  /**
  * Procesa cola de sincronizacion pendiente 
  */
  PROCEDURE sp_process_sync_queue(p_device_id IN VARCHAR2,
                                  p_user_id   IN NUMBER);

  /**
  * Resuelve conflicto de sincronizacion 
  */
  PROCEDURE sp_resolve_sync_conflict(p_conflict_id IN NUMBER,
                                     p_resolution  IN VARCHAR2,
                                     p_resolved_by IN NUMBER);

  -- ========================================================================
  -- FUNCIONES DE CÁLCULO
  -- ========================================================================

  /**
  * Calcula saldo pendiente de una venta 
  */
  FUNCTION fn_get_sale_balance(p_sale_id IN NUMBER) RETURN NUMBER;

  /**
  * Obtiene estado de riesgo de una venta 
  * @return VERDE, AMARILLO, ROJO, CRITICO
  */
  FUNCTION fn_get_risk_status(p_sale_id IN NUMBER) RETURN VARCHAR2;

  /**
  * Calcula dias desde Ultimo pago 
  */
  FUNCTION fn_days_since_payment(p_sale_id IN NUMBER) RETURN NUMBER;

  /**
  * Calcula porcentaje pagado de una venta 
  * @return Porcentaje (0-100)
  */
  FUNCTION fn_payment_percentage(p_sale_id IN NUMBER) RETURN NUMBER;

  /**
  * Calcula total cobrado por cobrador en un periodo 
  * @return Total cobrado
  */
  FUNCTION fn_collector_total(p_collector_id IN NUMBER,
                              p_from_date    IN DATE,
                              p_to_date      IN DATE) RETURN NUMBER;

  /**
  * Calcula distancia entre dos puntos GPS (en km) 
  * @return Distancia en kilometros
  */
  FUNCTION fn_gps_distance(p_lat1 IN NUMBER,
                           p_lon1 IN NUMBER,
                           p_lat2 IN NUMBER,
                           p_lon2 IN NUMBER) RETURN NUMBER;

  /**
  * Verifica si cliente esta� en lista negra 
  */
  FUNCTION fn_is_blacklisted(p_customer_id IN NUMBER) RETURN CHAR;

  /**
  * Verifica si usuario tiene permiso especifico 
  * @return '1' si tiene, '0' si no
  */
  FUNCTION fn_has_permission(p_user_id         IN NUMBER,
                             p_permission_code IN VARCHAR2) RETURN CHAR;

  /**
  * Genera numero de folio consecutivo 
  */
  FUNCTION fn_generate_folio(p_type IN VARCHAR2) RETURN VARCHAR2;

  -- ========================================================================
  -- FUNCIONES DE VALIDACION
  -- ========================================================================

  /**
  * Valida coordenadas GPS 
  */
  FUNCTION fn_validate_gps(p_lat IN NUMBER, p_lon IN NUMBER) RETURN CHAR;

  /**
  * Valida que una venta pueda recibir pagos 
  * @return '1' si puede, '0' si no
  */
  FUNCTION fn_can_receive_payment(p_sale_id IN NUMBER) RETURN CHAR;

  /**
  * Valida monto de pago 
  * @return '1' si valido, '0' si no
  */
  FUNCTION fn_validate_payment_amount(p_sale_id IN NUMBER,
                                      p_amount  IN NUMBER) RETURN CHAR;

  -- ========================================================================
  -- PROCEDIMIENTOS DE REPORTES Y ESTADISTICAS
  -- ========================================================================

  /**
  * Genera estadisticas diarias de cobranza 
  */
  PROCEDURE sp_daily_collection_stats(p_date    IN DATE DEFAULT SYSDATE,
                                      p_zone_id IN NUMBER DEFAULT NULL);

  /**
  * Envia recordatorio de pago por WhatsApp 
  */
  PROCEDURE sp_send_whatsapp_reminder(p_sale_id       IN NUMBER,
                                      p_template_name IN VARCHAR2 DEFAULT 'PAYMENT_REMINDER');

  -- ========================================================================
  -- PROCEDIMIENTOS DE CATALOGOS 
  -- ========================================================================

  /**
  * Obtiene todos los estados de venta activos 
  */
  PROCEDURE sp_get_sale_statuses(p_cursor OUT SYS_REFCURSOR);

  /**
  * Obtiene todos los estados de pago activos 
  */
  PROCEDURE sp_get_payment_statuses(p_cursor OUT SYS_REFCURSOR);

  /**
  * Obtiene todos los estados de riesgo activos 
  */
  PROCEDURE sp_get_risk_statuses(p_cursor OUT SYS_REFCURSOR);

  /**
  * Obtiene el tema activo de la aplicación 
  */
  PROCEDURE sp_get_active_theme(p_cursor OUT SYS_REFCURSOR);

  /**
  * Obtiene plantillas de notificaciones activas 
  */
  PROCEDURE sp_get_notification_templates(p_template_type IN VARCHAR2 DEFAULT NULL,
                                          p_cursor        OUT SYS_REFCURSOR);

  /**
  * Obtiene configuraciones de la aplicación 
  */
  PROCEDURE sp_get_app_settings(p_category  IN VARCHAR2 DEFAULT NULL,
                                p_is_public IN NUMBER DEFAULT 1,
                                p_cursor    OUT SYS_REFCURSOR);

  /**
  * Obtiene acciones de visita disponibles 
  */
  PROCEDURE sp_get_visit_actions(p_cursor OUT SYS_REFCURSOR);

  /**
  * Obtiene todas las categorías de productos activas
  */
  PROCEDURE sp_get_product_categories(p_cursor OUT SYS_REFCURSOR);

  /**
  * Obtiene un setting específico por key 
  */
  FUNCTION fn_get_setting_value(p_setting_key IN VARCHAR2) RETURN VARCHAR2;

  /**
  * Obtiene el estado de riesgo dinámico basado en días de atraso 
  */
  FUNCTION fn_get_risk_status_by_days(p_days_overdue IN NUMBER)
    RETURN VARCHAR2;

  /**
  * Obtiene el tema de la aplicación por rol (PROFILE_THEMES)
  * @return Cursor con datos del tema del rol o NULL si no configurado
  */
  PROCEDURE sp_get_theme_by_role(p_role_id IN NUMBER,
                                  p_cursor  OUT SYS_REFCURSOR);

  /**
  * Obtiene todos los permisos (PERMISSION_NAME) asignados al rol del usuario
  * @param p_user_id  ID del usuario autenticado
  * @param p_cursor   Cursor con columnas: permissionName, module, description
  */
  PROCEDURE sp_get_user_permissions(p_user_id IN NUMBER,
                                    p_cursor  OUT SYS_REFCURSOR);

  -- ========================================================================
  -- MÓDULO DE RECURSOS HUMANOS (HR)
  -- ========================================================================

  /**
  * Registra un nuevo empleado
  */
  PROCEDURE sp_register_employee(
    p_employee_id       OUT NUMBER,
    p_user_id           IN NUMBER,
    p_employee_code     IN VARCHAR2,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT 0,
    p_commission_rate   IN NUMBER DEFAULT 0,
    p_hire_date         IN DATE DEFAULT SYSDATE,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_created_by        IN NUMBER DEFAULT NULL
  );

  /**
  * Actualiza datos de un empleado
  */
  PROCEDURE sp_update_employee(
    p_employee_id       IN NUMBER,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT NULL,
    p_commission_rate   IN NUMBER DEFAULT NULL,
    p_employment_status IN VARCHAR2 DEFAULT NULL,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  );

  /**
  * Da de baja a un empleado
  */
  PROCEDURE sp_terminate_employee(
    p_employee_id       IN NUMBER,
    p_termination_date  IN DATE DEFAULT SYSDATE,
    p_reason            IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  );

  /**
  * Obtiene lista de empleados con filtros
  */
  PROCEDURE sp_get_employees(
    p_status           IN VARCHAR2 DEFAULT NULL,
    p_department       IN VARCHAR2 DEFAULT NULL,
    p_search_term      IN VARCHAR2 DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Obtiene un empleado por ID
  */
  PROCEDURE sp_get_employee_by_id(
    p_employee_id      IN NUMBER,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Registra asistencia de empleado
  */
  PROCEDURE sp_register_attendance(
    p_attendance_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_attendance_date  IN DATE DEFAULT SYSDATE,
    p_attendance_type  IN VARCHAR2,
    p_check_in_time    IN TIMESTAMP DEFAULT SYSTIMESTAMP,
    p_check_out_time   IN TIMESTAMP DEFAULT NULL,
    p_gps_lat          IN NUMBER DEFAULT NULL,
    p_gps_lon          IN NUMBER DEFAULT NULL,
    p_notes            IN VARCHAR2 DEFAULT NULL
  );

  /**
  * Obtiene asistencias de un empleado
  */
  PROCEDURE sp_get_attendance_by_employee(
    p_employee_id      IN NUMBER,
    p_start_date       IN DATE DEFAULT NULL,
    p_end_date         IN DATE DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Obtiene resumen de asistencias mensual
  */
  PROCEDURE sp_get_attendance_summary(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT EXTRACT(YEAR FROM SYSDATE),
    p_month            IN NUMBER DEFAULT EXTRACT(MONTH FROM SYSDATE),
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Genera nómina de un empleado
  */
  PROCEDURE sp_generate_payroll(
    p_payroll_id       OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE,
    p_created_by       IN NUMBER DEFAULT NULL
  );

  /**
  * Aprueba una nómina
  */
  PROCEDURE sp_approve_payroll(
    p_payroll_id       IN NUMBER,
    p_approved_by      IN NUMBER
  );

  /**
  * Marca nómina como pagada
  */
  PROCEDURE sp_pay_payroll(
    p_payroll_id       IN NUMBER,
    p_payment_date     IN DATE DEFAULT SYSDATE,
    p_payment_method   IN VARCHAR2,
    p_payment_ref      IN VARCHAR2 DEFAULT NULL,
    p_paid_by          IN NUMBER
  );

  /**
  * Obtiene nóminas de un empleado
  */
  PROCEDURE sp_get_payroll_by_employee(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Registra comisión por venta
  */
  PROCEDURE sp_register_commission_from_sale(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_sale_id          IN NUMBER,
    p_commission_rate  IN NUMBER
  );

  /**
  * Registra comisión por cobro
  */
  PROCEDURE sp_register_commission_from_payment(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_payment_id       IN NUMBER,
    p_commission_rate  IN NUMBER
  );

  /**
  * Obtiene comisiones pendientes
  */
  PROCEDURE sp_get_commissions_pending(
    p_employee_id      IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Calcula años de antigüedad de un empleado
  */
  FUNCTION fn_get_employee_seniority_years(p_employee_id IN NUMBER) RETURN NUMBER;

  /**
  * Calcula total de comisiones en un periodo
  */
  FUNCTION fn_get_employee_total_commissions(
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE
  ) RETURN NUMBER;

  /**
  * Calcula horas trabajadas entre entrada y salida
  */
  FUNCTION fn_calculate_worked_hours(
    p_check_in         IN TIMESTAMP,
    p_check_out        IN TIMESTAMP
  ) RETURN NUMBER;

  -- ========================================================================
  -- GESTIÓN DE DOCUMENTOS DE EMPLEADOS
  -- ========================================================================

  /**
  * Registra un documento de empleado
  */
  PROCEDURE sp_upload_employee_document(
    p_document_id      OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_doc_type_id      IN NUMBER,
    p_file_name        IN VARCHAR2,
    p_file_path        IN VARCHAR2,
    p_file_size        IN NUMBER,
    p_mime_type        IN VARCHAR2 DEFAULT 'application/pdf',
    p_expiration_date  IN DATE DEFAULT NULL,
    p_notes            IN VARCHAR2 DEFAULT NULL,
    p_uploaded_by      IN NUMBER DEFAULT NULL
  );

  /**
  * Obtiene documentos de un empleado
  */
  PROCEDURE sp_get_employee_documents(
    p_employee_id      IN NUMBER,
    p_doc_type_id      IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Obtiene un documento específico por ID
  */
  PROCEDURE sp_get_document_by_id(
    p_document_id      IN NUMBER,
    p_cursor           OUT SYS_REFCURSOR
  );

  /**
  * Elimina un documento de empleado
  */
  PROCEDURE sp_delete_employee_document(
    p_document_id      IN NUMBER,
    p_deleted_by       IN NUMBER DEFAULT NULL
  );

  /**
  * Obtiene tipos de documentos del catálogo
  */
  PROCEDURE sp_get_document_types(
    p_cursor           OUT SYS_REFCURSOR
  );
  --
END EM_VITARAIZ_AD;
/
CREATE OR REPLACE PACKAGE BODY EM_VITARAIZ_AD AS

  -- ========================================================================
  -- PROCEDIMIENTO PRIVADO - Log de auditoria adaptado a schema real
  -- ========================================================================
  PROCEDURE log_audit(p_entity_type IN VARCHAR2,
                      p_entity_id   IN NUMBER,
                      p_action      IN VARCHAR2,
                      p_user_id     IN NUMBER,
                      p_details     IN VARCHAR2 DEFAULT NULL) IS
    PRAGMA AUTONOMOUS_TRANSACTION;
  BEGIN
    --
    INSERT INTO audit_logs
      (log_id,
       user_id,
       table_name,
       record_id,
       action,
       new_values,
       timestamp)
    VALUES
      (seq_audit_logs.NEXTVAL,
       p_user_id,
       p_entity_type,
       p_entity_id,
       p_action,
       p_details,
       SYSTIMESTAMP);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
  END log_audit;
  -- ========================================================================
  -- PROCEDIMIENTOS DE PAGOS
  -- ========================================================================
  PROCEDURE sp_register_payment(p_payment_id   OUT NUMBER,
                                p_sale_id      IN NUMBER,
                                p_collector_id IN NUMBER,
                                p_amount       IN NUMBER,
                                p_gps_lat      IN NUMBER,
                                p_gps_lon      IN NUMBER,
                                p_device_id    IN VARCHAR2,
                                p_photo_path   IN VARCHAR2 DEFAULT NULL,
                                p_notes        IN VARCHAR2 DEFAULT NULL,
                                p_action_id    IN NUMBER   DEFAULT NULL,
                                p_sub_id       IN NUMBER   DEFAULT NULL) IS
    v_total_paid  NUMBER;
    v_sale_amount NUMBER;
    v_new_status  VARCHAR2(20);
  BEGIN
    -- Validaciones
    IF fn_can_receive_payment(p_sale_id) = '0' THEN
      RAISE_APPLICATION_ERROR(-20001, 'La venta no puede recibir pagos');
    END IF;
    --
    IF fn_validate_payment_amount(p_sale_id, p_amount) = '0' THEN
      RAISE_APPLICATION_ERROR(-20002, 'Monto de pago invalido');
    END IF;
    -- Insertar pago
    INSERT INTO payments
      (payment_id,
       sale_id,
       collector_id,
       payment_date,
       amount,
       gps_latitude,
       gps_longitude,
       device_id,
       notes,
       collection_action_id,
       collection_sub_id,
       status)
    VALUES
      (seq_payments.NEXTVAL,
       p_sale_id,
       p_collector_id,
       SYSDATE,
       p_amount,
       p_gps_lat,
       p_gps_lon,
       p_device_id,
       p_notes,
       p_action_id,
       p_sub_id,
       C_PAY_PENDING)
    RETURNING payment_id INTO p_payment_id;
    -- Foto si se proporciona
    IF p_photo_path IS NOT NULL THEN
      INSERT INTO payment_photos
        (photo_id,
         payment_id,
         photo_type,
         file_path,
         gps_latitude,
         gps_longitude)
      VALUES
        (seq_payment_photos.NEXTVAL,
         p_payment_id,
         'RECEIPT',
         p_photo_path,
         p_gps_lat,
         p_gps_lon);
    END IF;
    --
    log_audit('PAYMENT',
              p_payment_id,
              'INSERT',
              p_collector_id,
              'Pago: $' || p_amount);
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_payment;
  --
  PROCEDURE sp_approve_payment(p_payment_id  IN NUMBER,
                               p_approved_by IN NUMBER) IS
    v_sale_id     NUMBER;
    v_amount      NUMBER;
    v_total_paid  NUMBER;
    v_sale_amount NUMBER;
    v_new_status  VARCHAR2(20);
  BEGIN
    --
    SELECT sale_id, amount
      INTO v_sale_id, v_amount
      FROM payments
     WHERE payment_id = p_payment_id;
    --
    UPDATE payments
       SET status     = C_PAY_CONFIRMED,
           updated_at = SYSTIMESTAMP,
           updated_by = p_approved_by
     WHERE payment_id = p_payment_id;
    --
    -- Calcular total pagado
    SELECT SUM(amount)
      INTO v_total_paid
      FROM payments
     WHERE sale_id = v_sale_id
       AND status = C_PAY_CONFIRMED;
    --
    SELECT total_amount
      INTO v_sale_amount
      FROM sales
     WHERE sale_id = v_sale_id;
    --
    -- Actualizar estado de venta
    IF v_total_paid >= v_sale_amount THEN
      v_new_status := C_STATUS_LIQUIDADO;
    ELSE
      v_new_status := C_STATUS_EN_PROCESO;
    END IF;
    --
    UPDATE sales SET status = v_new_status WHERE sale_id = v_sale_id;
    --
    log_audit('PAYMENT',
              p_payment_id,
              'APPROVE',
              p_approved_by,
              'Pago aprobado: $' || v_amount);
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_approve_payment;
  --
  PROCEDURE sp_reject_payment(p_payment_id  IN NUMBER,
                              p_rejected_by IN NUMBER,
                              p_reason      IN VARCHAR2) IS
  BEGIN
    --
    UPDATE payments
       SET status     = C_PAY_REJECTED,
           notes      = COALESCE(notes, '') || ' | RECHAZADO: ' || p_reason,
           updated_at = SYSTIMESTAMP,
           updated_by = p_rejected_by
     WHERE payment_id = p_payment_id;
    --
    log_audit('PAYMENT', p_payment_id, 'REJECT', p_rejected_by, p_reason);
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_reject_payment;
  --
  PROCEDURE sp_cancel_payment(p_payment_id IN NUMBER,
                              p_user_id    IN NUMBER,
                              p_reason     IN VARCHAR2) IS
    v_sale_id NUMBER;
  BEGIN
    --
    SELECT sale_id
      INTO v_sale_id
      FROM payments
     WHERE payment_id = p_payment_id;
    --
    UPDATE payments
       SET status = 'ANULADO',
           notes  = COALESCE(notes, '') || ' | ANULADO: ' || p_reason
     WHERE payment_id = p_payment_id;
    --
    log_audit('PAYMENT', p_payment_id, 'CANCEL', p_user_id, p_reason);
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_cancel_payment;
  --
  PROCEDURE sp_liquidate_sale(p_sale_id IN NUMBER, p_user_id IN NUMBER) IS
  BEGIN
    --
    UPDATE sales SET status = C_STATUS_LIQUIDADO WHERE sale_id = p_sale_id;
    log_audit('SALE', p_sale_id, 'LIQUIDATE', p_user_id, 'Liquidada');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_liquidate_sale;
  -- ========================================================================
  -- PROCEDIMIENTOS DE VENTAS
  -- ========================================================================
PROCEDURE sp_register_sale(p_sale_id               OUT NUMBER,
                             p_customer_id           IN NUMBER,
                             p_seller_id             IN NUMBER,
                             p_payment_term_days     IN NUMBER,
                             p_payment_term          IN VARCHAR2 DEFAULT 'SEMANAL',
                             p_collection_day        IN VARCHAR2 DEFAULT NULL,
                             p_first_collection_date IN DATE DEFAULT NULL,
                             p_down_payment          IN NUMBER DEFAULT 0,
                             p_notes                 IN VARCHAR2 DEFAULT NULL) IS
  BEGIN
    --
    INSERT INTO sales
      (sale_id,
       customer_id,
       seller_id,
       sale_date,
       total_amount,
       payment_term,
       collection_day,
       first_collection_date,
       down_payment,
       payment_terms,
       number_of_payments,
       status,
       notes,
       created_at)
    VALUES
      (seq_sales.NEXTVAL,
       p_customer_id,
       p_seller_id,
       SYSDATE,
       0, -- Se calculara al agregar detalles
       p_payment_term,
       p_collection_day,
       p_first_collection_date,
       NVL(p_down_payment, 0),
       p_payment_term_days || ' dias', -- Legacy field
       p_payment_term_days,
       C_STATUS_POR_INICIAR,
       p_notes,
       SYSTIMESTAMP)
    RETURNING sale_id INTO p_sale_id;
    --
    log_audit('SALE', p_sale_id, 'INSERT', p_seller_id, 
              'Venta registrada | Plazo: ' || p_payment_term || 
              ' | Dia cobro: ' || NVL(p_collection_day, 'N/A') ||
              ' | Enganche: $' || NVL(p_down_payment, 0));
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_sale;
  --
  PROCEDURE sp_add_sale_detail(p_sale_id    IN NUMBER,
                               p_product_id IN NUMBER,
                               p_quantity   IN NUMBER,
                               p_unit_price IN NUMBER) IS
    v_subtotal NUMBER;
  BEGIN
    --
    v_subtotal := p_quantity * p_unit_price;
    --
    INSERT INTO sale_items
      (sale_item_id, sale_id, product_id, quantity, unit_price, subtotal)
    VALUES
      (seq_sale_items.NEXTVAL,
       p_sale_id,
       p_product_id,
       p_quantity,
       p_unit_price,
       v_subtotal);
    --
    -- Actualizar el total de la venta
    UPDATE sales
       SET total_amount = total_amount + v_subtotal
     WHERE sale_id = p_sale_id;
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_add_sale_detail;
  --
  PROCEDURE sp_add_sale_photo(p_photo_id     OUT NUMBER,
                              p_sale_id      IN NUMBER,
                              p_photo_type   IN VARCHAR2,
                              p_file_path    IN VARCHAR2,
                              p_gps_lat      IN NUMBER DEFAULT NULL,
                              p_gps_lon      IN NUMBER DEFAULT NULL,
                              p_file_size    IN NUMBER DEFAULT NULL,
                              p_uploaded_by  IN NUMBER DEFAULT NULL) IS
  BEGIN
    --
    INSERT INTO sale_photos
      (photo_id,
       sale_id,
       photo_type,
       file_path,
       gps_latitude,
       gps_longitude,
       file_size,
       uploaded_at,
       uploaded_by,
       synced)
    VALUES
      (seq_sale_photos.NEXTVAL,
       p_sale_id,
       p_photo_type,
       p_file_path,
       p_gps_lat,
       p_gps_lon,
       p_file_size,
       CURRENT_TIMESTAMP,
       p_uploaded_by,
       '1')
    RETURNING photo_id INTO p_photo_id;
    --
    log_audit('SALE_PHOTO',
              p_photo_id,
              'INSERT',
              p_uploaded_by,
              'Foto tipo: ' || p_photo_type || ' para venta ' || p_sale_id);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_add_sale_photo;
  --
  PROCEDURE sp_get_sale_photos(p_sale_id IN NUMBER,
                               p_cursor  OUT SYS_REFCURSOR) IS
  BEGIN
    --
    OPEN p_cursor FOR
      SELECT photo_id,
             sale_id,
             photo_type,
             file_path,
             thumbnail_path,
             gps_latitude,
             gps_longitude,
             file_size,
             uploaded_at,
             uploaded_by,
             synced
        FROM sale_photos
       WHERE sale_id = p_sale_id
       ORDER BY 
         CASE photo_type
           WHEN 'FACHADA' THEN 1
           WHEN 'CLIENTE' THEN 2
           WHEN 'CONTRATO' THEN 3
           WHEN 'ADICIONAL' THEN 4
           ELSE 5
         END,
         uploaded_at;
    --
  END sp_get_sale_photos;
  --
  PROCEDURE sp_delete_sale_photo(p_photo_id IN NUMBER) IS
    v_sale_id NUMBER;
  BEGIN
    --
    SELECT sale_id INTO v_sale_id
      FROM sale_photos
     WHERE photo_id = p_photo_id;
    --
    DELETE FROM sale_photos WHERE photo_id = p_photo_id;
    --
    log_audit('SALE_PHOTO', p_photo_id, 'DELETE', NULL, 'Foto eliminada de venta ' || v_sale_id);
    COMMIT;
    --
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20010, 'Foto no encontrada');
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_delete_sale_photo;
  --
  PROCEDURE sp_cancel_sale(p_sale_id IN NUMBER,
                           p_user_id IN NUMBER DEFAULT NULL,
                           p_reason  IN VARCHAR2 DEFAULT NULL) IS
  BEGIN
    --
    UPDATE sales SET status = C_STATUS_CANCELADO WHERE sale_id = p_sale_id;
    log_audit('SALE', p_sale_id, 'CANCEL', p_user_id, p_reason);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_cancel_sale;
  --
  PROCEDURE sp_reassign_collector(p_sale_id          IN NUMBER,
                                  p_new_collector_id IN NUMBER,
                                  p_user_id          IN NUMBER) IS
  BEGIN
    --
    UPDATE sales
       SET assigned_collector_id = p_new_collector_id
     WHERE sale_id = p_sale_id;
    log_audit('SALE',
              p_sale_id,
              'REASSIGN',
              p_user_id,
              'Nuevo cobrador: ' || p_new_collector_id);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_reassign_collector;
  -- ========================================================================
  -- PROCEDIMIENTOS DE CLIENTES
  -- ========================================================================

  PROCEDURE sp_register_customer(p_customer_id    OUT NUMBER,
                                 p_name           IN VARCHAR2,
                                 p_phone          IN VARCHAR2 DEFAULT NULL,
                                 p_email          IN VARCHAR2 DEFAULT NULL,
                                 p_address        IN VARCHAR2 DEFAULT NULL,
                                 p_zone_id        IN NUMBER DEFAULT NULL,
                                 p_gps_lat        IN VARCHAR2 DEFAULT NULL,
                                 p_gps_lon        IN VARCHAR2 DEFAULT NULL,
                                 p_is_gold        IN NUMBER DEFAULT 0,
                                 p_is_blacklisted IN NUMBER DEFAULT 0,
                                 p_created_by     IN NUMBER DEFAULT NULL) IS
    v_gps_lat NUMBER;
    v_gps_lon NUMBER;
  BEGIN
    -- Convertir GPS de VARCHAR2 a NUMBER con manejo de errores
    BEGIN
      v_gps_lat := CASE WHEN p_gps_lat IS NOT NULL THEN TO_NUMBER(p_gps_lat, '999.999999', 'NLS_NUMERIC_CHARACTERS=''.,''') ELSE NULL END;
    EXCEPTION
      WHEN OTHERS THEN
        v_gps_lat := NULL;
    END;
    
    BEGIN
      v_gps_lon := CASE WHEN p_gps_lon IS NOT NULL THEN TO_NUMBER(p_gps_lon, '999.999999', 'NLS_NUMERIC_CHARACTERS=''.,''') ELSE NULL END;
    EXCEPTION
      WHEN OTHERS THEN
        v_gps_lon := NULL;
    END;
    
    --
    INSERT INTO customers
      (customer_id,
       customer_name,
       phone,
       email,
       address,
       zone_id,
       gps_latitude,
       gps_longitude,
       is_gold_customer,
       is_blacklisted,
       created_by,
       created_at)
    VALUES
      (seq_customers.NEXTVAL,
       p_name,
       p_phone,
       p_email,
       p_address,
       p_zone_id,
       v_gps_lat,
       v_gps_lon,
       CASE WHEN p_is_gold = 1 THEN 1 ELSE 0 END,
       CASE WHEN p_is_blacklisted = 1 THEN 1 ELSE 0 END,
       p_created_by,
       SYSTIMESTAMP)
    RETURNING customer_id INTO p_customer_id;
    --
    log_audit('CUSTOMER',
              p_customer_id,
              'INSERT',
              p_created_by,
              'Cliente registrado: ' || p_name);
    --
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_customer;
  --
  PROCEDURE sp_update_customer(p_customer_id    IN NUMBER,
                               p_name           IN VARCHAR2,
                               p_phone          IN VARCHAR2 DEFAULT NULL,
                               p_email          IN VARCHAR2 DEFAULT NULL,
                               p_address        IN VARCHAR2 DEFAULT NULL,
                               p_zone_id        IN NUMBER DEFAULT NULL,
                               p_gps_lat        IN VARCHAR2 DEFAULT NULL,
                               p_gps_lon        IN VARCHAR2 DEFAULT NULL,
                               p_is_gold        IN NUMBER DEFAULT 0,
                               p_is_blacklisted IN NUMBER DEFAULT 0) IS
    v_gps_lat NUMBER;
    v_gps_lon NUMBER;
  BEGIN
    -- Convertir GPS de VARCHAR2 a NUMBER con manejo de errores
    BEGIN
      v_gps_lat := CASE WHEN p_gps_lat IS NOT NULL THEN TO_NUMBER(p_gps_lat, '999.999999', 'NLS_NUMERIC_CHARACTERS=''.,''') ELSE NULL END;
    EXCEPTION
      WHEN OTHERS THEN
        v_gps_lat := NULL;
    END;
    
    BEGIN
      v_gps_lon := CASE WHEN p_gps_lon IS NOT NULL THEN TO_NUMBER(p_gps_lon, '999.999999', 'NLS_NUMERIC_CHARACTERS=''.,''') ELSE NULL END;
    EXCEPTION
      WHEN OTHERS THEN
        v_gps_lon := NULL;
    END;
    
    --
    UPDATE customers
       SET customer_name    = p_name,
           phone            = p_phone,
           email            = p_email,
           address          = p_address,
           zone_id          = p_zone_id,
           gps_latitude     = v_gps_lat,
           gps_longitude    = v_gps_lon,
           is_gold_customer = CASE
                                WHEN p_is_gold = 1 THEN
                                 1
                                ELSE
                                 0
                              END,
           is_blacklisted = CASE
                              WHEN p_is_blacklisted = 1 THEN
                               1
                              ELSE
                               0
                            END,
           updated_at       = SYSTIMESTAMP
     WHERE customer_id = p_customer_id;
    --
    log_audit('CUSTOMER',
              p_customer_id,
              'UPDATE',
              NULL,
              'Cliente actualizado: ' || p_name);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_customer;
  --
  PROCEDURE sp_delete_customer(p_customer_id IN NUMBER) IS
  BEGIN
    --
    -- Desactivación lógica
    UPDATE customers
       SET is_blacklisted = 1, blacklist_date = SYSDATE
     WHERE customer_id = p_customer_id;
    --
    log_audit('CUSTOMER',
              p_customer_id,
              'DELETE',
              NULL,
              'Cliente eliminado/desactivado');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_delete_customer;
  --
  PROCEDURE sp_propose_blacklist(p_customer_id IN NUMBER,
                                 p_proposed_by IN NUMBER,
                                 p_reason      IN VARCHAR2) IS
  BEGIN
    --
    INSERT INTO blacklist_proposals
      (proposal_id, customer_id, proposed_by, reason, status)
    VALUES
      (seq_blacklist_proposals.NEXTVAL,
       p_customer_id,
       p_proposed_by,
       p_reason,
       'PENDING');
    log_audit('BLACKLIST',
              p_customer_id,
              'PROPOSE',
              p_proposed_by,
              p_reason);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_propose_blacklist;
  --
  PROCEDURE sp_approve_blacklist(p_proposal_id IN NUMBER,
                                 p_approved_by IN NUMBER,
                                 p_is_approved IN CHAR) IS
    v_customer_id NUMBER;
    v_new_status  VARCHAR2(20);
  BEGIN
    --
    SELECT customer_id
      INTO v_customer_id
      FROM blacklist_proposals
     WHERE proposal_id = p_proposal_id;
    --
    v_new_status := CASE p_is_approved
                      WHEN '1' THEN
                       'APPROVED'
                      ELSE
                       'REJECTED'
                    END;
    --
    UPDATE blacklist_proposals
       SET status      = v_new_status,
           reviewed_by = p_approved_by,
           reviewed_at = SYSTIMESTAMP
     WHERE proposal_id = p_proposal_id;
    --
    IF p_is_approved = '1' THEN
      UPDATE customers
         SET is_blacklisted = '1', blacklist_date = SYSDATE
       WHERE customer_id = v_customer_id;
    END IF;
    --
    log_audit('BLACKLIST',
              p_proposal_id,
              v_new_status,
              p_approved_by,
              NULL);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_approve_blacklist;
  --
  PROCEDURE sp_mark_gold_customer(p_customer_id IN NUMBER,
                                  p_user_id     IN NUMBER) IS
  BEGIN
    --
    UPDATE customers
       SET is_gold_customer = '1', gold_since = SYSTIMESTAMP
     WHERE customer_id = p_customer_id;
    log_audit('CUSTOMER',
              p_customer_id,
              'MARK_GOLD',
              p_user_id,
              'Gold customer');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_mark_gold_customer;
  -- ========================================================================
  -- PROCEDIMIENTOS DE PRODUCTOS
  -- ========================================================================
  PROCEDURE sp_register_product(p_product_id  OUT NUMBER,
                                p_name        IN VARCHAR2,
                                p_description IN VARCHAR2 DEFAULT NULL,
                                p_unit_price  IN NUMBER,
                                p_stock       IN NUMBER,
                                p_category_id IN NUMBER DEFAULT NULL,
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
       category_id,
       unit_price,
       stock_quantity,
       is_active,
       photo_url,
       created_at)
    VALUES
      (v_product_id,
       v_product_code,
       p_name,
       p_description,
       p_category_id,
       p_unit_price,
       NVL(p_stock, 0),
       1,
       p_photo_url,
       SYSTIMESTAMP);
    
    -- Devolver ID generado
    p_product_id := v_product_id;
    --
    log_audit('PRODUCT',
              p_product_id,
              'INSERT',
              NULL,
              'Producto: ' || p_name || ' [' || v_product_code || ']');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_product;
  --
  PROCEDURE sp_update_product(p_product_id  IN NUMBER,
                              p_name        IN VARCHAR2,
                              p_description IN VARCHAR2 DEFAULT NULL,
                              p_unit_price  IN NUMBER,
                              p_stock       IN NUMBER,
                              p_is_active   IN NUMBER DEFAULT 1,
                              p_category_id IN NUMBER DEFAULT NULL,
                              p_photo_url   IN VARCHAR2 DEFAULT NULL) IS
  BEGIN
    --
    UPDATE products
       SET product_name   = p_name,
           description    = p_description,
           unit_price     = p_unit_price,
           stock_quantity = NVL(p_stock, stock_quantity),
           category_id    = p_category_id,
           photo_url      = CASE WHEN p_photo_url IS NOT NULL THEN p_photo_url ELSE photo_url END,
           is_active      = CASE
                              WHEN p_is_active = 1 THEN 1
                              ELSE 0
                            END,
           updated_at     = SYSTIMESTAMP
     WHERE product_id = p_product_id;
    --
    log_audit('PRODUCT',
              p_product_id,
              'UPDATE',
              NULL,
              'Producto actualizado: ' || p_name);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_product;
  --
  PROCEDURE sp_delete_product(p_product_id IN NUMBER) IS
  BEGIN
    --
    -- Desactivación lógica
    UPDATE products SET is_active = 0 WHERE product_id = p_product_id;
    --
    log_audit('PRODUCT',
              p_product_id,
              'DELETE',
              NULL,
              'Producto desactivado');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_delete_product;
  --
  PROCEDURE sp_update_product_photo(p_product_id IN NUMBER,
                                    p_photo_url  IN VARCHAR2) IS
  BEGIN
    UPDATE products
       SET photo_url  = p_photo_url,
           updated_at = SYSTIMESTAMP
     WHERE product_id = p_product_id;
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_product_photo;
  -- ========================================================================
  -- PROCEDIMIENTOS DE ZONAS
  -- ========================================================================
  PROCEDURE sp_register_zone(p_zone_id     OUT NUMBER,
                             p_name        IN VARCHAR2,
                             p_zone_code   IN VARCHAR2 DEFAULT NULL,
                             p_description IN VARCHAR2 DEFAULT NULL,
                             p_is_active   IN NUMBER DEFAULT 1) IS
  BEGIN
    --
    INSERT INTO zones
      (zone_id, zone_name, zone_code, description, is_active)
    VALUES
      (seq_zones.NEXTVAL, p_name, p_zone_code, p_description,
       CASE WHEN NVL(p_is_active,1) = 1 THEN '1' ELSE '0' END)
    RETURNING zone_id INTO p_zone_id;
    --
    log_audit('ZONE', p_zone_id, 'INSERT', NULL, 'Zona: ' || p_name);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_zone;
  --
  PROCEDURE sp_update_zone(p_zone_id     IN NUMBER,
                           p_name        IN VARCHAR2,
                           p_zone_code   IN VARCHAR2 DEFAULT NULL,
                           p_description IN VARCHAR2 DEFAULT NULL,
                           p_is_active   IN NUMBER DEFAULT 1) IS
  BEGIN
    --
    UPDATE zones
       SET zone_name  = p_name,
           zone_code  = p_zone_code,
           description = p_description,
           is_active   = CASE WHEN NVL(p_is_active,1) = 1 THEN '1' ELSE '0' END
     WHERE zone_id = p_zone_id;
    --
    log_audit('ZONE',
              p_zone_id,
              'UPDATE',
              NULL,
              'Zona actualizada: ' || p_name);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_zone;
  --
  PROCEDURE sp_delete_zone(p_zone_id IN NUMBER) IS
    v_customer_count NUMBER := 0;
  BEGIN
    --
    -- Verificar si hay clientes asignados a esta zona
    SELECT COUNT(*) INTO v_customer_count
      FROM customers WHERE zone_id = p_zone_id;
    --
    IF v_customer_count > 0 THEN
      -- Borrado logico: desactivar la zona
      UPDATE zones SET is_active = '0' WHERE zone_id = p_zone_id;
      log_audit('ZONE', p_zone_id, 'DEACTIVATE', NULL,
                'Zona desactivada (tenia ' || v_customer_count || ' clientes asignados)');
    ELSE
      -- Sin dependencias: borrado fisico
      DELETE FROM zones WHERE zone_id = p_zone_id;
      log_audit('ZONE', p_zone_id, 'DELETE', NULL, 'Zona eliminada');
    END IF;
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_delete_zone;
  -- ========================================================================
  -- PROCEDIMIENTOS DE AUTENTICACION
  -- ========================================================================
  FUNCTION fn_authenticate_user(p_username IN VARCHAR2,
                                p_password IN VARCHAR2) RETURN NUMBER IS
    v_user_id       NUMBER;
    v_password_hash VARCHAR2(500);
    v_is_active     NUMBER;
  BEGIN
    --
    SELECT user_id, password_hash, is_active
      INTO v_user_id, v_password_hash, v_is_active
      FROM users
     WHERE username = p_username;
    --
    IF v_is_active = 0 THEN
      RETURN NULL;
    END IF;
    --
    -- TODO: Implementar verificación BCrypt en lugar de comparación directa
    IF v_password_hash = p_password THEN
      -- Actualizar último login
      UPDATE users SET last_login = SYSTIMESTAMP WHERE user_id = v_user_id;
      COMMIT;
      RETURN v_user_id;
    ELSE
      RETURN NULL;
    END IF;
    --
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RETURN NULL;
    WHEN OTHERS THEN
      RETURN NULL;
  END fn_authenticate_user;
  --  
  PROCEDURE sp_get_user_info(p_user_id   IN NUMBER,
                             p_username  OUT VARCHAR2,
                             p_role_name OUT VARCHAR2,
                             p_role_id   OUT NUMBER) IS
  BEGIN
    SELECT u.username, NVL(r.role_name, 'Usuario'), u.role_id
      INTO p_username, p_role_name, p_role_id
      FROM users u
      LEFT JOIN roles r
        ON u.role_id = r.role_id
     WHERE u.user_id = p_user_id
       AND u.is_active = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      p_username  := NULL;
      p_role_name := NULL;
      p_role_id   := NULL;
    WHEN OTHERS THEN
      RAISE;
  END sp_get_user_info;
  --
  PROCEDURE sp_update_last_login(p_user_id IN NUMBER) IS
  BEGIN
    --
    UPDATE users SET last_login = SYSTIMESTAMP WHERE user_id = p_user_id;
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
  END sp_update_last_login;
  --
  PROCEDURE sp_get_users(p_search_term IN VARCHAR2 DEFAULT NULL,
                         p_role_id     IN NUMBER DEFAULT NULL,
                         p_is_active   IN NUMBER DEFAULT NULL,
                         p_cursor      OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT u.user_id      AS "userId",
             u.username     AS "username",
             u.full_name    AS "fullName",
             u.email        AS "email",
             u.role_id      AS "roleId",
             r.role_name    AS "roleName",
             u.zone_id      AS "zoneId",
             z.zone_name    AS "zoneName",
             CASE
               WHEN NVL(u.is_active, '1') = '1' THEN
                1
               ELSE
                0
             END            AS "isActive",
             u.created_at   AS "createdAt",
             u.last_login   AS "lastLogin"
        FROM users u
        JOIN roles r
          ON u.role_id = r.role_id
        LEFT JOIN zones z
          ON u.zone_id = z.zone_id
       WHERE (p_search_term IS NULL OR
             UPPER(u.username) LIKE '%' || UPPER(TRIM(p_search_term)) || '%' OR
             UPPER(u.full_name) LIKE '%' || UPPER(TRIM(p_search_term)) || '%' OR
             UPPER(NVL(u.email, '')) LIKE '%' || UPPER(TRIM(p_search_term)) || '%')
         AND (p_role_id IS NULL OR u.role_id = p_role_id)
         AND (p_is_active IS NULL OR
             (p_is_active = 1 AND NVL(u.is_active, '1') = '1') OR
             (p_is_active = 0 AND NVL(u.is_active, '1') = '0'))
       ORDER BY u.username;
  END sp_get_users;
  --
  PROCEDURE sp_get_user_by_id(p_user_id IN NUMBER,
                              p_cursor  OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT u.user_id      AS "userId",
             u.username     AS "username",
             u.full_name    AS "fullName",
             u.email        AS "email",
             u.role_id      AS "roleId",
             r.role_name    AS "roleName",
             u.zone_id      AS "zoneId",
             z.zone_name    AS "zoneName",
             CASE
               WHEN NVL(u.is_active, '1') = '1' THEN
                1
               ELSE
                0
             END            AS "isActive",
             u.created_at   AS "createdAt",
             u.last_login   AS "lastLogin"
        FROM users u
        JOIN roles r
          ON u.role_id = r.role_id
        LEFT JOIN zones z
          ON u.zone_id = z.zone_id
       WHERE u.user_id = p_user_id;
  END sp_get_user_by_id;
  --
  PROCEDURE sp_register_user(p_user_id       OUT NUMBER,
                             p_username      IN VARCHAR2,
                             p_full_name     IN VARCHAR2,
                             p_email         IN VARCHAR2 DEFAULT NULL,
                             p_password      IN VARCHAR2,
                             p_role_id       IN NUMBER,
                             p_zone_id       IN NUMBER DEFAULT NULL,
                             p_is_active     IN NUMBER DEFAULT 1,
                             p_created_by    IN NUMBER DEFAULT NULL) IS
  BEGIN
    INSERT INTO users
      (user_id,
       username,
       password_hash,
       full_name,
       email,
       role_id,
       zone_id,
       is_active,
       created_at)
    VALUES
      (seq_users.NEXTVAL,
       LOWER(TRIM(p_username)),
       p_password,
       TRIM(p_full_name),
       CASE
         WHEN p_email IS NULL THEN
          NULL
         ELSE
          LOWER(TRIM(p_email))
       END,
       p_role_id,
       p_zone_id,
       CASE
         WHEN NVL(p_is_active, 1) = 1 THEN
          '1'
         ELSE
          '0'
       END,
       SYSTIMESTAMP)
    RETURNING user_id INTO p_user_id;

    log_audit('USER',
              p_user_id,
              'INSERT',
              p_created_by,
              'Usuario: ' || LOWER(TRIM(p_username)));
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_user;
  --
  PROCEDURE sp_update_user(p_user_id    IN NUMBER,
                           p_full_name  IN VARCHAR2,
                           p_email      IN VARCHAR2 DEFAULT NULL,
                           p_role_id    IN NUMBER,
                           p_zone_id    IN NUMBER DEFAULT NULL,
                           p_is_active  IN NUMBER DEFAULT 1,
                           p_updated_by IN NUMBER DEFAULT NULL) IS
  BEGIN
    UPDATE users
       SET full_name  = TRIM(p_full_name),
           email      = CASE
                          WHEN p_email IS NULL THEN
                           NULL
                          ELSE
                           LOWER(TRIM(p_email))
                        END,
           role_id    = p_role_id,
           zone_id    = p_zone_id,
           is_active  = CASE
                          WHEN NVL(p_is_active, 1) = 1 THEN
                           '1'
                          ELSE
                           '0'
                        END,
           updated_at = SYSTIMESTAMP
     WHERE user_id = p_user_id;

    log_audit('USER', p_user_id, 'UPDATE', p_updated_by, 'Usuario actualizado');
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_user;
  --
  PROCEDURE sp_delete_user(p_user_id    IN NUMBER,
                           p_deleted_by IN NUMBER DEFAULT NULL) IS
  BEGIN
    UPDATE users
       SET is_active  = '0',
           updated_at = SYSTIMESTAMP
     WHERE user_id = p_user_id;

    log_audit('USER', p_user_id, 'DELETE', p_deleted_by, 'Usuario desactivado');
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_delete_user;
  --
  FUNCTION fn_change_user_password(p_user_id          IN NUMBER,
                                   p_current_password IN VARCHAR2,
                                   p_new_password     IN VARCHAR2) RETURN CHAR IS
    v_password_hash users.password_hash%TYPE;
  BEGIN
    SELECT password_hash
      INTO v_password_hash
      FROM users
     WHERE user_id = p_user_id;

    IF v_password_hash != p_current_password THEN
      RETURN '0';
    END IF;

    UPDATE users
       SET password_hash = p_new_password,
           updated_at    = SYSTIMESTAMP
     WHERE user_id = p_user_id;

    COMMIT;
    RETURN '1';
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RETURN '0';
    WHEN OTHERS THEN
      ROLLBACK;
      RETURN '0';
  END fn_change_user_password;
  --
  PROCEDURE sp_get_roles(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT r.role_id             AS "roleId",
             r.role_name           AS "roleName",
             NVL(r.description, r.role_description) AS "description",
             r.default_theme_color AS "defaultThemeColor"
        FROM roles r
       ORDER BY r.role_name;
  END sp_get_roles;
  --
  PROCEDURE sp_update_role(p_role_id             IN NUMBER,
                           p_role_name           IN VARCHAR2,
                           p_description         IN VARCHAR2 DEFAULT NULL,
                           p_default_theme_color IN VARCHAR2 DEFAULT NULL,
                           p_updated_by          IN NUMBER DEFAULT NULL) IS
  BEGIN
    UPDATE roles
       SET role_name           = TRIM(p_role_name),
           role_description    = p_description,
           description         = p_description,
           default_theme_color = p_default_theme_color
     WHERE role_id = p_role_id;

    log_audit('ROLE', p_role_id, 'UPDATE', p_updated_by, 'Rol actualizado');
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_role;
  --
  PROCEDURE sp_get_role_permissions(p_role_id IN NUMBER,
                                    p_cursor  OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT p.permission_name AS "permissionName",
             p.module          AS "module",
             p.description     AS "description"
        FROM role_permissions rp
        JOIN permissions p
          ON rp.permission_id = p.permission_id
       WHERE rp.role_id = p_role_id
       ORDER BY p.module, p.permission_name;
  END sp_get_role_permissions;
    --
    PROCEDURE sp_get_permissions(p_cursor OUT SYS_REFCURSOR) IS
    BEGIN
      OPEN p_cursor FOR
        SELECT p.permission_name AS "permissionName",
               p.module          AS "module",
               p.description     AS "description"
          FROM permissions p
         ORDER BY p.module, p.permission_name;
    END sp_get_permissions;
  --
  PROCEDURE sp_set_role_permissions(p_role_id         IN NUMBER,
                                    p_permissions_csv IN CLOB,
                                    p_updated_by      IN NUMBER DEFAULT NULL) IS
    v_permission_name VARCHAR2(200);
    v_position        NUMBER := 1;
  BEGIN
    DELETE FROM role_permissions WHERE role_id = p_role_id;

    LOOP
      v_permission_name :=
       TRIM(REGEXP_SUBSTR(p_permissions_csv, '[^,]+', 1, v_position));

      EXIT WHEN v_permission_name IS NULL;

      INSERT INTO role_permissions
        (role_id, permission_id, granted_at)
      SELECT p_role_id, p.permission_id, SYSTIMESTAMP
        FROM permissions p
       WHERE UPPER(p.permission_name) = UPPER(v_permission_name);

      v_position := v_position + 1;
    END LOOP;

    log_audit('ROLE',
              p_role_id,
              'UPDATE_PERMISSIONS',
              p_updated_by,
              'Permisos actualizados');
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_set_role_permissions;
  -- ========================================================================
  -- PROCEDIMIENTOS DE RUTAS
  -- ========================================================================
  PROCEDURE sp_register_visit_action(p_sale_id      IN NUMBER,
                                     p_collector_id IN NUMBER,
                                     p_action_type  IN VARCHAR2,
                                     p_gps_lat      IN NUMBER,
                                     p_gps_lon      IN NUMBER,
                                     p_notes        IN VARCHAR2 DEFAULT NULL) IS
  BEGIN
    --
    INSERT INTO collector_visit_actions
      (action_id,
       sale_id,
       collector_id,
       action_type,
       action_date,
       gps_latitude,
       gps_longitude,
       notes)
    VALUES
      (seq_collector_actions.NEXTVAL,
       p_sale_id,
       p_collector_id,
       p_action_type,
       SYSDATE,
       p_gps_lat,
       p_gps_lon,
       p_notes);
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_visit_action;
  --
  PROCEDURE sp_generate_daily_route(p_collector_id IN NUMBER,
                                    p_route_date   IN DATE DEFAULT SYSDATE) IS
    v_route_id    NUMBER;
    v_sales_count NUMBER;
  BEGIN
    --
    SELECT COUNT(*)
      INTO v_sales_count
      FROM sales
     WHERE assigned_collector_id = p_collector_id
       AND status IN (C_STATUS_EN_PROCESO, C_STATUS_POR_INICIAR);
    --
    INSERT INTO collector_routes
      (route_id,
       collector_id,
       route_date,
       sales_assigned,
       sales_visited,
       total_collected)
    VALUES
      (seq_collector_routes.NEXTVAL,
       p_collector_id,
       TRUNC(p_route_date),
       v_sales_count,
       0,
       0)
    RETURNING route_id INTO v_route_id;
    --
    log_audit('ROUTE',
              v_route_id,
              'GENERATE',
              p_collector_id,
              v_sales_count || ' cuentas');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_generate_daily_route;
  --
  PROCEDURE sp_close_daily_route(p_route_id        IN NUMBER,
                                 p_total_collected IN NUMBER) IS
  BEGIN
    --
    UPDATE collector_routes
       SET total_collected = p_total_collected
     WHERE route_id = p_route_id;
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_close_daily_route;
  -- ========================================================================
  -- SINCRONIZACION (Implementacion simplificada)
  -- ========================================================================
  PROCEDURE sp_process_sync_queue(p_device_id IN VARCHAR2,
                                  p_user_id   IN NUMBER) IS
  BEGIN
    -- Implementacion futura con parsing JSON
    NULL;
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_process_sync_queue;
  --
  PROCEDURE sp_resolve_sync_conflict(p_conflict_id IN NUMBER,
                                     p_resolution  IN VARCHAR2,
                                     p_resolved_by IN NUMBER) IS
  BEGIN
    -- Implementacion futura
    NULL;
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_resolve_sync_conflict;
  -- ========================================================================
  -- FUNCIONES DE CALCULO
  -- ========================================================================
  FUNCTION fn_get_sale_balance(p_sale_id IN NUMBER) RETURN NUMBER IS
    v_balance NUMBER;
  BEGIN
    --
    SELECT s.total_amount - NVL((SELECT SUM(p.amount)
                                  FROM payments p
                                 WHERE p.sale_id = s.sale_id
                                   AND p.status = C_PAY_CONFIRMED),
                                0)
      INTO v_balance
      FROM sales s
     WHERE s.sale_id = p_sale_id;
    RETURN v_balance;
    --
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RETURN 0;
    WHEN OTHERS THEN
      RETURN - 1;
  END fn_get_sale_balance;
  --
  FUNCTION fn_get_risk_status(p_sale_id IN NUMBER) RETURN VARCHAR2 IS
    v_days NUMBER;
  BEGIN
    --
    v_days := fn_days_since_payment(p_sale_id);
    RETURN CASE WHEN v_days <= 7 THEN C_RISK_VERDE WHEN v_days <= 14 THEN C_RISK_AMARILLO WHEN v_days <= 21 THEN C_RISK_ROJO ELSE C_RISK_CRITICO END;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN C_RISK_CRITICO;
  END fn_get_risk_status;
  --
  FUNCTION fn_days_since_payment(p_sale_id IN NUMBER) RETURN NUMBER IS
    v_days NUMBER;
  BEGIN
    --
    SELECT TRUNC(SYSDATE) - TRUNC(NVL((SELECT MAX(p.payment_date)
                                        FROM payments p
                                       WHERE p.sale_id = s.sale_id
                                         AND p.status = C_PAY_CONFIRMED),
                                      s.sale_date))
      INTO v_days
      FROM sales s
     WHERE s.sale_id = p_sale_id;
    RETURN v_days;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 999;
  END fn_days_since_payment;
  --
  FUNCTION fn_payment_percentage(p_sale_id IN NUMBER) RETURN NUMBER IS
    v_total NUMBER;
    v_paid  NUMBER;
  BEGIN
    --
    SELECT s.total_amount,
           NVL((SELECT SUM(p.amount)
                 FROM payments p
                WHERE p.sale_id = s.sale_id
                  AND p.status = C_PAY_CONFIRMED),
               0)
      INTO v_total, v_paid
      FROM sales s
     WHERE s.sale_id = p_sale_id;
    --
    RETURN CASE WHEN v_total > 0 THEN ROUND((v_paid / v_total) * 100, 2) ELSE 0 END;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_payment_percentage;
  --
  FUNCTION fn_collector_total(p_collector_id IN NUMBER,
                              p_from_date    IN DATE,
                              p_to_date      IN DATE) RETURN NUMBER IS
    v_total NUMBER;
  BEGIN
    --
    SELECT NVL(SUM(amount), 0)
      INTO v_total
      FROM payments
     WHERE collector_id = p_collector_id
       AND payment_date BETWEEN p_from_date AND p_to_date
       AND status = C_PAY_CONFIRMED;
    RETURN v_total;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_collector_total;
  --
  FUNCTION fn_gps_distance(p_lat1 IN NUMBER,
                           p_lon1 IN NUMBER,
                           p_lat2 IN NUMBER,
                           p_lon2 IN NUMBER) RETURN NUMBER IS
    v_distance NUMBER;
    c_pi           CONSTANT NUMBER := 3.14159265359;
    c_earth_radius CONSTANT NUMBER := 6371;
  BEGIN
    --
    v_distance := c_earth_radius *
                  ACOS(COS((90 - p_lat1) * c_pi / 180) *
                       COS((90 - p_lat2) * c_pi / 180) +
                       SIN((90 - p_lat1) * c_pi / 180) *
                       SIN((90 - p_lat2) * c_pi / 180) *
                       COS((p_lon1 - p_lon2) * c_pi / 180));
    RETURN ROUND(v_distance, 2);
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_gps_distance;
  --
  FUNCTION fn_is_blacklisted(p_customer_id IN NUMBER) RETURN CHAR IS
    v_result CHAR(1);
  BEGIN
    SELECT NVL(is_blacklisted, '0')
      INTO v_result
      FROM customers
     WHERE customer_id = p_customer_id;
    RETURN v_result;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN '0';
  END fn_is_blacklisted;
  --
  FUNCTION fn_has_permission(p_user_id         IN NUMBER,
                             p_permission_code IN VARCHAR2) RETURN CHAR IS
    v_count NUMBER;
  BEGIN
    --
    SELECT COUNT(*)
      INTO v_count
      FROM users u
      JOIN role_permissions rp
        ON u.role_id = rp.role_id
      JOIN permissions p
        ON rp.permission_id = p.permission_id
     WHERE u.user_id = p_user_id
       AND p.permission_name = p_permission_code;
    RETURN CASE WHEN v_count > 0 THEN '1' ELSE '0' END;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN '0';
  END fn_has_permission;
  --
  FUNCTION fn_generate_folio(p_type IN VARCHAR2) RETURN VARCHAR2 IS
    v_folio VARCHAR2(50);
    v_count NUMBER;
  BEGIN
    --
    IF p_type = 'SALE' THEN
      SELECT COUNT(*) INTO v_count FROM sales;
    ELSIF p_type = 'PAYMENT' THEN
      SELECT COUNT(*) INTO v_count FROM payments;
    ELSIF p_type = 'CUSTOMER' THEN
      SELECT COUNT(*) INTO v_count FROM customers;
    ELSE
      v_count := 0;
    END IF;
    --
    v_folio := p_type || '-' || TO_CHAR(SYSDATE, 'YYYYMMDD') || '-' ||
               LPAD(v_count + 1, 6, '0');
    RETURN v_folio;
    --
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 'ERROR-' || TO_CHAR(SYSDATE, 'YYYYMMDD');
  END fn_generate_folio;
  -- ========================================================================
  -- FUNCIONES DE VALIDACION
  -- ========================================================================
  FUNCTION fn_validate_gps(p_lat IN NUMBER, p_lon IN NUMBER) RETURN CHAR IS
  BEGIN
    --
    IF p_lat BETWEEN - 90 AND 90 AND p_lon BETWEEN - 180 AND 180 THEN
      RETURN '1';
    ELSE
      RETURN '0';
    END IF;
  END fn_validate_gps;
  --
  FUNCTION fn_can_receive_payment(p_sale_id IN NUMBER) RETURN CHAR IS
    v_status VARCHAR2(20);
  BEGIN
    --
    SELECT status INTO v_status FROM sales WHERE sale_id = p_sale_id;
    RETURN CASE WHEN v_status IN(C_STATUS_EN_PROCESO, C_STATUS_POR_INICIAR) THEN '1' ELSE '0' END;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN '0';
  END fn_can_receive_payment;
  --
  FUNCTION fn_validate_payment_amount(p_sale_id IN NUMBER,
                                      p_amount  IN NUMBER) RETURN CHAR IS
    v_balance NUMBER;
  BEGIN
    --
    -- Monto negativo siempre es invalido
    IF p_amount < 0 THEN
      RETURN '0';
    END IF;
    -- Monto = 0 es valido para registros de visita (no encontrado, promesa de pago, etc.)
    IF p_amount = 0 THEN
      RETURN '1';
    END IF;
    -- Monto > 0: validar que no exceda el saldo + 10% de tolerancia
    v_balance := fn_get_sale_balance(p_sale_id);
    RETURN CASE WHEN p_amount <=(v_balance * 1.1) THEN '1' ELSE '0' END;
  END fn_validate_payment_amount;
  -- ========================================================================
  -- REPORTES
  -- ========================================================================
  PROCEDURE sp_daily_collection_stats(p_date    IN DATE DEFAULT SYSDATE,
                                      p_zone_id IN NUMBER DEFAULT NULL) IS
  BEGIN
    --
    FOR rec IN (SELECT z.zone_name,
                       u.full_name AS collector_name,
                       COUNT(p.payment_id) AS num_payments,
                       SUM(p.amount) AS total_collected
                  FROM payments p
                  JOIN users u
                    ON p.collector_id = u.user_id
                  JOIN sales s
                    ON p.sale_id = s.sale_id
                  JOIN customers c
                    ON s.customer_id = c.customer_id
                  JOIN zones z
                    ON c.zone_id = z.zone_id
                 WHERE TRUNC(p.payment_date) = TRUNC(p_date)
                   AND p.status = 'CONFIRMADO'
                   AND (p_zone_id IS NULL OR z.zone_id = p_zone_id)
                 GROUP BY z.zone_name, u.full_name
                 ORDER BY z.zone_name, u.full_name) LOOP
      DBMS_OUTPUT.PUT_LINE(rec.zone_name || ' - ' || rec.collector_name ||
                           ': $' || rec.total_collected || ' (' ||
                           rec.num_payments || ' pagos)');
    END LOOP;
    --
  END sp_daily_collection_stats;
  --
  PROCEDURE sp_send_whatsapp_reminder(p_sale_id       IN NUMBER,
                                      p_template_name IN VARCHAR2 DEFAULT 'PAYMENT_REMINDER') IS
    v_customer_id   NUMBER;
    v_customer_name VARCHAR2(200);
    v_phone         VARCHAR2(20);
    v_amount        NUMBER;
    v_template      VARCHAR2(4000);
    v_message       VARCHAR2(4000);
  BEGIN
    --
    SELECT c.customer_name, c.phone, s.payment_amount
      INTO v_customer_name, v_phone, v_amount
      FROM sales s
      JOIN customers c
        ON s.customer_id = c.customer_id
     WHERE s.sale_id = p_sale_id;
    --
    SELECT template_body
      INTO v_template
      FROM whatsapp_templates
     WHERE template_name = p_template_name
       AND is_active = '1';
    --
    v_message := REPLACE(v_template, '{customer_name}', v_customer_name);
    v_message := REPLACE(v_message, '{amount}', TO_CHAR(v_amount));
    -- Obtener customer_id
    SELECT c.customer_id
      INTO v_customer_id
      FROM sales s
      JOIN customers c
        ON s.customer_id = c.customer_id
     WHERE s.sale_id = p_sale_id;
    --
    INSERT INTO whatsapp_messages
      (message_id, customer_id, phone_number, message_body, status)
    VALUES
      (seq_whatsapp_messages.NEXTVAL,
       v_customer_id,
       v_phone,
       v_message,
       'PENDING');
    COMMIT;
    --
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_send_whatsapp_reminder;
  --
  PROCEDURE sp_get_sale_statuses(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT STATUS_CODE   AS "statusCode",
             STATUS_NAME   AS "statusName",
             DESCRIPTION   AS "description",
             DISPLAY_ORDER AS "displayOrder",
             COLOR_HEX     AS "colorHex",
             ICON          AS "icon"
        FROM CATALOG_SALE_STATUSES
       WHERE IS_ACTIVE = 1
       ORDER BY DISPLAY_ORDER, STATUS_NAME;
  END sp_get_sale_statuses;
  --
  PROCEDURE sp_get_payment_statuses(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    -- Devuelve estados internos del flujo de aprobación (STATUS_TYPE='WORKFLOW')
    -- Para acciones del cobrador en campo ver sp_get_visit_actions
    OPEN p_cursor FOR
      SELECT STATUS_ID      AS "statusId",
             STATUS_KEY     AS "statusCode",
             STATUS_NAME    AS "statusName",
             DESCRIPTION    AS "description",
             DISPLAY_ORDER  AS "displayOrder",
             COLOR_HEX      AS "colorHex",
             ICON           AS "icon",
             REQUIRES_NOTE  AS "requiresNote",
             REQUIRES_PHOTO AS "requiresPhoto"
        FROM CATALOG_PAYMENT_STATUSES
       WHERE IS_ACTIVE          = 1
         AND STATUS_TYPE        = 'WORKFLOW'
         AND PARENT_STATUS_ID   IS NULL
       ORDER BY DISPLAY_ORDER, STATUS_NAME;
  END sp_get_payment_statuses;
  --
  PROCEDURE sp_get_risk_statuses(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT STATUS_CODE   AS "statusCode",
             STATUS_NAME   AS "statusName",
             DESCRIPTION   AS "description",
             MIN_DAYS      AS "minDays",
             MAX_DAYS      AS "maxDays",
             COLOR_HEX     AS "colorHex",
             ICON          AS "icon",
             DISPLAY_ORDER AS "displayOrder"
        FROM CATALOG_RISK_STATUSES
       WHERE IS_ACTIVE = 1
       ORDER BY DISPLAY_ORDER, MIN_DAYS;
  END sp_get_risk_statuses;
  --
  PROCEDURE sp_get_active_theme(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT THEME_CODE       AS "themeCode",
             THEME_NAME       AS "themeName",
             PRIMARY_COLOR    AS "primaryColor",
             SECONDARY_COLOR  AS "secondaryColor",
             ACCENT_COLOR     AS "accentColor",
             BACKGROUND_COLOR AS "backgroundColor",
             TEXT_COLOR       AS "textColor"
        FROM CATALOG_APP_THEMES
       WHERE IS_DEFAULT = 1
         AND IS_ACTIVE = 1
       FETCH FIRST 1 ROWS ONLY;
  END sp_get_active_theme;

  PROCEDURE sp_get_notification_templates(p_template_type IN VARCHAR2 DEFAULT NULL,
                                          p_cursor        OUT SYS_REFCURSOR) IS
  BEGIN
    IF p_template_type IS NULL THEN
      OPEN p_cursor FOR
        SELECT TEMPLATE_CODE AS "templateCode",
               TEMPLATE_NAME AS "templateName",
               TEMPLATE_TYPE AS "templateType",
               SUBJECT       AS "subject",
               MESSAGE_BODY  AS "messageBody",
               VARIABLES     AS "variables"
          FROM CATALOG_NOTIFICATION_TEMPLATES
         WHERE IS_ACTIVE = 1
         ORDER BY TEMPLATE_NAME;
    ELSE
      OPEN p_cursor FOR
        SELECT TEMPLATE_CODE AS "templateCode",
               TEMPLATE_NAME AS "templateName",
               TEMPLATE_TYPE AS "templateType",
               SUBJECT       AS "subject",
               MESSAGE_BODY  AS "messageBody",
               VARIABLES     AS "variables"
          FROM CATALOG_NOTIFICATION_TEMPLATES
         WHERE IS_ACTIVE = 1
           AND UPPER(TEMPLATE_TYPE) = UPPER(p_template_type)
         ORDER BY TEMPLATE_NAME;
    END IF;
  END sp_get_notification_templates;

  PROCEDURE sp_get_app_settings(p_category  IN VARCHAR2 DEFAULT NULL,
                                p_is_public IN NUMBER DEFAULT 1,
                                p_cursor    OUT SYS_REFCURSOR) IS
  BEGIN
    IF p_category IS NULL THEN
      OPEN p_cursor FOR
        SELECT SETTING_KEY   AS "settingKey",
               SETTING_VALUE AS "settingValue",
               SETTING_TYPE  AS "settingType",
               DESCRIPTION   AS "description",
               CATEGORY      AS "category"
          FROM CATALOG_APP_SETTINGS
         WHERE (p_is_public = 0 OR IS_PUBLIC = 1)
         ORDER BY CATEGORY, SETTING_KEY;
    ELSE
      OPEN p_cursor FOR
        SELECT SETTING_KEY   AS "settingKey",
               SETTING_VALUE AS "settingValue",
               SETTING_TYPE  AS "settingType",
               DESCRIPTION   AS "description",
               CATEGORY      AS "category"
          FROM CATALOG_APP_SETTINGS
         WHERE UPPER(CATEGORY) = UPPER(p_category)
           AND (p_is_public = 0 OR IS_PUBLIC = 1)
         ORDER BY SETTING_KEY;
    END IF;
  END sp_get_app_settings;

  PROCEDURE sp_get_visit_actions(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    -- Devuelve acciones de visita del cobrador (STATUS_TYPE='VISIT_ACTION')
    -- Tabla unificada: CATALOG_PAYMENT_STATUSES (ver migración 03)
    OPEN p_cursor FOR
      SELECT STATUS_ID           AS "statusId",
             STATUS_KEY          AS "actionCode",
             STATUS_NAME         AS "actionName",
             DESCRIPTION         AS "description",
             ICON                AS "icon",
             COLOR_HEX           AS "colorHex",
             REQUIRES_NOTE       AS "requiresNote",
             REQUIRES_PHOTO      AS "requiresPhoto",
             DISPLAY_ORDER       AS "displayOrder",
             PARENT_STATUS_ID    AS "parentActionId"
        FROM CATALOG_PAYMENT_STATUSES
       WHERE IS_ACTIVE   = 1
         AND STATUS_TYPE = 'VISIT_ACTION'
       ORDER BY PARENT_STATUS_ID NULLS FIRST, DISPLAY_ORDER, STATUS_NAME;
  END sp_get_visit_actions;

  PROCEDURE sp_get_product_categories(p_cursor OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT CATEGORY_ID   AS "categoryId",
             CATEGORY_NAME AS "categoryName",
             DESCRIPTION   AS "description",
             CREATED_AT    AS "createdAt"
        FROM PRODUCT_CATEGORIES
       ORDER BY CATEGORY_ID;
  END sp_get_product_categories;

  FUNCTION fn_get_setting_value(p_setting_key IN VARCHAR2) RETURN VARCHAR2 IS
    v_value VARCHAR2(4000);
  BEGIN
    SELECT SETTING_VALUE
      INTO v_value
      FROM CATALOG_APP_SETTINGS
     WHERE SETTING_KEY = p_setting_key;
  
    RETURN v_value;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RETURN NULL;
    WHEN OTHERS THEN
      RETURN NULL;
  END fn_get_setting_value;

  FUNCTION fn_get_risk_status_by_days(p_days_overdue IN NUMBER)
    RETURN VARCHAR2 IS
    v_status VARCHAR2(50);
  BEGIN
    SELECT STATUS_CODE
      INTO v_status
      FROM CATALOG_RISK_STATUSES
     WHERE IS_ACTIVE = 1
       AND p_days_overdue >= MIN_DAYS
       AND p_days_overdue <= MAX_DAYS
     FETCH FIRST 1 ROWS ONLY;
  
    RETURN v_status;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      -- Si no hay coincidencia, devolver el estado crítico
      RETURN 'CRITICO';
    WHEN OTHERS THEN
      RETURN 'CRITICO';
  END fn_get_risk_status_by_days;
  --
  -- ========================================================================
  -- PROCEDIMIENTO DE TEMA POR ROL
  -- ========================================================================
  PROCEDURE sp_get_theme_by_role(p_role_id IN NUMBER,
                                  p_cursor  OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT pt.THEME_ID         AS "themeId",
             pt.ROLE_ID          AS "roleId",
             pt.THEME_NAME       AS "themeName",
             r.ROLE_NAME         AS "roleName",
             pt.PRIMARY_COLOR    AS "primaryColor",
             pt.SECONDARY_COLOR  AS "secondaryColor",
             pt.ACCENT_COLOR     AS "accentColor",
             pt.BACKGROUND_COLOR AS "backgroundColor",
             pt.TEXT_COLOR       AS "textColor",
             pt.TITLE_TEXT_COLOR AS "titleTextColor",
             pt.FORM_TEXT_COLOR  AS "formTextColor",
             pt.MENU_TEXT_COLOR  AS "menuTextColor",
             pt.ICON_NAME        AS "iconName"
        FROM PROFILE_THEMES pt
        JOIN ROLES r ON pt.ROLE_ID = r.ROLE_ID
       WHERE pt.ROLE_ID = p_role_id
         AND pt.IS_ACTIVE = 1
       FETCH FIRST 1 ROWS ONLY;
  EXCEPTION
    WHEN OTHERS THEN
      OPEN p_cursor FOR SELECT NULL FROM DUAL WHERE 1 = 0;
  END sp_get_theme_by_role;

  -- ========================================================================
  -- PROCEDIMIENTO DE PERMISOS POR USUARIO
  -- ========================================================================
  PROCEDURE sp_get_user_permissions(p_user_id IN NUMBER,
                                    p_cursor  OUT SYS_REFCURSOR) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT p.PERMISSION_NAME  AS "permissionName",
             p.MODULE           AS "module",
             p.DESCRIPTION      AS "description"
        FROM PERMISSIONS p
        JOIN ROLE_PERMISSIONS rp ON p.PERMISSION_ID = rp.PERMISSION_ID
        JOIN USERS u             ON u.ROLE_ID        = rp.ROLE_ID
       WHERE u.USER_ID = p_user_id
       ORDER BY p.MODULE, p.PERMISSION_NAME;
  EXCEPTION
    WHEN OTHERS THEN
      OPEN p_cursor FOR SELECT NULL, NULL, NULL FROM DUAL WHERE 1 = 0;
  END sp_get_user_permissions;

  -- ========================================================================
  -- MÓDULO DE RECURSOS HUMANOS (HR) - IMPLEMENTACIÓN
  -- ========================================================================

  PROCEDURE sp_register_employee(
    p_employee_id       OUT NUMBER,
    p_user_id           IN NUMBER,
    p_employee_code     IN VARCHAR2,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT 0,
    p_commission_rate   IN NUMBER DEFAULT 0,
    p_hire_date         IN DATE DEFAULT SYSDATE,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_created_by        IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    INSERT INTO employees (
      employee_id, user_id, employee_code, job_title, department,
      base_salary, commission_rate, hire_date, employment_status,
      bank_name, bank_account, created_by, created_at
    ) VALUES (
      seq_employees.NEXTVAL, p_user_id, p_employee_code, p_job_title, p_department,
      NVL(p_base_salary, 0), NVL(p_commission_rate, 0), NVL(p_hire_date, SYSDATE), 'ACTIVO',
      p_bank_name, p_bank_account, p_created_by, SYSTIMESTAMP
    ) RETURNING employee_id INTO p_employee_id;
    
    log_audit('EMPLOYEE', p_employee_id, 'INSERT', p_created_by, 
              'Empleado registrado: ' || p_employee_code);
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_employee;

  PROCEDURE sp_update_employee(
    p_employee_id       IN NUMBER,
    p_job_title         IN VARCHAR2 DEFAULT NULL,
    p_department        IN VARCHAR2 DEFAULT NULL,
    p_base_salary       IN NUMBER DEFAULT NULL,
    p_commission_rate   IN NUMBER DEFAULT NULL,
    p_employment_status IN VARCHAR2 DEFAULT NULL,
    p_bank_name         IN VARCHAR2 DEFAULT NULL,
    p_bank_account      IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    UPDATE employees
    SET job_title         = COALESCE(p_job_title, job_title),
        department        = COALESCE(p_department, department),
        base_salary       = COALESCE(p_base_salary, base_salary),
        commission_rate   = COALESCE(p_commission_rate, commission_rate),
        employment_status = COALESCE(p_employment_status, employment_status),
        bank_name         = COALESCE(p_bank_name, bank_name),
        bank_account      = COALESCE(p_bank_account, bank_account),
        updated_by        = p_updated_by,
        updated_at        = SYSTIMESTAMP
    WHERE employee_id = p_employee_id;
    
    log_audit('EMPLOYEE', p_employee_id, 'UPDATE', p_updated_by, 'Empleado actualizado');
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_update_employee;

  PROCEDURE sp_terminate_employee(
    p_employee_id       IN NUMBER,
    p_termination_date  IN DATE DEFAULT SYSDATE,
    p_reason            IN VARCHAR2 DEFAULT NULL,
    p_updated_by        IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    UPDATE employees
    SET employment_status = 'BAJA',
        termination_date  = NVL(p_termination_date, SYSDATE),
        notes             = NVL(notes, TO_CLOB('')) || TO_CLOB(CHR(10) || 
                           'BAJA: ' || TO_CHAR(SYSDATE, 'DD/MM/YYYY') || 
                           CASE WHEN p_reason IS NOT NULL THEN ' - ' || p_reason ELSE '' END),
        updated_by        = p_updated_by,
        updated_at        = SYSTIMESTAMP
    WHERE employee_id = p_employee_id;
    
    -- También desactivar el usuario
    UPDATE users SET is_active = '0' 
    WHERE user_id = (SELECT user_id FROM employees WHERE employee_id = p_employee_id);
    
    log_audit('EMPLOYEE', p_employee_id, 'TERMINATE', p_updated_by, p_reason);
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_terminate_employee;

  PROCEDURE sp_get_employees(
    p_status           IN VARCHAR2 DEFAULT NULL,
    p_department       IN VARCHAR2 DEFAULT NULL,
    p_search_term      IN VARCHAR2 DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        e.employee_id        AS "employeeId",
        e.employee_code      AS "employeeCode",
        e.user_id            AS "userId",
        u.username           AS "username",
        u.full_name          AS "fullName",
        u.email              AS "email",
        u.phone              AS "phone",
        r.role_name          AS "roleName",
        z.zone_name          AS "zoneName",
        e.job_title          AS "jobTitle",
        e.department         AS "department",
        e.base_salary        AS "baseSalary",
        e.commission_rate    AS "commissionRate",
        e.hire_date          AS "hireDate",
        e.termination_date   AS "terminationDate",
        e.employment_status  AS "employmentStatus",
        TRUNC(MONTHS_BETWEEN(SYSDATE, e.hire_date) / 12) AS "yearsOfService",
        MOD(TRUNC(MONTHS_BETWEEN(SYSDATE, e.hire_date)), 12) AS "monthsOfService",
        e.bank_name          AS "bankName",
        e.bank_account       AS "bankAccount",
        e.emergency_contact_1 AS "emergencyContact1",
        e.emergency_phone_1   AS "emergencyPhone1"
      FROM employees e
      JOIN users u ON e.user_id = u.user_id
      JOIN roles r ON u.role_id = r.role_id
      LEFT JOIN zones z ON u.zone_id = z.zone_id
      WHERE (p_status IS NULL OR e.employment_status = p_status)
        AND (p_department IS NULL OR UPPER(e.department) = UPPER(p_department))
        AND (p_search_term IS NULL OR 
             UPPER(u.full_name) LIKE '%' || UPPER(p_search_term) || '%' OR
             UPPER(e.employee_code) LIKE '%' || UPPER(p_search_term) || '%')
      ORDER BY u.full_name;
  END sp_get_employees;

  PROCEDURE sp_get_employee_by_id(
    p_employee_id      IN NUMBER,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        e.employee_id, e.employee_code, e.user_id, u.username, u.full_name,
        u.email, u.phone, r.role_name, z.zone_name,
        e.job_title, e.department, e.base_salary, e.commission_rate,
        e.hire_date, e.termination_date, e.employment_status,
        TRUNC(MONTHS_BETWEEN(SYSDATE, e.hire_date) / 12) AS years_of_service,
        e.bank_name, e.bank_account, e.clabe,
        e.emergency_contact_1, e.emergency_phone_1, e.emergency_relation_1,
        e.emergency_contact_2, e.emergency_phone_2, e.emergency_relation_2,
        e.beneficiary_name, e.beneficiary_relation,
        e.address, e.gps_latitude, e.gps_longitude, e.notes
      FROM employees e
      JOIN users u ON e.user_id = u.user_id
      JOIN roles r ON u.role_id = r.role_id
      LEFT JOIN zones z ON u.zone_id = z.zone_id
      WHERE e.employee_id = p_employee_id;
  END sp_get_employee_by_id;

  PROCEDURE sp_register_attendance(
    p_attendance_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_attendance_date  IN DATE DEFAULT SYSDATE,
    p_attendance_type  IN VARCHAR2,
    p_check_in_time    IN TIMESTAMP DEFAULT SYSTIMESTAMP,
    p_check_out_time   IN TIMESTAMP DEFAULT NULL,
    p_gps_lat          IN NUMBER DEFAULT NULL,
    p_gps_lon          IN NUMBER DEFAULT NULL,
    p_notes            IN VARCHAR2 DEFAULT NULL
  ) IS
    v_worked_hours NUMBER;
  BEGIN
    v_worked_hours := fn_calculate_worked_hours(p_check_in_time, p_check_out_time);
    
    INSERT INTO employee_attendance (
      attendance_id, employee_id, attendance_date, attendance_type,
      check_in_time, check_out_time, worked_hours,
      gps_latitude, gps_longitude, notes, created_at
    ) VALUES (
      seq_employee_attendance.NEXTVAL, p_employee_id, 
      TRUNC(NVL(p_attendance_date, SYSDATE)), p_attendance_type,
      p_check_in_time, p_check_out_time, v_worked_hours,
      p_gps_lat, p_gps_lon, p_notes, SYSTIMESTAMP
    ) RETURNING attendance_id INTO p_attendance_id;
    
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_register_attendance;

  PROCEDURE sp_get_attendance_by_employee(
    p_employee_id      IN NUMBER,
    p_start_date       IN DATE DEFAULT NULL,
    p_end_date         IN DATE DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        attendance_id       AS "attendanceId",
        employee_id         AS "employeeId",
        attendance_date     AS "attendanceDate",
        attendance_type     AS "attendanceType",
        check_in_time       AS "checkInTime",
        check_out_time      AS "checkOutTime",
        worked_hours        AS "workedHours",
        notes               AS "notes"
      FROM employee_attendance
      WHERE employee_id = p_employee_id
        AND (p_start_date IS NULL OR attendance_date >= p_start_date)
        AND (p_end_date IS NULL OR attendance_date <= p_end_date)
      ORDER BY attendance_date DESC;
  END sp_get_attendance_by_employee;

  PROCEDURE sp_get_attendance_summary(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT EXTRACT(YEAR FROM SYSDATE),
    p_month            IN NUMBER DEFAULT EXTRACT(MONTH FROM SYSDATE),
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        COUNT(CASE WHEN attendance_type = 'ASISTENCIA' THEN 1 END) AS "asistencias",
        COUNT(CASE WHEN attendance_type = 'RETARDO' THEN 1 END) AS "retardos",
        COUNT(CASE WHEN attendance_type = 'FALTA' THEN 1 END) AS "faltas",
        COUNT(CASE WHEN attendance_type = 'FALTA_JUSTIFICADA' THEN 1 END) AS "faltasJustificadas",
        SUM(NVL(worked_hours, 0)) AS "totalHoras"
      FROM employee_attendance
      WHERE employee_id = p_employee_id
        AND EXTRACT(YEAR FROM attendance_date) = p_year
        AND EXTRACT(MONTH FROM attendance_date) = p_month;
  END sp_get_attendance_summary;

  PROCEDURE sp_generate_payroll(
    p_payroll_id       OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE,
    p_created_by       IN NUMBER DEFAULT NULL
  ) IS
    v_base_salary      NUMBER;
    v_commission_rate  NUMBER;
    v_commissions      NUMBER := 0;
    v_days_worked      NUMBER := 0;
    v_absences_count   NUMBER := 0;
    v_late_count       NUMBER := 0;
    v_sales_count      NUMBER := 0;
    v_sales_amount     NUMBER := 0;
    v_collections_count NUMBER := 0;
    v_collections_amount NUMBER := 0;
    v_gross_total      NUMBER;
    v_net_total        NUMBER;
  BEGIN
    SELECT base_salary, commission_rate
    INTO v_base_salary, v_commission_rate
    FROM employees
    WHERE employee_id = p_employee_id;
    
    SELECT 
      COUNT(CASE WHEN attendance_type IN ('ASISTENCIA', 'RETARDO') THEN 1 END),
      COUNT(CASE WHEN attendance_type = 'FALTA' THEN 1 END),
      COUNT(CASE WHEN attendance_type = 'RETARDO' THEN 1 END)
    INTO v_days_worked, v_absences_count, v_late_count
    FROM employee_attendance
    WHERE employee_id = p_employee_id
      AND attendance_date BETWEEN p_period_start AND p_period_end;
    
    v_commissions := fn_get_employee_total_commissions(p_employee_id, p_period_start, p_period_end);
    
    v_gross_total := v_base_salary + v_commissions;
    v_net_total   := v_gross_total;
    
    INSERT INTO employee_payroll (
      payroll_id, employee_id, period_start, period_end,
      base_salary, commissions, gross_total, net_total,
      days_worked, absences_count, late_count,
      sales_count, collections_count,
      status, created_by, created_at
    ) VALUES (
      seq_employee_payroll.NEXTVAL, p_employee_id, p_period_start, p_period_end,
      v_base_salary, v_commissions, v_gross_total, v_net_total,
      v_days_worked, v_absences_count, v_late_count,
      v_sales_count, v_collections_count,
      'PENDIENTE', p_created_by, SYSTIMESTAMP
    ) RETURNING payroll_id INTO p_payroll_id;
    
    UPDATE employee_commissions
    SET payroll_id = p_payroll_id,
        status = 'PROCESADA',
        processed_at = SYSTIMESTAMP
    WHERE employee_id = p_employee_id
      AND status = 'PENDIENTE'
      AND reference_date BETWEEN p_period_start AND p_period_end;
    
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_generate_payroll;

  PROCEDURE sp_approve_payroll(
    p_payroll_id       IN NUMBER,
    p_approved_by      IN NUMBER
  ) IS
  BEGIN
    UPDATE employee_payroll
    SET status = 'APROBADA',
        approved_by = p_approved_by,
        approved_at = SYSTIMESTAMP
    WHERE payroll_id = p_payroll_id;
    
    COMMIT;
  END sp_approve_payroll;

  PROCEDURE sp_pay_payroll(
    p_payroll_id       IN NUMBER,
    p_payment_date     IN DATE DEFAULT SYSDATE,
    p_payment_method   IN VARCHAR2,
    p_payment_ref      IN VARCHAR2 DEFAULT NULL,
    p_paid_by          IN NUMBER
  ) IS
  BEGIN
    UPDATE employee_payroll
    SET status = 'PAGADA',
        payment_date = NVL(p_payment_date, SYSDATE),
        payment_method = p_payment_method,
        payment_reference = p_payment_ref,
        paid_by = p_paid_by,
        paid_at = SYSTIMESTAMP
    WHERE payroll_id = p_payroll_id;
    
    UPDATE employee_commissions
    SET status = 'PAGADA'
    WHERE payroll_id = p_payroll_id;
    
    COMMIT;
  END sp_pay_payroll;

  PROCEDURE sp_get_payroll_by_employee(
    p_employee_id      IN NUMBER,
    p_year             IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        payroll_id AS "payrollId",
        period_start AS "periodStart",
        period_end AS "periodEnd",
        payment_date AS "paymentDate",
        base_salary AS "baseSalary",
        commissions AS "commissions",
        gross_total AS "grossTotal",
        net_total AS "netTotal",
        days_worked AS "daysWorked",
        absences_count AS "absencesCount",
        late_count AS "lateCount",
        status AS "status"
      FROM employee_payroll
      WHERE employee_id = p_employee_id
        AND (p_year IS NULL OR EXTRACT(YEAR FROM period_start) = p_year)
      ORDER BY period_start DESC;
  END sp_get_payroll_by_employee;

  PROCEDURE sp_register_commission_from_sale(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_sale_id          IN NUMBER,
    p_commission_rate  IN NUMBER
  ) IS
    v_base_amount NUMBER;
    v_sale_date DATE;
  BEGIN
    SELECT total_amount, sale_date 
    INTO v_base_amount, v_sale_date
    FROM sales WHERE sale_id = p_sale_id;
    
    INSERT INTO employee_commissions (
      commission_id, employee_id, commission_type, reference_id,
      reference_date, base_amount, commission_rate, commission_amount,
      status, created_at
    ) VALUES (
      seq_employee_commissions.NEXTVAL, p_employee_id, 'VENTA', p_sale_id,
      v_sale_date, v_base_amount, p_commission_rate, 
      v_base_amount * (p_commission_rate / 100),
      'PENDIENTE', SYSTIMESTAMP
    ) RETURNING commission_id INTO p_commission_id;
    
    COMMIT;
  END sp_register_commission_from_sale;

  PROCEDURE sp_register_commission_from_payment(
    p_commission_id    OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_payment_id       IN NUMBER,
    p_commission_rate  IN NUMBER
  ) IS
    v_base_amount NUMBER;
    v_payment_date DATE;
  BEGIN
    SELECT amount, payment_date
    INTO v_base_amount, v_payment_date
    FROM payments WHERE payment_id = p_payment_id;
    
    INSERT INTO employee_commissions (
      commission_id, employee_id, commission_type, reference_id,
      reference_date, base_amount, commission_rate, commission_amount,
      status, created_at
    ) VALUES (
      seq_employee_commissions.NEXTVAL, p_employee_id, 'COBRO', p_payment_id,
      v_payment_date, v_base_amount, p_commission_rate,
      v_base_amount * (p_commission_rate / 100),
      'PENDIENTE', SYSTIMESTAMP
    ) RETURNING commission_id INTO p_commission_id;
    
    COMMIT;
  END sp_register_commission_from_payment;

  PROCEDURE sp_get_commissions_pending(
    p_employee_id      IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        commission_id AS "commissionId",
        employee_id AS "employeeId",
        commission_type AS "commissionType",
        reference_id AS "referenceId",
        reference_date AS "referenceDate",
        base_amount AS "baseAmount",
        commission_rate AS "commissionRate",
        commission_amount AS "commissionAmount",
        status AS "status"
      FROM employee_commissions
      WHERE status = 'PENDIENTE'
        AND (p_employee_id IS NULL OR employee_id = p_employee_id)
      ORDER BY reference_date;
  END sp_get_commissions_pending;

  FUNCTION fn_get_employee_seniority_years(p_employee_id IN NUMBER) 
    RETURN NUMBER IS
    v_years NUMBER;
  BEGIN
    SELECT TRUNC(MONTHS_BETWEEN(SYSDATE, hire_date) / 12)
    INTO v_years
    FROM employees
    WHERE employee_id = p_employee_id;
    
    RETURN v_years;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_get_employee_seniority_years;

  FUNCTION fn_get_employee_total_commissions(
    p_employee_id      IN NUMBER,
    p_period_start     IN DATE,
    p_period_end       IN DATE
  ) RETURN NUMBER IS
    v_total NUMBER;
  BEGIN
    SELECT NVL(SUM(commission_amount), 0)
    INTO v_total
    FROM employee_commissions
    WHERE employee_id = p_employee_id
      AND status = 'PENDIENTE'
      AND reference_date BETWEEN p_period_start AND p_period_end;
    
    RETURN v_total;
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_get_employee_total_commissions;

  FUNCTION fn_calculate_worked_hours(
    p_check_in         IN TIMESTAMP,
    p_check_out        IN TIMESTAMP
  ) RETURN NUMBER IS
    v_hours NUMBER;
  BEGIN
    IF p_check_in IS NULL OR p_check_out IS NULL THEN
      RETURN 0;
    END IF;
    
    v_hours := EXTRACT(DAY FROM (p_check_out - p_check_in)) * 24 +
               EXTRACT(HOUR FROM (p_check_out - p_check_in)) +
               EXTRACT(MINUTE FROM (p_check_out - p_check_in)) / 60;
    
    RETURN ROUND(v_hours, 2);
  EXCEPTION
    WHEN OTHERS THEN
      RETURN 0;
  END fn_calculate_worked_hours;

  -- ========================================================================
  -- GESTIÓN DE DOCUMENTOS DE EMPLEADOS
  -- ========================================================================

  PROCEDURE sp_upload_employee_document(
    p_document_id      OUT NUMBER,
    p_employee_id      IN NUMBER,
    p_doc_type_id      IN NUMBER,
    p_file_name        IN VARCHAR2,
    p_file_path        IN VARCHAR2,
    p_file_size        IN NUMBER,
    p_mime_type        IN VARCHAR2 DEFAULT 'application/pdf',
    p_expiration_date  IN DATE DEFAULT NULL,
    p_notes            IN VARCHAR2 DEFAULT NULL,
    p_uploaded_by      IN NUMBER DEFAULT NULL
  ) IS
  BEGIN
    INSERT INTO employees_documents (
      document_id, employee_id, doc_type_id, file_name, file_path,
      file_size, mime_type, upload_date, expiration_date, notes,
      uploaded_by
    ) VALUES (
      seq_employee_documents.NEXTVAL, p_employee_id, p_doc_type_id, p_file_name, p_file_path,
      p_file_size, p_mime_type, SYSDATE, p_expiration_date, p_notes,
      p_uploaded_by
    ) RETURNING document_id INTO p_document_id;
    
    log_audit('EMPLOYEE_DOCUMENT', p_document_id, 'INSERT', p_uploaded_by, 
              'Documento subido: ' || p_file_name);
    COMMIT;
  EXCEPTION
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_upload_employee_document;

  PROCEDURE sp_get_employee_documents(
    p_employee_id      IN NUMBER,
    p_doc_type_id      IN NUMBER DEFAULT NULL,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        d.document_id,
        d.employee_id,
        d.doc_type_id,
        dt.doc_type_code,
        dt.doc_type_name,
        dt.description AS doc_type_description,
        dt.is_required,
        d.file_name,
        d.file_path,
        d.file_size,
        d.mime_type,
        d.upload_date,
        d.expiration_date,
        CASE 
          WHEN d.expiration_date IS NOT NULL AND d.expiration_date < SYSDATE THEN 'VENCIDO'
          WHEN d.expiration_date IS NOT NULL AND d.expiration_date < ADD_MONTHS(SYSDATE, 1) THEN 'POR_VENCER'
          ELSE 'VIGENTE'
        END AS status,
        d.notes,
        d.uploaded_by,
        u.full_name AS uploaded_by_name,
        d.upload_date AS upload_timestamp
      FROM employees_documents d
      INNER JOIN catalog_employee_doc_types dt ON d.doc_type_id = dt.doc_type_id
      LEFT JOIN users u ON d.uploaded_by = u.user_id
      WHERE d.employee_id = p_employee_id
        AND (p_doc_type_id IS NULL OR d.doc_type_id = p_doc_type_id)
      ORDER BY dt.display_order, d.upload_date DESC;
  END sp_get_employee_documents;

  PROCEDURE sp_get_document_by_id(
    p_document_id      IN NUMBER,
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        d.document_id,
        d.employee_id,
        d.doc_type_id,
        dt.doc_type_code,
        dt.doc_type_name,
        d.file_name,
        d.file_path,
        d.file_size,
        d.mime_type,
        d.upload_date,
        d.expiration_date,
        d.notes,
        d.uploaded_by,
        u.full_name AS uploaded_by_name,
        d.upload_date AS upload_timestamp
      FROM employees_documents d
      INNER JOIN catalog_employee_doc_types dt ON d.doc_type_id = dt.doc_type_id
      LEFT JOIN users u ON d.uploaded_by = u.user_id
      WHERE d.document_id = p_document_id;
  END sp_get_document_by_id;

  PROCEDURE sp_delete_employee_document(
    p_document_id      IN NUMBER,
    p_deleted_by       IN NUMBER DEFAULT NULL
  ) IS
    v_file_name VARCHAR2(500);
  BEGIN
    SELECT file_name INTO v_file_name
    FROM employees_documents
    WHERE document_id = p_document_id;
    
    DELETE FROM employees_documents
    WHERE document_id = p_document_id;
    
    log_audit('EMPLOYEE_DOCUMENT', p_document_id, 'DELETE', p_deleted_by, 
              'Documento eliminado: ' || v_file_name);
    COMMIT;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      RAISE_APPLICATION_ERROR(-20001, 'Documento no encontrado');
    WHEN OTHERS THEN
      ROLLBACK;
      RAISE;
  END sp_delete_employee_document;

  PROCEDURE sp_get_document_types(
    p_cursor           OUT SYS_REFCURSOR
  ) IS
  BEGIN
    OPEN p_cursor FOR
      SELECT 
        doc_type_id,
        doc_type_code,
        doc_type_name,
        description,
        is_required,
        display_order
      FROM catalog_employee_doc_types
      ORDER BY display_order;
  END sp_get_document_types;
  --
END EM_VITARAIZ_AD;
/
