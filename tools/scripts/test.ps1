# Runs the IDCardSoftware unit tests without requiring Visual Studio.
param(
  [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$dotnet = Join-Path $env:LOCALAPPDATA "IDCardBuild\dotnet-sdk\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

$repoRoot = Join-Path $PSScriptRoot "..\.."
$testProject = Join-Path $repoRoot "src\IDCardSoftware.Tests\IDCardSoftware.Tests.csproj"

& $dotnet test $testProject -c $Configuration
exit $LASTEXITCODE
