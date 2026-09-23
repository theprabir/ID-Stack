[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$dest = Join-Path $env:LOCALAPPDATA "IDCardBuild\nuget.exe"
if (-not (Test-Path $dest)) {
  Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $dest -UseBasicParsing -TimeoutSec 120
}
& $dest help | Select-Object -First 2
Write-Host "nuget.exe at: $dest"
