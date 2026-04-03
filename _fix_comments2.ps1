$utf8 = New-Object System.Text.UTF8Encoding($false)
$f = 'c:\Projects\VitaRaizSalesApp\VitaRaiz.Mobile\Pages\RegistrarCobroPage.xaml'
$c = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)

# Fix comments where " -- -->" ends them - the " --" before "-->" is extra
# e.g. "<!-- GPS -- -->" becomes "<!-- GPS -->"
$c = [regex]::Replace($c, ' -- (-->)', ' $1')
$c = [regex]::Replace($c, ' --- (-->)', ' $1')
$c = [regex]::Replace($c, ' ==- (-->)', ' $1')
# Also "<!-- BODY --- -->" style
$c = $c.Replace('--- -->', '-->')

[System.IO.File]::WriteAllText($f, $c, $utf8)
Write-Host "Written"

# Verify: look for any -- inside comment content (not the closing -->)
$lines = $c -split "`n"
$issueCount = 0
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    # Find all comment content (between <!-- and -->)
    $m = [regex]::Match($line, '<!--(.*?)-->')
    if ($m.Success) {
        $content = $m.Groups[1].Value
        if ($content -match '--') {
            Write-Host ("L" + ($i+1) + " STILL BAD: " + $line.Trim())
            $issueCount++
        }
    }
}
if ($issueCount -eq 0) {
    Write-Host "All XML comments are now valid!"
} else {
    Write-Host "$issueCount bad comments remaining"
}
