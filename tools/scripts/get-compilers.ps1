param()
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$url = "https://www.nuget.org/api/v2/package/Microsoft.Net.Compilers.Toolset/4.9.2"
$out = Join-Path $env:TEMP "microsoft.net.compilers.toolset.4.9.2.zip"

Write-Host "Downloading $url ..."
Invoke-WebRequest -Uri $url -OutFile $out -UseBasicParsing -TimeoutSec 300
Write-Host "Saved to $out"

$dest = Join-Path $env:LOCALAPPDATA "IDCardBuild\compilers"
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
Expand-Archive -Path $out -DestinationPath $dest -Force
Write-Host "Extracted to $dest"

$csc = Get-ChildItem $dest -Recurse -Filter csc.exe | Select-Object -First 1
Write-Host "csc.exe: $($csc.FullName)"
& $csc.FullName /version
