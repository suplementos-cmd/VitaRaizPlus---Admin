# VitaRaiz Sales & Collection System

**🔄 Actualizado**: 25 de Marzo, 2026  
**Versión**: 2.0

Sistema integral de gestión de ventas y cobranza para VitaRaiz, implementado con Clean Architecture, CQRS y .NET 8.0.

---

## 🎯 INICIO RÁPIDO

### ¿Nuevo en el proyecto?

1. 📖 Lee [RESUMEN_CORRECCIONES.md](RESUMEN_CORRECCIONES.md) - Cambios recientes y estado actual
2. 🏗️ Revisa [ARQUITECTURA.md](ARQUITECTURA.md) - Estructura completa del sistema
3. ⚙️ Sigue [CONFIGURACION.md](CONFIGURACION.md) - Guía paso a paso de configuración
4. 🗺️ Consulta [API_ENDPOINTS_MAP.md](API_ENDPOINTS_MAP.md) - 46 endpoints documentados

### ¿Problemas con el login?

✅ **SOLUCIONADO** - WebPortal ya funciona correctamente. Ver [RESUMEN_CORRECCIONES.md](RESUMEN_CORRECCIONES.md)

---

## 🏗️ Arquitectura

### Estructura de Proyectos

```
VitaRaizSalesApp/
├── VitaRaiz.Domain/          # Entidades del dominio (sin dependencias)
├── VitaRaiz.Application/     # Lógica de negocio (CQRS + MediatR)
├── VitaRaiz.Infrastructure/  # Acceso a datos (Oracle + EF Core)
├── VitaRaiz.API/             # REST API con JWT authentication
├── VitaRaiz.WebPortal/       # Portal administrativo (Blazor Server)
└── VitaRaiz.Mobile/          # App móvil multiplataforma (.NET MAUI)
```

### Principios Aplicados

- ✅ **Clean Architecture** - Separación de capas con dependencias unidireccionales
- ✅ **CQRS** - Commands & Queries con MediatR
- ✅ **DDD** - Domain-Driven Design con entidades ricas
- ✅ **Repository Pattern** - Abstracción de acceso a datos
- ✅ **Dependency Injection** - IoC en todos los proyectos

📖 **Documentación completa**: [ARQUITECTURA.md](ARQUITECTURA.md)

---

## 🔧 Tecnologías

### Backend
- **.NET 8.0** - Framework principal
- **Oracle XE 21c** - Base de datos
- **Entity Framework Core 8.0.3** - ORM
- **Oracle.EntityFrameworkCore 8.23.60** - Provider Oracle
- **MediatR 14.1.0** - Patrón mediator para CQRS
- **FluentValidation 12.1.1** - Validación de entrada

### Frontend
- **Blazor Server** - Web portal administrativo
- **.NET MAUI** - Aplicación móvil (Android, iOS, Windows)
- **Bootstrap 5** - UI framework

### Seguridad
- **JWT Bearer Authentication** - Autenticación basada en tokens (8 horas de validez)
- **Role-based Authorization** - Control de acceso por roles (Admin, Supervisor, Vendedor, Cobrador)

---

## 📦 Configuración de Base de Datos

### Requisitos Previos

1. Oracle XE 21c instalado
2. Conexión: `localhost:1521/XEPDB1`
3. Usuario: `salesapp` / Password: `SalesApp2026`

### Paquete PL/SQL

El sistema utiliza el paquete **EM_VITARAIZ_AD** que contiene:

- **25 Procedimientos almacenados**
- **13 Funciones**
- **6 Vistas**

#### Componentes Principales:

- `fn_authenticate_user` - Autenticación de usuarios
- `sp_register_payment` - Registrar pagos
- `fn_get_sale_balance` - Obtener saldo pendiente
- `fn_get_risk_status` - Calcular nivel de riesgo
- `sp_daily_collection_stats` - Estadísticas diarias de cobranza
- `v_collector_customers_detail` - Vista de clientes por cobrador

📖 **Guía completa de configuración**: [CONFIGURACION.md](CONFIGURACION.md)

---

## 🚀 Instalación y Ejecución

### Paso 1: Verificar Requisitos

```powershell
# Verificar .NET SDK
dotnet --version
# Debe mostrar: 8.0.xxx

# Verificar Oracle
sqlplus salesapp/SalesApp2026@localhost:1521/XEPDB1
```

### Paso 2: Configurar Base de Datos

```powershell
cd Database
sqlplus salesapp/SalesApp2026@localhost:1521/XEPDB1 @01_Tablas.sql
sqlplus salesapp/SalesApp2026@localhost:1521/XEPDB1 @EM_VITARAIZ_AD.pck
```

### Paso 3: Ejecutar la API

```powershell
cd VitaRaiz.API
dotnet restore
dotnet run --launch-profile http
```

**API disponible en**: `http://localhost:5299`  
**Swagger UI**: `http://localhost:5299/swagger`

### Paso 4: Ejecutar WebPortal

```powershell
cd VitaRaiz.WebPortal
dotnet restore
dotnet run --launch-profile http
```

**WebPortal disponible en**: `http://localhost:5145`

### Paso 5: Ejecutar Mobile App (Opcional)

**Windows**:
```powershell
cd VitaRaiz.Mobile
dotnet restore
dotnet run -f net8.0-windows10.0.19041.0
```

**Android**:
```powershell
cd VitaRaiz.Mobile
dotnet restore
dotnet run -f net8.0-android
```

📖 **Guía detallada**: [CONFIGURACION.md](CONFIGURACION.md)

---

## 📱 Funcionalidades

### API REST - 46 Endpoints Documentados

#### Controladores:

**🔐 AuthController** (3 endpoints)
- `POST /api/auth/login` - Inicio de sesión (genera JWT)
- `GET /api/auth/validate` - Validar token ✨ NUEVO
- `GET /api/auth/me` - Usuario actual ✨ NUEVO

**💰 SalesController** (7 endpoints)
- `GET /api/sales` - Obtener ventas con filtros
- `GET /api/sales/active` - Ventas activas
- `GET /api/sales/{id}` - Venta por ID
- `GET /api/sales/{id}/balance` - Balance pendiente
- `POST /api/sales` - Crear venta
- `PUT /api/sales/{id}/cancel` - Cancelar venta

**💳 PaymentsController** (5 endpoints)
- `GET /api/payments` - Obtener pagos
- `GET /api/payments/{id}` - Pago por ID
- `POST /api/payments` - Registrar pago
- `PUT /api/payments/{id}/approve` - Aprobar pago
- `PUT /api/payments/{id}/reject` - Rechazar pago

**👥 CustomersController** (5 endpoints)
- `GET /api/customers` - Listar clientes
- `GET /api/customers/{id}` - Cliente por ID
- `POST /api/customers` - Crear cliente
- `PUT /api/customers/{id}` - Actualizar cliente
- `DELETE /api/customers/{id}` - Eliminar cliente

**📦 ProductsController, 🗺️ ZonesController, 📚 CatalogsController** y más...

📖 **Documentación completa**: [API_ENDPOINTS_MAP.md](API_ENDPOINTS_MAP.md)

---

### Web Portal

**Páginas disponibles:**

1. **🏠 Dashboard** (`/dashboard`)
   - Métricas clave del día
   - Ventas, cobros, clientes activos
   - Gráficos y estadísticas

2. **👥 Gestión de Clientes** (`/clientes`)
   - Lista completa con búsqueda
   - Indicadores Gold ⭐ y Blacklist 🚫
   - Filtrado por zona

3. **💰 Gestión de Ventas** (`/ventas`)
   - Listado con filtros por estado
   - Consulta de balance en tiempo real
   - Asignación de cobradores

4. **💳 Gestión de Pagos** (`/pagos`)
   - Registro y seguimiento
   - Geolocalización GPS 📍
   - Validación y aprobación

5. **📊 Reportes** (`/reportes`)
   - Cobranza diaria
   - Ventas por periodo
   - Rendimiento por cobrador
   - Análisis de morosidad

---

### Mobile App

**Características**:
- ✅ Login con credenciales
- ✅ Gestión de clientes
- ✅ Creación de ventas
- ✅ Registro de pagos con geolocalización
- ✅ Visualización de catálogos
- ✅ Sincronización con API
- 🔜 Modo offline (próximamente)
- 🔜 Notificaciones push (próximamente)

**Plataformas soportadas**:
- ✅ Android
- ✅ iOS
- ✅ Windows

---

## 🔐 Autenticación y Autorización

### JWT Token

**Configuración**:
- **Validez**: 8 horas (480 minutos)
- **Algoritmo**: HS256
- **Claims**: userId, username, role

**Headers requeridos**:
```
Authorization: Bearer {token}
```

### Roles del Sistema

| Rol | Permisos |
|-----|----------|
| **Admin** | Acceso total a todos los endpoints |
| **Supervisor** | Crear/editar clientes, aprobar pagos, cancelar ventas |
| **Vendedor** | Crear ventas, ver clientes, ver productos |
| **Cobrador** | Registrar pagos, ver ventas activas, ver clientes |

---

## 📊 Estado del Proyecto

### ✅ FUNCIONANDO CORRECTAMENTE

- ✅ **API Backend**: 46 endpoints operativos
- ✅ **WebPortal**: Login y todas las funcionalidades ✨ CORREGIDO
- ✅ **Mobile App**: Login y operaciones principales
- ✅ **Base de Datos**: Oracle XE con paquete PL/SQL
- ✅ **Autenticación**: JWT tokens con validación
- ✅ **CORS**: Configurado para todos los clientes

### 🆕 Cambios Recientes (25/03/2026)

1. ✅ Agregado endpoint `/api/auth/validate`
2. ✅ Agregado endpoint `/api/auth/me`
3. ✅ CORS mejorado para WebPortal
4. ✅ URLs corregidas en configuraciones
5. ✅ Documentación completa creada

📖 **Detalles completos**: [RESUMEN_CORRECCIONES.md](RESUMEN_CORRECCIONES.md)

---

## 🔧 Solución de Problemas

### ❌ WebPortal no puede hacer login
✅ **SOLUCIONADO** - Verifica que:
1. API esté corriendo en el puerto correcto
2. URL en `appsettings.json` del WebPortal coincida con la API
3. CORS esté habilitado en la API

### ❌ Mobile no se conecta
Verifica:
1. URL en `ApiService.cs` sea correcta
2. Para Android Emulator usa `http://10.0.2.2:5299`
3. Para Android Device usa tu IP local

### ❌ Error de Oracle
Verifica:
1. Oracle Database esté corriendo: `lsnrctl status`
2. Connection String sea correcta
3. Package `EM_VITARAIZ_AD` esté compilado

📖 **Guía completa de solución de problemas**: [CONFIGURACION.md](CONFIGURACION.md#solución-de-problemas)

---

## 📚 Documentación

| Documento | Descripción |
|-----------|-------------|
| [README.md](README.md) | Este archivo - Visión general del proyecto |
| [ARQUITECTURA.md](ARQUITECTURA.md) | Arquitectura completa del sistema |
| [API_ENDPOINTS_MAP.md](API_ENDPOINTS_MAP.md) | 46 endpoints documentados con ejemplos |
| [CONFIGURACION.md](CONFIGURACION.md) | Guía de instalación y configuración |
| [RESUMEN_CORRECCIONES.md](RESUMEN_CORRECCIONES.md) | Cambios recientes y correcciones |
| [ANALISIS_PROYECTO.md](ANALISIS_PROYECTO.md) | Análisis técnico del proyecto |

---

## 🚀 Roadmap

### Próximas Mejoras

- [ ] Implementar Refresh Tokens
- [ ] Health Checks endpoint
- [ ] Logging centralizado (Serilog)
- [ ] API Versioning (v1, v2)
- [ ] Testing automatizado (Unit + Integration)
- [ ] Offline support en Mobile
- [ ] Notificaciones push
- [ ] CI/CD Pipeline

---

## 👥 Contribución

Para contribuir al proyecto:

1. Fork el repositorio
2. Crea una rama de feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

---

## 📄 Licencia

Este proyecto es propiedad de VitaRaiz. Todos los derechos reservados.

---

## 📞 Contacto y Soporte

Para soporte técnico o preguntas:
- Revisar documentación en `/docs/`
- Consultar [CONFIGURACION.md](CONFIGURACION.md) - Sección de troubleshooting
- Abrir un Issue en el repositorio

---

**© 2026 VitaRaiz Sales & Collection System - v2.0**

### Aplicación Móvil

**Características planificadas:**
- Login con JWT
- Dashboard de cobrador
- Registro de pagos con cámara
- GPS para ubicación
- SQLite offline sync
- Ruta optimizada por GPS

## 🔐 Seguridad

### JWT Configuration

En `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "VitaRaiz_SuperSecretKey_2026_MustBe32CharactersMinimum!",
    "Issuer": "VitaRaizAPI",
    "Audience": "VitaRaizClients",
    "ExpirationMinutes": 480
  }
}
```

### Roles Disponibles

- **Admin** - Acceso total
- **Supervisor** - Gestión de cobradores y reportes
- **Cobrador** - Registro de pagos
- **Vendedor** - Registro de ventas

## 📊 Estado del Proyecto

### ✅ Completado

- [x] Arquitectura Clean Architecture
- [x] Domain Layer (8 entidades)
- [x] Application Layer (Commands, Queries, Handlers, Validators)
- [x] Infrastructure Layer (DbContext, Repositories, Oracle integration)
- [x] API Layer (Controllers, JWT, Swagger)
- [x] WebPortal (5 páginas funcionales)
- [x] MAUI Project (estructura base)

### ⚠️ Pendiente

- [ ] Implementar hashing de contraseñas (BCrypt)
- [ ] Crear modales para formularios
- [ ] Agregar paginación en tablas
- [ ] Implementar gráficos en Dashboard
- [ ] UI Mobile completa (LoginPage, PaymentPage)
- [ ] SQLite offline sync
- [ ] Unit Tests
- [ ] Integration Tests

### 🐛 Problemas Conocidos

1. **Mobile - Android SDK Required**: El proyecto MAUI requiere Android SDK instalado para compilar
2. **Nullability Warnings**: 5 warnings en WebPortal (no críticos)

## 📝 Convenciones de Código

### Nomenclatura

- **Entities**: PascalCase (User, Customer, Sale)
- **Commands**: `{Action}{Entity}Command` (RegisterPaymentCommand)
- **Queries**: `Get{Entity}{Detail}Query` (GetSaleBalanceQuery)
- **Handlers**: `{Command/Query}Handler`
- **Repositories**: `I{Entity}Repository`, `{Entity}Repository`

### Estructura de Archivos

```
Application/
├── Commands/
│   └── Payments/
│       ├── RegisterPaymentCommand.cs
│       ├── RegisterPaymentCommandHandler.cs
│       └── RegisterPaymentCommandValidator.cs
├── Queries/
│   └── Sales/
│       ├── GetSaleBalanceQuery.cs
│       └── GetSaleBalanceQueryHandler.cs
└── Interfaces/
    └── IPaymentRepository.cs
```

## 🤝 Contribución

### Ramas

- `master` - Producción estable
- `feature/main` - Features principales
- `feature/develop` - Desarrollo activo

### Workflow

1. Crear rama desde `feature/develop`
2. Implementar cambios
3. Commit con mensajes descriptivos
4. Push y crear Pull Request
5. Merge después de revisión

## 📄 Licencia

Proyecto privado - VitaRaiz © 2026

## 👥 Equipo

- **Desarrollador Principal**: Esteban Gabriel
- **Base de Datos**: Oracle XE 21c
- **Paquete PL/SQL**: EM_VITARAIZ_AD

## 📞 Contacto

Para soporte o consultas sobre el sistema, contactar al equipo de desarrollo.

---

**Última actualización**: 23 de Marzo, 2026
