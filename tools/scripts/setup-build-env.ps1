# One-shot setup of the local build toolchain (no admin required).
# Installs to %LOCALAPPDATA%\IDCardBuild:
#   - dotnet-sdk\   .NET SDK 8 (build engine only; output targets .NET Framework 4.8)
#   - nuget.exe     NuGet CLI (optional, for package inspection)
#   - compilers\    Roslyn compiler toolset (optional, used by legacy MSBuild scenarios)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ErrorActionPreference = "Stop"

$root = Join-Path $env:LOCALAPPDATA "IDCardBuild"
New-Item -ItemType Directory -Force -Path $root | Out-Null

& (Join-Path $PSScriptRoot "get-dotnet-sdk.ps1")
& (Join-Path $PSScriptRoot "get-nuget.ps1")
& (Join-Path $PSScriptRoot "get-compilers.ps1")

Write-Host "`nBuild environment ready at $root"
Write-Host "Build:  tools\scripts\build.ps1"
Write-Host "Test:   tools\scripts\test.ps1"
