param(
    [string]$DllPath = (Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\ValheimAutoTranslator.dll')
)

$ErrorActionPreference = 'Stop'
$manifest = Join-Path $PSScriptRoot 'manifest.json'
$readme = Join-Path $PSScriptRoot 'README.md'
$changelog = Join-Path $PSScriptRoot 'CHANGELOG.md'
$icon = Join-Path $PSScriptRoot 'icon.png'
$archive = Join-Path $PSScriptRoot 'Ntxfloy-ValheimAutoTranslator-0.1.5-test.zip'

foreach ($required in @($DllPath, $manifest, $readme, $changelog)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing: $required" }
}

if (-not (Test-Path -LiteralPath $icon)) {
    Add-Type -AssemblyName System.Drawing
    $bitmap = New-Object System.Drawing.Bitmap(256, 256)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::FromArgb(25, 37, 56))
        $font = New-Object System.Drawing.Font('Arial', 96, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 239, 247))
        try {
            $format = New-Object System.Drawing.StringFormat
            $format.Alignment = [System.Drawing.StringAlignment]::Center
            $format.LineAlignment = [System.Drawing.StringAlignment]::Center
            $graphics.DrawString('AT', $font, $brush, [System.Drawing.RectangleF]::new(0, 0, 256, 256), $format)
        }
        finally { $brush.Dispose(); $font.Dispose() }
        $bitmap.Save($icon, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $graphics.Dispose(); $bitmap.Dispose() }
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
$zip = [System.IO.Compression.ZipFile]::Open($archive, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $manifest, 'manifest.json') | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $readme, 'README.md') | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $changelog, 'CHANGELOG.md') | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $icon, 'icon.png') | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $DllPath, 'BepInEx/plugins/ValheimAutoTranslator.dll') | Out-Null
}
finally { $zip.Dispose() }
Write-Host $archive
