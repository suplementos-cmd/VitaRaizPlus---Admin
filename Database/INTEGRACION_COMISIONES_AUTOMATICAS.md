# 🔄 GUÍA DE INTEGRACIÓN - COMISIONES AUTOMÁTICAS

## 📋 Objetivo

Generar automáticamente comisiones para empleados cuando:
- **Vendedor** realiza una venta → Comisión sobre monto total de venta
- **Cobrador** confirma un pago → Comisión sobre monto cobrado

---

## ✅ PASO 1: Identificar Puntos de Integración

### 📍 **Ubicación 1: Handlers de Venta**
**Archivo**: `VitaRaiz.Application/Commands/Sales/CreateSaleCommandHandler.cs`

**Momento**: Después de crear la venta exitosamente

```csharp
// Al final del Handle, después de INSERT exitoso
var saleId = await _saleRepository.CreateSaleAsync(...);

// 🆕 AGREGAR: Generar comisión si el vendedor tiene commission_rate > 0
await GenerateCommissionForSaleAsync(saleId, command.SellerId);

return saleId;
```

---

### 📍 **Ubicación 2: Handlers de Pago**
**Archivo**: `VitaRaiz.Application/Commands/Payments/ConfirmPaymentCommandHandler.cs` (o similar)

**Momento**: Después de aprobar/confirmar el pago

```csharp
// Después de sp_approve_payment
await _paymentRepository.ApprovePaymentAsync(paymentId, userId);

// 🆕 AGREGAR: Generar comisión si el cobrador tiene commission_rate > 0
await GenerateCommissionForPaymentAsync(paymentId, collectorId);
```

---

## 🔧 PASO 2: Implementar Métodos Auxiliares

### Método 1: GenerateCommissionForSaleAsync

```csharp
private async Task GenerateCommissionForSaleAsync(int saleId, int sellerId)
{
    try
    {
        // 1. Verificar si el vendedor es empleado
        var employees = await _employeeRepository.GetEmployeesAsync();
        var employee = employees.FirstOrDefault(e => e.UserId == sellerId && e.EmploymentStatus == "ACTIVO");
        
        if (employee == null || employee.CommissionRate <= 0)
        {
            _logger.LogInformation("Vendedor {SellerId} no es empleado o no tiene comisión configurada", sellerId);
            return;
        }

        // 2. Registrar comisión
        var commissionId = await _employeeRepository.RegisterCommissionFromSaleAsync(
            employee.EmployeeId,
            saleId,
            employee.CommissionRate
        );
        
        _logger.LogInformation("Comisión {CommissionId} generada para empleado {EmployeeId} por venta {SaleId}",
            commissionId, employee.EmployeeId, saleId);
    }
    catch (Exception ex)
    {
        // No fallar la venta si falla la comisión
        _logger.LogError(ex, "Error generando comisión para venta {SaleId}", saleId);
    }
}
```

### Método 2: GenerateCommissionForPaymentAsync

```csharp
private async Task GenerateCommissionForPaymentAsync(int paymentId, int collectorId)
{
    try
    {
        // 1. Verificar si el cobrador es empleado
        var employees = await _employeeRepository.GetEmployeesAsync();
        var employee = employees.FirstOrDefault(e => e.UserId == collectorId && e.EmploymentStatus == "ACTIVO");
        
        if (employee == null || employee.CommissionRate <= 0)
        {
            _logger.LogInformation("Cobrador {CollectorId} no es empleado o no tiene comisión configurada", collectorId);
            return;
        }

        // 2. Registrar comisión
        var commissionId = await _employeeRepository.RegisterCommissionFromPaymentAsync(
            employee.EmployeeId,
            paymentId,
            employee.CommissionRate
        );
        
        _logger.LogInformation("Comisión {CommissionId} generada para empleado {EmployeeId} por pago {PaymentId}",
            commissionId, employee.EmployeeId, paymentId);
    }
    catch (Exception ex)
    {
        // No fallar el pago si falla la comisión
        _logger.LogError(ex, "Error generando comisión para pago {PaymentId}", paymentId);
    }
}
```

---

## 🔍 PASO 3: Inyectar IEmployeeRepository

En cada Handler que necesite generar comisiones:

```csharp
public class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, int>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IEmployeeRepository _employeeRepository; // 🆕 AGREGAR
    private readonly ILogger<CreateSaleCommandHandler> _logger;

    public CreateSaleCommandHandler(
        ISaleRepository saleRepository,
        IEmployeeRepository employeeRepository, // 🆕 AGREGAR
        ILogger<CreateSaleCommandHandler> logger)
    {
        _saleRepository = saleRepository;
        _employeeRepository = employeeRepository; // 🆕 AGREGAR
        _logger = logger;
    }
    
    // ... resto del código
}
```

---

## 📈 FLUJO COMPLETO

### Caso 1: Venta con Comisión

```
1. Usuario crea venta (SellerId = 5)
   ↓
2. sp_register_sale ejecuta → SaleId = 1001
   ↓
3. Buscar empleado con UserId = 5
   ↓
4. Si existe y CommissionRate = 3.5%
   ↓
5. sp_register_commission_from_sale(
      EmployeeId = 10,
      SaleId = 1001,
      CommissionRate = 3.5
   )
   ↓
6. Comisión creada con STATUS = 'PENDIENTE'
```

### Caso 2: Pago con Comisión

```
1. Cobrador confirma pago (CollectorId = 8)
   ↓
2. sp_approve_payment ejecuta → PaymentId = 2005
   ↓
3. Buscar empleado con UserId = 8
   ↓
4. Si existe y CommissionRate = 2.0%
   ↓
5. sp_register_commission_from_payment(
      EmployeeId = 15,
      PaymentId = 2005,
      CommissionRate = 2.0
   )
   ↓
6. Comisión creada con STATUS = 'PENDIENTE'
```

---

## ✅ VENTAJAS DE ESTE DISEÑO

1. **✅ No Intrusivo**: La venta/pago se completa aunque falle la comisión
2. **✅ Trazable**: Logs completos de cada comisión generada
3. **✅ Configurable**: Solo genera si employee.CommissionRate > 0
4. **✅ Automático**: No requiere intervención manual
5. **✅ Flexible**: Cada empleado puede tener su propio % de comisión

---

## 🎯 SIGUIENTES PASOS

1. **Localizar Handlers** de CreateSale y ConfirmPayment
2. **Agregar IEmployeeRepository** al constructor
3. **Copiar métodos auxiliares** (GenerateCommissionForSaleAsync, GenerateCommissionForPaymentAsync)
4. **Llamar métodos** después de operaciones exitosas
5. **Probar** con empleados que tengan CommissionRate configurado

---

## 📊 EJEMPLO DE USO

```csharp
// Empleado: Juan Pérez
// EmployeeId: 10
// UserId: 5 (vinculado a USERS)
// RoleName: "Vendedor"
// CommissionRate: 3.5

// Cuando Juan crea una venta de $10,000:
// → Comisión automática = $10,000 * 3.5% = $350
// → Estado: PENDIENTE
// → Se procesará en la próxima nómina
```

---

## 🔐 SEGURIDAD

- ✅ Solo empleados ACTIVOS generan comisiones
- ✅ Solo si CommissionRate > 0
- ✅ Validación de existencia de empleado
- ✅ Try-catch para no afectar operación principal

