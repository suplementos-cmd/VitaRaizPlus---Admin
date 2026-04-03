$utf8 = New-Object System.Text.UTF8Encoding($false)
$f = 'c:\Projects\VitaRaizSalesApp\VitaRaiz.Mobile\Pages\RegistrarCobroPage.xaml'
$c = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
Write-Host "File length: $($c.Length)"

# All comment closings got mangled from "-->" to "==>"
$c = $c.Replace('==>', '-->')

# Comment openings that got mangled
$c = $c.Replace('<!-- ==- ', '<!-- ')
$c = $c.Replace('<!-- -- ', '<!-- ')
$c = $c.Replace('<!-- === ', '<!-- ')

[System.IO.File]::WriteAllText($f, $c, $utf8)
Write-Host "Written"

$check = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
$bad = [regex]::Matches($check, '<!--[^>]*--[^>]').Count
Write-Host "Bad comment patterns remaining: $bad"
# Also find any remaining "==>" 
$eqgt = ($check -split "`n" | Select-String '==>').Count
Write-Host "Remaining '==>': $eqgt"
