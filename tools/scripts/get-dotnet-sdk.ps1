# Installs .NET SDK 8 into a user-local folder (no admin required).
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $env:TEMP "dotnet-install.ps1"
$installDir = Join-Path $env:LOCALAPPDATA "IDCardBuild\dotnet-sdk"

if (-not (Test-Path $scriptPath)) {
  Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $scriptPath -UseBasicParsing -TimeoutSec 120
}

& $scriptPath -Channel 8.0 -InstallDir $installDir
Write-Host "dotnet at: $installDir\dotnet.exe"
& (Join-Path $installDir "dotnet.exe") --list-sdks
