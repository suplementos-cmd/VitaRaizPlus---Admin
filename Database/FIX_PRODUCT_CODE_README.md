# 🔧 FIX: Generación Automática de PRODUCT_CODE

## 📋 Problema Identificado

Al intentar crear productos desde la aplicación web, se producía el siguiente error:

```
ORA-01400: no se puede realizar una inserción NULL en ("SALESAPP"."PRODUCTS"."PRODUCT_CODE")
```

**Causa:** La columna `PRODUCT_CODE` en la tabla `PRODUCTS` es `NOT NULL` y tiene constraint `UNIQUE`, pero el procedimiento almacenado `sp_register_product` no estaba generando este valor automáticamente.

## ✅ Solución Implementada

Se modificó el procedimiento `sp_register_product` en el package `EM_VITARAIZ_AD` para:

1. **Generar automáticamente** el `PRODUCT_CODE` usando la secuencia existente
2. **Formato:** `PROD-000001`, `PROD-000002`, etc. (6 dígitos con padding de ceros)
3. **Garantizar unicidad** usando la misma secuencia que `PRODUCT_ID`

### Cambios Realizados

**Antes:**
```sql
INSERT INTO products (product_id, product_name, ...)
VALUES (seq_products.NEXTVAL, p_name, ...)
RETURNING product_id INTO p_product_id;
```

**Después:**
```sql
-- Obtener ID de secuencia
SELECT seq_products.NEXTVAL INTO v_product_id FROM DUAL;

-- Generar código automático
v_product_code := 'PROD-' || LPAD(v_product_id, 6, '0');

-- Insertar con código generado
INSERT INTO products (product_id, product_code, product_name, ...)
VALUES (v_product_id, v_product_code, p_name, ...);
```

## 🚀 Cómo Aplicar el Fix

### Opción 1: Script PowerShell Automatizado (Recomendado)

```powershell
cd c:\Projects\VitaRaizSalesApp\Database
.\_compilar_package_fix.ps1
```

Este script:
- ✅ Conecta a Oracle
- ✅ Compila el package actualizado
- ✅ Verifica que la compilación fue exitosa
- ✅ Muestra el estado final

### Opción 2: SQL*Plus Manual

```bash
cd c:\Projects\VitaRaizSalesApp\Database
sqlplus SALESAPP/yourpassword@XEPDB1

SQL> @EM_VITARAIZ_AD.pck
SQL> SELECT object_name, object_type, status FROM user_objects WHERE object_name = 'EM_VITARAIZ_AD';
```

### Opción 3: SQL Developer

1. Abrir SQL Developer
2. Conectar como usuario `SALESAPP`
3. Abrir archivo: `Database/EM_VITARAIZ_AD.pck`
4. Ejecutar script completo (F5)
5. Verificar que no hay errores de compilación

## 🧪 Verificación

### 1. Verificar Estado del Package

```sql
SELECT object_name, object_type, status, last_ddl_time
FROM user_objects
WHERE object_name = 'EM_VITARAIZ_AD'
ORDER BY object_type;
```

**Resultado esperado:**
```
OBJECT_NAME      OBJECT_TYPE    STATUS   LAST_DDL_TIME
EM_VITARAIZ_AD   PACKAGE        VALID    2026-04-08 21:XX:XX
EM_VITARAIZ_AD   PACKAGE BODY   VALID    2026-04-08 21:XX:XX
```

### 2. Prueba de Creación de Producto

```sql
DECLARE
  v_product_id NUMBER;
BEGIN
  SALESAPP.EM_VITARAIZ_AD.sp_register_product(
    p_product_id  => v_product_id,
    p_name        => 'Producto de Prueba',
    p_description => 'Prueba de código automático',
    p_unit_price  => 100,
    p_stock       => 10,
    p_category    => 'Papelería'
  );
  
  DBMS_OUTPUT.PUT_LINE('✅ Producto creado con ID: ' || v_product_id);
  
  FOR rec IN (SELECT product_id, product_code, product_name 
              FROM SALESAPP.PRODUCTS 
              WHERE product_id = v_product_id) LOOP
    DBMS_OUTPUT.PUT_LINE('📦 Código generado: ' || rec.product_code);
  END LOOP;
  
  ROLLBACK; -- Deshacer la prueba
END;
/
```

### 3. Prueba desde la Aplicación Web

1. Iniciar la aplicación: `cd VitaRaiz.API && dotnet run`
2. Navegar a: http://localhost:5299/admin/papeleria (o cualquier categoría)
3. Clic en "Nuevo Producto"
4. Llenar formulario y guardar
5. **✅ Debe guardarse sin errores**
6. Verificar en la BD que el `PRODUCT_CODE` se generó automáticamente

## 📊 Ejemplos de Códigos Generados

| PRODUCT_ID | PRODUCT_CODE | PRODUCT_NAME |
|------------|--------------|--------------|
| 1          | PROD-000001  | Cuaderno A4  |
| 2          | PROD-000002  | Lapicero Azul |
| 150        | PROD-000150  | Uniforme XL  |
| 9999       | PROD-009999  | Kit Premium  |

## 📁 Archivos Modificados

- ✅ `Database/EM_VITARAIZ_AD.pck` - Package actualizado
- ✅ `Database/EM_VITARAIZ_AD.~pck` - Backup actualizado
- ✅ `Database/VitaRaiz_Migration/SALESAPP_11_FIX_PRODUCT_CODE.sql` - Script de migración
- ✅ `Database/_INSTRUCCIONES_FIX_PRODUCT_CODE.sql` - Instrucciones detalladas
- ✅ `Database/_compilar_package_fix.ps1` - Script automatizado

## ⚠️ Notas Importantes

1. **No se requieren cambios en el código C#** - El API ya funcionará correctamente después de compilar el package
2. **Compatible con productos existentes** - Los productos ya creados mantienen sus códigos actuales
3. **Formato consistente** - Todos los nuevos productos seguirán el formato `PROD-XXXXXX`
4. **Unicidad garantizada** - Al usar la secuencia, no habrá códigos duplicados

## 🎯 Próximos Pasos

Después de aplicar el fix:

1. ✅ Reiniciar la aplicación API (si está corriendo)
2. ✅ Probar creación de productos en todas las categorías:
   - Papelería (`/admin/papeleria`)
   - Uniformes (`/admin/uniformes`)
   - Muestras (`/admin/muestras`)
   - Kits de Ventas (`/admin/kits-ventas`)
   - Kits de Cobros (`/admin/kits-cobros`)
3. ✅ Verificar que los códigos se generan correctamente

## 📞 Soporte

Si después de aplicar el fix persisten errores:

1. Verificar logs de la API en `VitaRaiz.API/logs/`
2. Verificar estado del package en Oracle
3. Revisar permisos del usuario SALESAPP sobre la secuencia `SEQ_PRODUCTS`

---

**Fecha de implementación:** 8 de abril de 2026  
**Versión:** 1.0  
**Estado:** ✅ Listo para aplicar
