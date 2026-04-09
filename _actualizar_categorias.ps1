#!/usr/bin/env pwsh
# Script para actualizar todas las páginas de categorías con CategoryId

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Actualizar páginas de categorías" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$files = @(
    @{File="UniformesList.razor"; Id=3; Name="Uniformes"},
    @{File="MuestrasList.razor"; Id=4; Name="Muestras"},
    @{File="KitsVentasList.razor"; Id=5; Name="Kits de Ventas"},
    @{File="KitsCobrosList.razor"; Id=6; Name="Kits de Cobros"}
)

$basePath = "c:\Projects\VitaRaizSalesApp\VitaRaiz.WebPortal\Components\Pages\Admin\Catalogos\"

foreach ($item in $files) {
    $filePath = Join-Path $basePath $item.File
    Write-Host "Actualizando $($item.File)..." -ForegroundColor Yellow
    
    if (Test-Path $filePath) {
        $content = Get-Content $filePath -Raw -Encoding UTF8
        
        # Actualizar CATEGORY_ID
        $content = $content -replace 'private const string CATEGORY = ".*?";', "private const int CATEGORY_ID = $($item.Id); // $($item.Name)"
        
        # Actualizar filtro en LoadAsync
        $content = $content -replace '\.Where\(p => p\.Category == CATEGORY\)', '.Where(p => p.CategoryId == CATEGORY_ID)'
        
        # Actualizar OpenCreate
        $content = $content -replace 'Category = CATEGORY', 'CategoryId = CATEGORY_ID'
        
        # Actualizar asignación en SaveAsync
        $content = $content -replace '_editing\.Category = CATEGORY;.*?// Guardar producto', "_editing.CategoryId = CATEGORY_ID; // Asegurar categoría`r`n        _saving = true;`r`n        `r`n        // Guardar producto"
        
        $content | Set-Content $filePath -Encoding UTF8 -NoNewline
        Write-Host "  ✓ $($item.File) actualizado" -ForegroundColor Green
    } else {
        Write-Host "  ✗ No se encontró $($item.File)" -ForegroundColor Red
    }
}

Write-Host "`n✅ Actualización completada!`n" -ForegroundColor Green
