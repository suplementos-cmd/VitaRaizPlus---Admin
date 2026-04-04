# Fix U+FFFD replacement characters in all WebPortal Razor files
# Run from the solution root: .\fix_encoding.ps1

$root = "C:\Projects\VitaRaizSalesApp\VitaRaiz.WebPortal"
$enc  = [System.Text.Encoding]::UTF8
$R    = [char]0xFFFD   # Unicode replacement character

# ── Context-aware replacement rules ──────────────────────────────────────────
# Order matters: more specific patterns first.
$rules = [ordered]@{
    # PageTitle separators
    "Dashboard ${R} VitaRaiz"                = "Dashboard - VitaRaiz"
    "Ventas ${R} VitaRaiz"                   = "Ventas - VitaRaiz"
    "Venta #@SaleId ${R} VitaRaiz"           = "Venta #@SaleId - VitaRaiz"
    "Clientes ${R} VitaRaiz"                 = "Clientes - VitaRaiz"
    "Cobros ${R} VitaRaiz"                   = "Cobros - VitaRaiz"
    "Reportes ${R} VitaRaiz"                 = "Reportes - VitaRaiz"
    "Perfil ${R} VitaRaiz"                   = "Perfil - VitaRaiz"
    "Usuarios ${R} VitaRaiz"                 = "Usuarios - VitaRaiz"
    "Roles ${R} VitaRaiz"                    = "Roles - VitaRaiz"
    "Temas ${R} VitaRaiz"                    = "Temas - VitaRaiz"
    "Productos ${R} VitaRaiz"                = "Productos - VitaRaiz"
    "Zonas ${R} VitaRaiz"                    = "Zonas - VitaRaiz"
    "Configuraci${R}n ${R} VitaRaiz"         = "Configuracion - VitaRaiz"
    "Configuraci${R}n"                        = "Configuracion"
    "Initialize${R}"                          = "Initializer"
    # Inline separators
    "@_greeting ${R} @DateTime"              = "@_greeting · @DateTime"
    "CollectorName ${R} @p.PaymentDate"      = "CollectorName · @p.PaymentDate"
    "VitaRaiz Sales App ${R}"               = "VitaRaiz Sales App ©"
    "VitaRaiz Sales App  @DateTime"         = "VitaRaiz Sales App © @DateTime"
    # Spanish words with ó
    "aprobaci${R}n"                          = "aprobacion"
    "Aprobaci${R}n"                          = "Aprobacion"
    "Gesti${R}n"                             = "Gestion"
    "gesti${R}n"                             = "gestion"
    "Sesi${R}n"                              = "Sesion"
    "sesi${R}n"                              = "sesion"
    "configuraci${R}n"                       = "configuracion"
    "Informaci${R}n"                         = "Informacion"
    "informaci${R}n"                         = "informacion"
    "Acci${R}n"                              = "Accion"
    "acci${R}n"                              = "accion"
    "creaci${R}n"                            = "creacion"
    "Creaci${R}n"                            = "Creacion"
    "descripci${R}n"                         = "descripcion"
    "Descripci${R}n"                         = "Descripcion"
    "eliminaci${R}n"                         = "eliminacion"
    "cancelaci${R}n"                         = "cancelacion"
    "contrasena"                             = "contrasena"       # already ascii
    "contrase${R}a"                          = "contrasena"
    "Contrase${R}a"                          = "Contrasena"
    "autenticaci${R}n"                       = "autenticacion"
    "Conexi${R}n"                            = "Conexion"
    "direcci${R}n"                           = "direccion"
    "Direcci${R}n"                           = "Direccion"
    "notificaci${R}n"                        = "notificacion"
    "paginaci${R}n"                          = "paginacion"
    "Paginaci${R}n"                          = "Paginacion"
    "validaci${R}n"                          = "validacion"
    "actualizaci${R}n"                       = "actualizacion"
    "Actualizaci${R}n"                       = "Actualizacion"
    "administraci${R}n"                      = "administracion"
    "Administraci${R}n"                      = "Administracion"
    "ubicaci${R}n"                           = "ubicacion"
    "Ubicaci${R}n"                           = "Ubicacion"
    "asignaci${R}n"                          = "asignacion"
    "Asignaci${R}n"                          = "Asignacion"
    # Spanish words with í
    "d${R}a "                                = "dia "
    "d${R}as"                                = "dias"
    "D${R}az"                                = "Diaz"
    # Spanish words with á
    "tambi${R}n"                             = "tambien"
    "Tambi${R}n"                             = "Tambien"
    "p${R}gina"                              = "pagina"
    "P${R}gina"                              = "Pagina"
    "n${R}mero"                              = "numero"
    "N${R}mero"                              = "Numero"
    "cat${R}logo"                            = "catalogo"
    "Cat${R}logo"                            = "Catalogo"
    "tel${R}fono"                            = "telefono"
    "Tel${R}fono"                            = "Telefono"
    # Spanish words with ú
    "${R}ltimo"                              = "ultimo"
    "${R}ltima"                              = "ultima"
    "${R}nico"                               = "unico"
    "${R}nica"                               = "unica"
    "b${R}squeda"                            = "busqueda"
    # Spanish words with ñ
    "a${R}o"                                 = "anno"
    "a${R}adir"                              = "agregar"
    # Emoji replacements (2-char sequences or single replacement)
    "Todo al dia ${R}"                       = "Todo al dia ✓"
    "${firstName.Split(' ')[0]} ${R}${R}"   = "`${firstName.Split(' ')[0]}"
    "${R}${R};"                              = ";"
    "${R};"                                  = ";"
    # Portal label fixes
    "Paginas"                                = "Paginas"
    "Panel de gesti${R}n"                    = "Panel de gestion"
    "Cerrar sesi${R}n"                       = "Cerrar sesion"
    "Iniciar Sesi${R}n"                      = "Iniciar Sesion"
    "Iniciando..."                           = "Iniciando..."
    "Portal de Gesti${R}n"                   = "Portal de Gestion"
    "Contrase${R}a"                          = "Contrasena"
    "contrase${R}a"                          = "contrasena"
    "Telef${R}no"                            = "Telefono"
    "Colecci${R}n"                           = "Coleccion"
}

# ── Process all razor files ────────────────────────────────────────────────────
$files = Get-ChildItem -Path $root -Recurse -Filter "*.razor"
$totalFixed = 0

foreach ($file in $files) {
    $bytes   = [System.IO.File]::ReadAllBytes($file.FullName)
    $content = $enc.GetString($bytes)

    if (-not ($content -match [regex]::Escape($R))) { continue }

    $original = $content
    foreach ($rule in $rules.GetEnumerator()) {
        $content = $content.Replace($rule.Key, $rule.Value)
    }
    # Catch-all: any remaining lone replacement chars that are still odd separators  
    # (surrounded by spaces) → replace with dash
    $content = $content -replace " $([regex]::Escape($R)) ", " - "
    # Any remaining U+FFFD → remove silently
    $content = $content -replace [regex]::Escape($R), ""

    if ($content -ne $original) {
        [System.IO.File]::WriteAllBytes($file.FullName, $enc.GetBytes($content))
        $remaining = ([regex]::Matches($content, [regex]::Escape($R))).Count
        Write-Host "FIXED: $($file.Name) (remaining FFFD: $remaining)"
        $totalFixed++
    }
}

Write-Host ""
Write-Host "Done. Fixed $totalFixed files."
