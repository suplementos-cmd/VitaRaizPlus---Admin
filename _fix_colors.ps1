$utf8 = New-Object System.Text.UTF8Encoding($false)  # UTF-8 without BOM
$base = 'c:\Projects\VitaRaizSalesApp\VitaRaiz.Mobile\Pages'
$ctrl = 'c:\Projects\VitaRaizSalesApp\VitaRaiz.Mobile\Controls'

function Fix-File {
    param([string]$path, [hashtable]$replacements)
    if (-not (Test-Path $path)) { Write-Host "  SKIP (not found): $path"; return }
    $c = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $changed = 0
    foreach ($kv in $replacements.GetEnumerator()) {
        if ($c.Contains($kv.Key)) {
            $c = $c.Replace($kv.Key, $kv.Value)
            $changed++
        }
    }
    [System.IO.File]::WriteAllText($path, $c, $utf8)
    Write-Host "  Fixed ($changed replacements): $(Split-Path $path -Leaf)"
}

# SalesPage.xaml
Write-Host "SalesPage.xaml:"
Fix-File "$base\SalesPage.xaml" @{
    'BackgroundColor="#FAFAFA"'    = 'BackgroundColor="{DynamicResource PageBackgroundNeutral}"'
    'BackgroundColor="#F3E5F5"'    = 'BackgroundColor="{DynamicResource ThemeColorLighter}"'
    'TextColor="#CE93D8"'          = 'TextColor="{DynamicResource ThemeColorLight}"'
    'TextColor="#DC3545"'          = 'TextColor="{DynamicResource DangerColor}"'
}

# SaleDetailPage.xaml
Write-Host "SaleDetailPage.xaml:"
Fix-File "$base\SaleDetailPage.xaml" @{
    'BackgroundColor="#F8F4FC"'    = 'BackgroundColor="{DynamicResource PageBackground}"'
    'BackgroundColor="#28A745"'    = 'BackgroundColor="{DynamicResource ThemeColor}"'
    'TextColor="#28A745"'          = 'TextColor="{DynamicResource ThemeColorHex}"'
    'TextColor="#DC3545"'          = 'TextColor="{DynamicResource DangerColor}"'
    'Color="#F0E6FA"'              = 'Color="{DynamicResource DividerColor}"'
    'Color="#F5F0FA"'              = 'Color="{DynamicResource DividerColor}"'
}

# PaymentDetailPage.xaml
Write-Host "PaymentDetailPage.xaml:"
Fix-File "$base\PaymentDetailPage.xaml" @{
    'BackgroundColor="#FAFAFA"'    = 'BackgroundColor="{DynamicResource PageBackgroundNeutral}"'
    'TextColor="#28A745"'          = 'TextColor="{DynamicResource ThemeColorHex}"'
    'TextColor="#DC3545"'          = 'TextColor="{DynamicResource DangerColor}"'
    'BackgroundColor="#28A745"'    = 'BackgroundColor="{DynamicResource ThemeColor}"'
    'BackgroundColor="#6C757D"'    = 'BackgroundColor="{DynamicResource NeutralActionColor}"'
    "#4428A745'"                   = "#44000000'"
    "#446C757D'"                   = "#44000000'"
}

# CustomersPage.xaml
Write-Host "CustomersPage.xaml:"
Fix-File "$base\CustomersPage.xaml" @{
    'BackgroundColor="#F0F4F8"'    = 'BackgroundColor="{DynamicResource PageBackground}"'
    'Stroke="#E0E0E0"'             = 'Stroke="{DynamicResource BorderColorMedium}"'
    'TextColor="#2196F3"'          = 'TextColor="{DynamicResource ThemeColorHex}"'
    'TextColor="#F44336"'          = 'TextColor="{DynamicResource DangerColor}"'
}

# CreateSalePage.xaml
Write-Host "CreateSalePage.xaml:"
Fix-File "$base\CreateSalePage.xaml" @{
    'BackgroundColor="#F8F4FC"'    = 'BackgroundColor="{DynamicResource PageBackground}"'
}

# HomePage.xaml
Write-Host "HomePage.xaml:"
Fix-File "$base\HomePage.xaml" @{
    'BackgroundColor="#F2F6F3"'    = 'BackgroundColor="{DynamicResource PageBackground}"'
    'TextColor="#FFFDE7"'          = 'TextColor="White"'
    'TextColor="#1A2E1F"'          = 'TextColor="{DynamicResource TextBodyDark}"'
    'TextColor="#6B7280"'          = 'TextColor="{DynamicResource TextSecondary}"'
    'TextColor="#1F2937"'          = 'TextColor="{DynamicResource TextPrimary}"'
    'TextColor="#9CA3AF"'          = 'TextColor="{DynamicResource TextMuted}"'
    'Stroke="#E5E7EB"'             = 'Stroke="{DynamicResource BorderColorLight}"'
}

# RegistrarCobroPage.xaml (UNTRACKED: fix -- in comments + color replacements)
Write-Host "RegistrarCobroPage.xaml (fixing comments + colors):"
$rcp = "$base\RegistrarCobroPage.xaml"
if (Test-Path $rcp) {
    $c = [System.IO.File]::ReadAllText($rcp, [System.Text.Encoding]::UTF8)
    # Fix invalid XML comments: replace "---" sequences inside comment markers
    # Pattern: <!-- ... --- ... --> becomes <!-- ... === ... -->
    $c = [System.Text.RegularExpressions.Regex]::Replace($c,
        '(?<=<!--[^>]*)--([\s\S]*?)(?=-->)', '==$1',
        [System.Text.RegularExpressions.RegexOptions]::None)
    # Also handle cases where comment itself starts with ---
    $c = $c.Replace('<!-- ---', '<!-- ===')
    $c = $c.Replace('--- -->', '=== -->')
    $c = $c.Replace('------', '======')
    # Apply color replacements
    $c = $c.Replace('BackgroundColor="#F8F4FC"', 'BackgroundColor="{DynamicResource PageBackground}"')
    $c = $c.Replace('BackgroundColor="#E8F5E9"', 'BackgroundColor="{DynamicResource ThemeColorLighter}"')
    $c = $c.Replace('TextColor="#2E7D32"', 'TextColor="{DynamicResource ThemeColorDark}"')
    $c = $c.Replace('Value="#E8F5E9"', 'Value="{DynamicResource ThemeColorLighter}"')
    $c = $c.Replace('Value="#4CAF50"', 'Value="{DynamicResource ThemeColor}"')
    $c = $c.Replace('Value="#2E7D32"', 'Value="{DynamicResource ThemeColorDark}"')
    $c = $c.Replace('TextColor="#DC3545"', 'TextColor="{DynamicResource DangerColor}"')
    $c = $c.Replace('Color="#F0E6FA"', 'Color="{DynamicResource DividerColor}"')
    [System.IO.File]::WriteAllText($rcp, $c, $utf8)
    Write-Host "  Fixed RegistrarCobroPage.xaml"
} else {
    Write-Host "  NOT FOUND: RegistrarCobroPage.xaml"
}

# BottomNavBar.xaml (UNTRACKED: fix color only - no problematic comments)
Write-Host "BottomNavBar.xaml:"
Fix-File "$ctrl\BottomNavBar.xaml" @{
    'TextColor="#9CA3AF"'          = 'TextColor="{DynamicResource TextMuted}"'
}

Write-Host ""
Write-Host "=== ALL DONE ==="
