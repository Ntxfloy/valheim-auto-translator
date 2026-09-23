param(
    [string]$GameRoot,
    [string]$Profile = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Default",
    [switch]$Install
)

$ErrorActionPreference = 'Stop'
if (-not $GameRoot) {
    $candidates = @('D:\Steam\steamapps\common\Valheim', 'D:\SteamLibrary\steamapps\common\Valheim')
    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    if ($programFilesX86) { $candidates = @((Join-Path $programFilesX86 'Steam\steamapps\common\Valheim')) + $candidates }
    $GameRoot = $candidates | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'valheim_Data\Managed\assembly_valheim.dll') } | Select-Object -First 1
    if (-not $GameRoot) { throw 'Valheim not found. Pass -GameRoot with your Valheim directory.' }
}
$managed = Join-Path $GameRoot 'valheim_Data\Managed'
$core = Join-Path $Profile 'BepInEx\core'
$localSdk = Join-Path $env:TEMP 'valheim-dotnet-sdk\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { 'dotnet' }
$output = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\ValheimAutoTranslator.dll'

foreach ($required in @((Join-Path $managed 'assembly_guiutils.dll'), (Join-Path $managed 'assembly_valheim.dll'), (Join-Path $core 'BepInEx.dll'), (Join-Path $core '0Harmony.dll'))) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Missing: $required" }
}

& $dotnet build (Join-Path $PSScriptRoot 'ValheimAutoTranslator.csproj') -c Release -p:GameRoot=$GameRoot -p:Profile=$Profile --nologo
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Host "Built $output"

if ($Install) {
    $destination = Join-Path $Profile 'BepInEx\plugins\ValheimAutoTranslator'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $output -Destination (Join-Path $destination 'ValheimAutoTranslator.dll') -Force
    Write-Host "Installed to $destination"
}
