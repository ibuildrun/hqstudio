# Builds the single-file GUI installer and the server bundle used by the in-app updater.
#   dist\HQStudio-Setup-<version>.exe      what the end user runs
#   dist\HQStudio-Server-v<version>.zip    compose + nginx files for the "Update site" button
# Usage: pwsh installer\build.ps1 [-Version 1.2.3] [-SkipTests]
param(
    [string]$Version = '',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root 'dist'
$publish = Join-Path $dist 'publish'
$stage = Join-Path $dist 'payload'
$payload = Join-Path $dist 'payload.zip'
$desktopProj = Join-Path $root 'HQStudio.Desktop\HQStudio.csproj'
$setupProj = Join-Path $root 'HQStudio.Setup\HQStudio.Setup.csproj'

if (-not $Version) {
    $m = [regex]::Match((Get-Content $desktopProj -Raw), '<Version>([^<]+)</Version>')
    $Version = $m.Groups[1].Value
}
Write-Host "Version: $Version"

function Invoke-Checked([scriptblock]$Block, [string]$What) {
    & $Block
    if ($LASTEXITCODE -ne 0) { throw "$What failed (exit code $LASTEXITCODE)" }
}

# 1. Desktop app (single file, self-contained)
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
Invoke-Checked { dotnet publish $desktopProj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$Version -o $publish } 'desktop publish'

# 2. Payload embedded into the installer
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force -Path (Join-Path $stage 'app'), (Join-Path $stage 'server\nginx') | Out-Null
Copy-Item (Join-Path $publish 'HQStudio.exe') (Join-Path $stage 'app\HQStudio.exe')
Copy-Item (Join-Path $root 'docs\INSTALL.ru.html') (Join-Path $stage 'app\ИНСТРУКЦИЯ.html')
Copy-Item (Join-Path $root 'deploy\docker-compose.yml') (Join-Path $stage 'server\docker-compose.yml')
Copy-Item (Join-Path $root 'deploy\.env.example') (Join-Path $stage 'server\.env.example')
Copy-Item (Join-Path $root 'deploy\nginx\default.conf') (Join-Path $stage 'server\nginx\default.conf')
if (Test-Path $payload) { Remove-Item $payload -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $payload, [System.IO.Compression.CompressionLevel]::Optimal, $false)

# 3. Tests of the installer logic
if (-not $SkipTests) {
    Invoke-Checked { dotnet test (Join-Path $root 'HQStudio.Setup.Tests\HQStudio.Setup.Tests.csproj') -c Release } 'installer tests'
}

# 4. Installer
$setupOut = Join-Path $dist 'setup'
if (Test-Path $setupOut) { Remove-Item -Recurse -Force $setupOut }
Invoke-Checked { dotnet publish $setupProj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:Version=$Version "-p:PayloadZip=$payload" -o $setupOut } 'installer publish'
$exe = Get-ChildItem $setupOut -Filter '*.exe' | Select-Object -First 1
$final = Join-Path $dist "HQStudio-Setup-$Version.exe"
Copy-Item $exe.FullName $final -Force

# 5. Server bundle for the updater (never contains .env)
$bundle = Join-Path $dist "HQStudio-Server-v$Version.zip"
if (Test-Path $bundle) { Remove-Item $bundle -Force }
Compress-Archive -Path (Join-Path $root 'deploy\docker-compose.yml'), (Join-Path $root 'deploy\.env.example'), (Join-Path $root 'deploy\nginx') -DestinationPath $bundle

Write-Host ''
Write-Host "Installer: $final ($([math]::Round((Get-Item $final).Length / 1MB, 1)) MB)"
Write-Host "Server bundle: $bundle"
