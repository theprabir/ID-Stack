# Builds the IDStack solution without requiring Visual Studio.
# Prerequisites (auto-installed to %LOCALAPPDATA%\IDCardBuild by get-dotnet-sdk.ps1):
#   .NET SDK 8 used only as the build engine; output targets .NET Framework 4.8.
param(
  [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$dotnet = Join-Path $env:LOCALAPPDATA "IDCardBuild\dotnet-sdk\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

$repoRoot = Join-Path $PSScriptRoot "..\.."
$solution = Join-Path $repoRoot "IDStack.sln"

& $dotnet build $solution -c $Configuration --nologo
exit $LASTEXITCODE
