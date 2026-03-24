# VitaRaiz Sales & Collection System

Sistema integral de gestión de ventas y cobranza para VitaRaiz, implementado con Clean Architecture, CQRS y .NET 8.0.

## 🏗️ Arquitectura

### Estructura de Proyectos

```
VitaRaizSalesApp/
├── VitaRaiz.Domain/          # Entidades del dominio (sin dependencias)
├── VitaRaiz.Application/     # Lógica de negocio (CQRS + MediatR)
├── VitaRaiz.Infrastructure/  # Acceso a datos (Oracle + EF Core)
├── VitaRaiz.API/             # REST API con JWT authentication
├── VitaRaiz.WebPortal/       # Portal administrativo (Blazor Server)
└── VitaRaiz.Mobile/          # App móvil Android (.NET MAUI)
```

### Principios Aplicados

- ✅ **Clean Architecture** - Separación de capas con dependencias unidireccionales
- ✅ **CQRS** - Commands & Queries con MediatR
- ✅ **DDD** - Domain-Driven Design con entidades ricas
- ✅ **Repository Pattern** - Abstracción de acceso a datos
- ✅ **Dependency Injection** - IoC en todos los proyectos

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
- **.NET MAUI** - Aplicación móvil Android
- **Bootstrap 5** - UI framework

### Seguridad
- **JWT Bearer Authentication** - Autenticación basada en tokens
- **Role-based Authorization** - Control de acceso por roles

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

- `sp_register_payment` - Registrar pagos
- `fn_get_sale_balance` - Obtener saldo pendiente
- `fn_get_risk_status` - Calcular nivel de riesgo
- `sp_daily_collection_stats` - Estadísticas diarias de cobranza
- `v_collector_customers_detail` - Vista de clientes por cobrador

## 🚀 Instalación y Ejecución

### 1. Clonar el Repositorio

```bash
git clone https://github.com/suplementos-cmd/VitaRaizPlus---Admin.git
cd VitaRaizPlus---Admin
```

### 2. Configurar Cadena de Conexión

**VitaRaiz.API/appsettings.json** y **VitaRaiz.WebPortal/appsettings.json**:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=salesapp;Password=SalesApp2026;"
  }
}
```

### 3. Compilar Solución

```bash
dotnet build
```

### 4. Ejecutar API

```bash
cd VitaRaiz.API
dotnet run
```

La API se ejecutará en: `https://localhost:5001` (o puerto asignado)

### 5. Ejecutar Web Portal

```bash
cd VitaRaiz.WebPortal
dotnet run
```

El portal se ejecutará en: `https://localhost:5145` (o puerto asignado)

### 6. Ejecutar Mobile (Requiere Android SDK)

```bash
cd VitaRaiz.Mobile
dotnet build -f net9.0-android
```

## 📱 Funcionalidades

### API REST

#### Endpoints Principales:

**Autenticación:**
- `POST /api/auth/login` - Inicio de sesión (genera JWT)

**Pagos:**
- `POST /api/payments` - Registrar pago (requiere auth)

**Ventas:**
- `GET /api/sales/{id}/balance` - Consultar balance de venta

### Web Portal

**Páginas disponibles:**

1. **Dashboard** (`/dashboard`)
   - Métricas clave del día
   - Ventas, cobros, clientes activos

2. **Gestión de Clientes** (`/clientes`)
   - Lista completa con búsqueda
   - Indicadores Gold ⭐ y Blacklist 🚫
   - Filtrado por zona

3. **Gestión de Ventas** (`/ventas`)
   - Listado con filtros por estado
   - Consulta de balance en tiempo real
   - Asignación de cobradores

4. **Gestión de Pagos** (`/pagos`)
   - Registro y seguimiento
   - Geolocalización GPS 📍
   - Validación y aprobación

5. **Reportes** (`/reportes`)
   - Cobranza diaria
   - Ventas por periodo
   - Rendimiento por cobrador
   - Análisis de morosidad

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
