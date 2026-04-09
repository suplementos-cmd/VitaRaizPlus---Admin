# ===========================================================================
# RESUMEN: Fix CategoryId para sistema de productos segmentado
# ===========================================================================

## Problema Solucionado

El sistema estaba usando `Category` (VARCHAR2) para filtrar productos, pero la tabla BDusa `CATEGORY_ID` (NUMBER FK). Esto causaba inconsistencias y productos no se agrupaban correctamente.

## Cambios Realizados

### 1. Base de Datos

#### Script SQL de Categorías
- **Archivo**: `SALESAPP_12_CATEGORIES_FIX.sql`
- **Categorías definidas**:
  * 1 = Suplementos
  * 2 = Papelería
  * 3 = Uniformes
  * 4 = Muestras
  * 5 = Kits de Ventas
  * 6 = Kits de Cobros

#### Procedimientos Almacenados Actualizados
- **sp_register_product**: Ahora acepta `p_category_id` (NUMBER) en lugar de `p_category` (VARCHAR2)
- **sp_update_product**: Actualizado para usar `category_id` en lugar de `category`

### 2. Backend (C#)

#### DTOs Actualizados
- `ProductDto` ahora incluye:
  ```csharp
  public int? CategoryId { get; set; }
  public string? Category { get; set; }  // Se mantiene para compatibilidad
  ```

#### Modelo de Dominio
- `Product` entity actualizada con `CategoryId`

#### EF Core Configuration
- Mapeo de `CATEGORY_ID` columna agregado

#### Repository & Commands
- `IProductRepository.CreateProductAsync()`: Acepta `int? categoryId`
- `IProductRepository.UpdateProductAsync()`: Acepta `int? categoryId`
- `CreateProductCommand`: Usa `CategoryId`
- `UpdateProductCommand`: Usa `CategoryId`

### 3. Frontend (Blazor)

#### Páginas Actualizadas
Todas las páginas de categorías ahora usan `CATEGORY_ID`:

| Página | Route | CATEGORY_ID | Constante |
|--------|-------|-------------|-----------|
| PapeleriaList | /admin/papeleria | 2 | `CATEGORY_ID = 2` |
| UniformesList | /admin/uniformes | 3 | `CATEGORY_ID = 3` |
| MuestrasList | /admin/muestras | 4 | `CATEGORY_ID = 4` |
| KitsVentasList | /admin/kits-ventas | 5 | `CATEGORY_ID = 5` |
| KitsCobrosList | /admin/kits-cobros | 6 | `CATEGORY_ID = 6` |

#### Cambios en el Código Razor
```csharp
// ANTES
private const string CATEGORY = "Papelería";
_all = allProducts.Where(p => p.Category == CATEGORY).ToList();
_editing = new ProductDto { Category = CATEGORY };

// DESPUÉS
private const int CATEGORY_ID = 2; // Papelería
_all = allProducts.Where(p => p.CategoryId == CATEGORY_ID).ToList();
_editing = new ProductDto { CategoryId = CATEGORY_ID };
```

## Instrucciones de Aplicación

### 1. Actualizar Base de Datos

```sql
-- 1. Ejecutar script de categorías
@Database/VitaRaiz_Migration/SALESAPP_12_CATEGORIES_FIX.sql

-- 2. Recompilar package
@Database/EM_VITARAIZ_AD.pck

-- 3. Verificar
SELECT CATEGORY_ID, CATEGORY_NAME FROM SALESAPP.PRODUCT_CATEGORIES ORDER BY CATEGORY_ID;
```

### 2. Reiniciar Aplicación

```powershell
cd c:\Projects\VitaRaizSalesApp\VitaRaiz.API
dotnet run
```

### 3. Probar Funcionalidad

1. Navegar a cada módulo de inventario
2. Crear productos en cada categoría
3. Verificar que se guarden con el `CATEGORY_ID` correcto
4. Confirmar que los filtros funcionan correctamente

## Verificación

### BD - Verificar Productos por Categoría
```sql
SELECT c.CATEGORY_NAME, COUNT(p.PRODUCT_ID) AS TOTAL
FROM SALESAPP.PRODUCT_CATEGORIES c
LEFT JOIN SALESAPP.PRODUCTS p ON p.CATEGORY_ID = c.CATEGORY_ID
GROUP BY c.CATEGORY_NAME
ORDER BY c.CATEGORY_ID;
```

### Web - Verificar Páginas
- ✅ `/admin/inventario` - Dashboard debe mostrar 6 categorías
- ✅ `/admin/papeleria` - Solo productos con CategoryId = 2
- ✅ `/admin/uniformes` - Solo productos con CategoryId = 3
- ✅ `/admin/muestras` - Solo productos con CategoryId = 4
- ✅ `/admin/kits-ventas` - Solo productos con CategoryId = 5
- ✅ `/admin/kits-cobros` - Solo productos con CategoryId = 6

## Archivos Modificados

### Base de Datos
- `Database/EM_VITARAIZ_AD.pck`
- `Database/VitaRaiz_Migration/SALESAPP_12_CATEGORIES_FIX.sql`

### Backend
- `VitaRaiz.Domain/Entities/Product.cs`
- `VitaRaiz.Infrastructure/Data/VitaRaizDbContext.cs`
- `VitaRaiz.Infrastructure/Repositories/ProductRepository.cs`
- `VitaRaiz.Application/DTOs/EntityDtos.cs`
- `VitaRaiz.Application/Commands/Products/ProductCommands.cs`
- `VitaRaiz.Application/Commands/Products/ProductCommandHandlers.cs`
- `VitaRaiz.Application/Interfaces/IRepositories.cs`
- `VitaRaiz.WebPortal/Models/ApiDtos.cs`

### Frontend
- `VitaRaiz.WebPortal/Components/Pages/Admin/Catalogos/PapeleriaList.razor`
- `VitaRaiz.WebPortal/Components/Pages/Admin/Catalogos/UniformesList.razor`
- `VitaRaiz.WebPortal/Components/Pages/Admin/Catalogos/MuestrasList.razor`
- `VitaRaiz.WebPortal/Components/Pages/Admin/Catalogos/KitsVentasList.razor`
- `VitaRaiz.WebPortal/Components/Pages/Admin/Catalogos/KitsCobrosList.razor`

---

**Fecha**: 8 de abril de 2026  
**Estado**: ✅ Completado  
**Próximo paso**: Ejecutar migrations SQL y probar creación de productos
