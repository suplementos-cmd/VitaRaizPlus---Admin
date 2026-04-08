$r = [string][char]0xFFFD

# Lista de archivos a corregir
$files = @(
    "VitaRaiz.WebPortal\Components\Pages\Dashboard\Dashboard.razor"
)

foreach ($path in $files) {
    $c = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $before = ($c.ToCharArray() | Where-Object { [int]$_ -gt 127 }).Count

    # Reemplazos específicos por contexto
    $c = $c.Replace("@_greeting " + $r + " @DateTime",  "@_greeting · @DateTime")
    $c = $c.Replace($r + "ltimos 30 d" + $r + "as",     "Últimos 30 días")
    $c = $c.Replace("Secci" + $r + "n Ventas",           "Sección Ventas")
    $c = $c.Replace("An" + $r + "lisis de Ventas",       "Análisis de Ventas")
    $c = $c.Replace("Gr" + $r + "fica 1",                "Gráfica 1")
    $c = $c.Replace("Gr" + $r + "fica 2",                "Gráfica 2")
    $c = $c.Replace("Gr" + $r + "fica 3",                "Gráfica 3")
    $c = $c.Replace("Gr" + $r + "fica 4",                "Gráfica 4")
    $c = $c.Replace("Gr" + $r + "fica 5",                "Gráfica 5")
    $c = $c.Replace("Gr" + $r + "fica 6",                "Gráfica 6")
    $c = $c.Replace("Gr" + $r + "fica 7",                "Gráfica 7")
    $c = $c.Replace("Gr" + $r + "fica de",               "Gráfica de")
    $c = $c.Replace("Ventas por d" + $r + "a",           "Ventas por día")
    $c = $c.Replace("Cobros por d" + $r + "a",           "Cobros por día")
    $c = $c.Replace("d" + $r + "a (principal",           "día (principal")
    $c = $c.Replace("Bot" + $r + "n ver", "Botón ver")
    $c = $c.Replace("gr" + $r + "ficas",                 "gráficas")
    $c = $c.Replace("m" + $r + "s gr",                   "más gr")
    $c = $c.Replace("Secci" + $r + "n Cobros",           "Sección Cobros")
    $c = $c.Replace("An" + $r + "lisis de Cobros",       "Análisis de Cobros")
    $c = $c.Replace("Buenos d" + $r + "as",              "Buenos días")

    $after = ($c.ToCharArray() | Where-Object { [int]$_ -gt 127 }).Count
    Write-Host "$path : $before → $after chars no-ASCII"
    [System.IO.File]::WriteAllText($path, $c, [System.Text.Encoding]::UTF8)
}
