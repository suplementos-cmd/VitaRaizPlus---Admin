# 🌐 API REFERENCE - MÓDULO HR

## Base URL
```
http://localhost:5000/api/employees
```

---

## 👥 GESTIÓN DE EMPLEADOS

### **GET** `/api/employees`
Lista todos los empleados con filtros opcionales

**Query Params:**
```
?status=ACTIVO         // ACTIVO, BAJA, SUSPENDIDO
?department=Ventas     // Filtrar por departamento
?search=Juan          // Buscar por nombre o código
```

**Response 200:**
```json
[
  {
    "employeeId": 1,
    "employeeCode": "EMP001",
    "userId": 10,
    "username": "juan.perez",
    "fullName": "Juan Pérez García",
    "email": "juan@vitaraiz.com",
    "phone": "5512345678",
    "roleName": "Vendedor",
    "zoneName": "Zona Norte",
    "jobTitle": "Vendedor Senior",
    "department": "Ventas",
    "baseSalary": 8000.00,
    "commissionRate": 3.00,
    "hireDate": "2024-01-15T00:00:00",
    "terminationDate": null,
    "employmentStatus": "ACTIVO",
    "yearsOfService": 0,
    "monthsOfService": 3,
    "bankName": "BBVA",
    "bankAccount": "012345678901234567",
    "emergencyContact1": "María Pérez",
    "emergencyPhone1": "5587654321"
  }
]
```

**Roles:** AdminFull, Admin, Supervisor

---

### **GET** `/api/employees/{id}`
Obtiene los detalles completos de un empleado

**Response 200:**
```json
{
  "employeeId": 1,
  "employeeCode": "EMP001",
  "fullName": "Juan Pérez García",
  "baseSalary": 8000.00,
  "commissionRate": 3.00,
  "hireDate": "2024-01-15",
  "address": "Calle Principal #123, Col. Centro",
  "gpsLatitude": 19.432608,
  "gpsLongitude": -99.133209,
  "emergencyContact1": "María Pérez",
  "emergencyPhone1": "5587654321",
  "emergencyRelation1": "ESPOSA",
  "emergencyContact2": "Pedro Pérez",
  "emergencyPhone2": "5587654322",
  "emergencyRelation2": "HERMANO",
  "beneficiaryName": "María Pérez",
  "beneficiaryRelation": "ESPOSA",
  "notes": "Empleado destacado del mes"
}
```

**Roles:** Todos (solo pueden ver su propia info excepto Admin)

---

### **POST** `/api/employees`
Crea un nuevo empleado

**Request Body:**
```json
{
  "userId": 10,
  "employeeCode": "EMP015",
  "jobTitle": "Vendedor",
  "department": "Ventas",
  "baseSalary": 8000.00,
  "commissionRate": 3.00,
  "hireDate": "2024-04-09",
  "bankName": "BBVA",
  "bankAccount": "012345678901234567",
  "clabe": "012180001234567890",
  "emergencyContact1": "María López",
  "emergencyPhone1": "5512345678",
  "emergencyRelation1": "ESPOSA",
  "address": "Calle Ejemplo #456"
}
```

**Response 201:**
```json
{
  "employeeId": 15
}
```

**Roles:** AdminFull, Admin

---

### **PUT** `/api/employees/{id}`
Actualiza datos de un empleado

**Request Body:** (todos los campos opcionales)
```json
{
  "jobTitle": "Vendedor Senior",
  "baseSalary": 9000.00,
  "commissionRate": 4.00,
  "bankAccount": "012345678901234568",
  "employmentStatus": "ACTIVO"
}
```

**Response 204:** No Content

**Roles:** AdminFull, Admin

---

### **DELETE** `/api/employees/{id}`
Da de baja a un empleado (soft delete)

**Request Body:**
```json
{
  "terminationDate": "2024-04-30",
  "reason": "Renuncia voluntaria"
}
```

**Response 204:** No Content

**Roles:** AdminFull

---

## 📅 CONTROL DE ASISTENCIAS

### **GET** `/api/employees/{id}/attendance`
Obtiene las asistencias de un empleado

**Query Params:**
```
?startDate=2024-04-01
?endDate=2024-04-30
```

**Response 200:**
```json
[
  {
    "attendanceId": 100,
    "employeeId": 1,
    "employeeName": "Juan Pérez",
    "attendanceDate": "2024-04-09",
    "attendanceType": "ASISTENCIA",
    "checkInTime": "2024-04-09T08:05:00",
    "checkOutTime": "2024-04-09T18:10:00",
    "workedHours": 10.08,
    "notes": null
  },
  {
    "attendanceId": 101,
    "employeeId": 1,
    "attendanceDate": "2024-04-08",
    "attendanceType": "RETARDO",
    "checkInTime": "2024-04-08T08:35:00",
    "checkOutTime": "2024-04-08T18:00:00",
    "workedHours": 9.42,
    "notes": "Tráfico en vía principal"
  }
]
```

**Roles:** Admin, Supervisor (empleado solo ve las suyas)

---

### **POST** `/api/employees/{id}/attendance`
Registra asistencia (check-in/check-out)

**Request Body:**
```json
{
  "attendanceDate": "2024-04-09",
  "attendanceType": "ASISTENCIA",
  "checkInTime": "2024-04-09T08:05:00",
  "checkOutTime": "2024-04-09T18:10:00",
  "gpsLatitude": 19.432608,
  "gpsLongitude": -99.133209,
  "notes": "Registro desde app móvil"
}
```

**Tipos válidos:**
- `ASISTENCIA` - Llegó a tiempo
- `RETARDO` - Llegó tarde
- `FALTA` - No asistió (registrado por admin)
- `FALTA_JUSTIFICADA` - Con justificación  
- `PERMISO` - Permiso autorizado
- `VACACIONES` - Vacaciones
- `INCAPACIDAD` - Incapacidad médica

**Response 200:**
```json
{
  "attendanceId": 102
}
```

**Roles:** Todos (para sí mismos), Admin (para cualquiera)

---

### **GET** `/api/employees/{id}/attendance/summary`
Resumen de asistencias del mes

**Query Params:**
```
?year=2024
?month=4
```

**Response 200:**
```json
{
  "employeeId": 1,
  "employeeName": "Juan Pérez",
  "asistencias": 18,
  "retardos": 2,
  "faltas": 0,
  "faltasJustificadas": 1,
  "totalHoras": 180.50
}
```

**Roles:** Admin, Supervisor

---

## 💰 NÓMINA

### **GET** `/api/employees/{id}/payroll`
Obtiene las nóminas de un empleado

**Query Params:**
```
?year=2024
```

**Response 200:**
```json
[
  {
    "payrollId": 50,
    "employeeId": 1,
    "employeeName": "Juan Pérez",
    "periodStart": "2024-04-01",
    "periodEnd": "2024-04-15",
    "paymentDate": "2024-04-17",
    "baseSalary": 4000.00,
    "commissions": 1200.00,
    "bonuses": 0,
    "deductions": 100.00,
    "grossTotal": 5200.00,
    "netTotal": 5100.00,
    "daysWorked": 10,
    "absencesCount": 0,
    "lateCount": 1,
    "salesCount": 4,
    "salesAmount": 40000.00,
    "collectionsCount": 0,
    "collectionsAmount": 0,
    "status": "PAGADA",
    "paymentMethod": "TRANSFERENCIA",
    "paymentReference": "TRANSFER-2024-04-17-001"
  }
]
```

**Roles:** Admin (para todos), Empleado (solo las suyas)

---

### **POST** `/api/employees/{id}/payroll`
Genera una nómina para un empleado

**Request Body:**
```json
{
  "periodStart": "2024-04-01",
  "periodEnd": "2024-04-15"
}
```

**Response 200:**
```json
{
  "payrollId": 51,
  "grossTotal": 5200.00,
  "netTotal": 5100.00,
  "status": "PENDIENTE"
}
```

**Proceso automático:**
1. Calcula días trabajados (de EMPLOYEE_ATTENDANCE)
2. Suma comisiones pendientes del periodo
3. Calcula deducciones por faltas
4. Genera totales bruto y neto
5. Estado inicial: PENDIENTE

**Roles:** AdminFull, Admin

---

### **PUT** `/api/employees/payroll/{payrollId}/approve`
Aprueba una nómina pendiente

**Response 204:** No Content

**Workflow:** PENDIENTE → **APROBADA**

**Roles:** AdminFull

---

### **PUT** `/api/employees/payroll/{payrollId}/pay`
Marca una nómina como pagada

**Request Body:**
```json
{
  "paymentDate": "2024-04-17",
  "paymentMethod": "TRANSFERENCIA",
  "paymentReference": "TRANSFER-2024-04-17-001"
}
```

**Métodos válidos:**
- `TRANSFERENCIA`
- `EFECTIVO`
- `CHEQUE`

**Response 204:** No Content

**Workflow:** APROBADA → **PAGADA**

**Proceso automático:**
- Marca todas las comisiones asociadas como PAGADA
- Registra fecha y referencia de pago

**Roles:** AdminFull

---

### **POST** `/api/employees/payroll/batch`
Genera nóminas para múltiples empleados

**Request Body:**
```json
{
  "employeeIds": [1, 2, 3, 4, 5],
  "periodStart": "2024-04-01",
  "periodEnd": "2024-04-15"
}
```

**Response 200:**
```json
{
  "success": 5,
  "failed": 0,
  "payrollIds": [51, 52, 53, 54, 55]
}
```

**Roles:** AdminFull

---

## 💵 COMISIONES

### **GET** `/api/employees/{id}/commissions`
Obtiene las comisiones de un empleado

**Query Params:**
```
?status=PENDIENTE     // PENDIENTE, PROCESADA, PAGADA
?startDate=2024-04-01
?endDate=2024-04-30
```

**Response 200:**
```json
[
  {
    "commissionId": 200,
    "employeeId": 1,
    "employeeName": "Juan Pérez",
    "commissionType": "VENTA",
    "referenceId": 150,
    "referenceDate": "2024-04-05",
    "baseAmount": 10000.00,
    "commissionRate": 3.00,
    "commissionAmount": 300.00,
    "status": "PAGADA"
  },
  {
    "commissionId": 201,
    "commissionType": "COBRO",
    "referenceId": 580,
    "referenceDate": "2024-04-08",
    "baseAmount": 5000.00,
    "commissionRate": 2.00,
    "commissionAmount": 100.00,
    "status": "PROCESADA"
  }
]
```

**Roles:** Admin (para todos), Empleado (solo las suyas)

---

### **GET** `/api/employees/commissions/pending`
Lista todas las comisiones pendientes

**Query Params:**
```
?employeeId=1         // Filtrar por empleado
```

**Response 200:**
```json
[
  {
    "commissionId": 202,
    "employeeId": 1,
    "employeeName": "Juan Pérez",
    "commissionType": "VENTA",
    "referenceId": 155,
    "referenceDate": "2024-04-09",
    "baseAmount": 15000.00,
    "commissionRate": 3.00,
    "commissionAmount": 450.00,
    "status": "PENDIENTE"
  }
]
```

**Roles:** AdminFull, Admin

---

### **POST** `/api/employees/{id}/commissions/from-sale`
Registra comisión por venta (automático en SalesController)

**Request Body:**
```json
{
  "saleId": 155,
  "commissionRate": 3.00
}
```

**Response 200:**
```json
{
  "commissionId": 202,
  "commissionAmount": 450.00
}
```

**Roles:** Sistema (automático), Admin (manual)

---

### **POST** `/api/employees/{id}/commissions/from-payment`
Registra comisión por cobro (automático en PaymentsController)

**Request Body:**
```json
{
  "paymentId": 580,
  "commissionRate": 2.00
}
```

**Response 200:**
```json
{
  "commissionId": 203,
  "commissionAmount": 100.00
}
```

**Roles:** Sistema (automático), Admin (manual)

---

## 📊 REPORTES Y ESTADÍSTICAS

### **GET** `/api/employees/stats/dashboard`
KPIs del dashboard de RRHH

**Response 200:**
```json
{
  "totalEmployees": 25,
  "activeEmployees": 22,
  "newHiresThisMonth": 2,
  "terminationsThisMonth": 0,
  "attendanceToday": {
    "present": 20,
    "late": 2,
    "absent": 0
  },
  "payrollPending": 3,
  "payrollApproved": 18,
  "totalCommissionsPending": 4500.00
}
```

**Roles:** Admin, Supervisor

---

### **GET** `/api/employees/reports/attendance`
Reporte de asistencias por periodo

**Query Params:**
```
?startDate=2024-04-01
?endDate=2024-04-30
?department=Ventas
```

**Response 200:**
```json
[
  {
    "employeeName": "Juan Pérez",
    "department": "Ventas",
    "totalDays": 20,
    "attendances": 18,
    "lates": 2,
    "absences": 0,
    "attendanceRate": 90.0
  }
]
```

**Roles:** Admin, Supervisor

---

## 🔐 AUTENTICACIÓN

Todos los endpoints requieren token JWT en header:

```
Authorization: Bearer {token}
```

El token se obtiene de `/api/auth/login` y contiene:
- userId
- username
- role
- roleId

---

## 🚨 CÓDIGOS DE ERROR

| Código | Descripción |
|--------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content (success sin body) |
| 400 | Bad Request (datos inválidos) |
| 401 | Unauthorized (no autenticado) |
| 403 | Forbidden (sin permisos) |
| 404 | Not Found |
| 500 | Internal Server Error |

**Formato de error:**
```json
{
  "error": "Mensaje de error descriptivo",
  "details": "Stack trace (solo en dev)"
}
```

---

## 💡 EJEMPLOS DE USO

### Ejemplo 1: Flujo completo de alta de empleado

```bash
# 1. Crear usuario (si no existe)
POST /api/users
{
  "username": "juan.perez",
  "password": "Pass123!",
  "fullName": "Juan Pérez García",
  "email": "juan@vitaraiz.com",
  "roleId": 3,  # Vendedor
  "phone": "5512345678"
}
# → userId: 10

# 2. Crear empleado vinculado al usuario
POST /api/employees
{
  "userId": 10,
  "employeeCode": "EMP015",
  "jobTitle": "Vendedor",
  "baseSalary": 8000,
  "commissionRate": 3,
  "hireDate": "2024-04-09",
  "bankAccount": "012345678901234567"
}
# → employeeId: 15

# 3. Juan registra su asistencia del día
POST /api/employees/15/attendance
{
  "attendanceType": "ASISTENCIA",
  "checkInTime": "2024-04-09T08:05:00",
  "gpsLatitude": 19.432608,
  "gpsLongitude": -99.133209
}
```

### Ejemplo 2: Venta con comisión automática

```bash
# Al crear venta, automáticamente se genera comisión
POST /api/sales
{
  "customerId": 100,
  "sellerId": 10,  # Juan (userId)
  "totalAmount": 10000
}
# → Sistema automáticamente crea:
# - Sale ID: 155
# - Commission ID: 202 (300.00 = 10000 × 3%)
```

### Ejemplo 3: Generación de nómina quincenal

```bash
# Generar nómina del 1-15 de abril
POST /api/employees/15/payroll
{
  "periodStart": "2024-04-01",
  "periodEnd": "2024-04-15"
}
# → payrollId: 51
# Sistema calcula:
# - Base: 4000 (mitad del mes)
# - Comisiones: 1200 (4 ventas pendientes)
# - Deducciones: 100 (1 falta)
# - Neto: 5100

# Aprobar nómina
PUT /api/employees/payroll/51/approve

# Registrar pago
PUT /api/employees/payroll/51/pay
{
  "paymentDate": "2024-04-17",
  "paymentMethod": "TRANSFERENCIA",
  "paymentReference": "TRANS-001"
}
```

---

## 📞 SOPORTE

Para dudas sobre la API, consultar:
- `HR_MODULE_IMPLEMENTATION_GUIDE.md` - Guía completa
- `HR_MODULE_RESUMEN_EJECUTIVO.md` - Resumen ejecutivo
- Swagger UI: `http://localhost:5000/swagger`
