# ===========================================================================
# Script PowerShell: Compilar package EM_VITARAIZ_AD en Oracle
# ===========================================================================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  FIX PRODUCT_CODE - VitaRaiz SalesApp" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Configuración de conexión (ajustar según tu ambiente)
$OracleUser = "SALESAPP"
$OraclePassword = Read-Host "Ingrese password para usuario $OracleUser" -AsSecureString
$OracleTNS = "XEPDB1"  # Ajustar según tu TNS

# Convertir SecureString a texto plano
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($OraclePassword)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

Write-Host "Conectando a Oracle..." -ForegroundColor Yellow

# Crear archivo temporal con comandos SQL
$TempSQLFile = Join-Path $PSScriptRoot "temp_compile.sql"
$PackageFile = Join-Path $PSScriptRoot "EM_VITARAIZ_AD.pck"

@"
-- Conectar y ejecutar
CONNECT $OracleUser/$PlainPassword@$OracleTNS
SET SERVEROUTPUT ON
SET ECHO ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

-- Compilar package
@$PackageFile

-- Verificar compilación
SELECT object_name, object_type, status 
FROM user_objects 
WHERE object_name = 'EM_VITARAIZ_AD'
ORDER BY object_type;

EXIT;
"@ | Out-File -FilePath $TempSQLFile -Encoding UTF8

try {
    # Verificar que sqlplus esté disponible
    $sqlplus = Get-Command sqlplus -ErrorAction SilentlyContinue
    
    if ($null -eq $sqlplus) {
        Write-Host "`n❌ ERROR: sqlplus no encontrado en PATH" -ForegroundColor Red
        Write-Host "   Instale Oracle Instant Client o agregue sqlplus al PATH`n" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "`nEjecutando compilación del package..." -ForegroundColor Green
    
    # Ejecutar sqlplus
    & sqlplus /nolog "@$TempSQLFile"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Package compilado exitosamente!" -ForegroundColor Green
        Write-Host "`nEl procedimiento sp_register_product ahora genera automáticamente" -ForegroundColor Cyan
        Write-Host "el campo PRODUCT_CODE con formato: PROD-000001, PROD-000002, etc.`n" -ForegroundColor Cyan
    } else {
        Write-Host "`n❌ Error al compilar el package (código: $LASTEXITCODE)" -ForegroundColor Red
        Write-Host "Revise los mensajes de error arriba`n" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "`n❌ Error: $_" -ForegroundColor Red
} finally {
    # Limpiar archivo temporal
    if (Test-Path $TempSQLFile) {
        Remove-Item $TempSQLFile -Force
    }
}

Write-Host "`nPresione cualquier tecla para continuar..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
