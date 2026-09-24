# Coordinate-based click-through test of ID Stack.
# Window is 1280x788; sidebar items are at fixed offsets.
$ErrorActionPreference = "Continue"
. "$PSScriptRoot\ui-click.ps1"

$results = New-Object System.Collections.Generic.List[string]
function Step([string]$name, [scriptblock]$action) {
  try {
    & $action
    Start-Sleep -Milliseconds 900
    $results.Add("PASS  $name")
    Write-Host "PASS  $name" -ForegroundColor Green
  } catch {
    $results.Add("FAIL  $name : " + $_.Exception.Message)
    Write-Host "FAIL  $name : $($_.Exception.Message)" -ForegroundColor Red
  }
}

$proc = Get-App
if (-not $proc) {
  Start-Process "D:\ID-Stack\src\IDStack\bin\Release\net48\IDStack.exe"
  Start-Sleep -Seconds 5
}
Write-Host "Testing PID $((Get-App).Id)"

# Sidebar item Y-centers (from screenshot: items at y=140,184,229,274,319,364 in a 788-high window)
$nav = @{
  "Home"             = 140/788
  "Template Editor"  = 184/788
  "Template Library" = 229/788
  "Data Import"      = 274/788
  "Batch Processing" = 319/788
  "Settings"         = 364/788
}
$navX = 110/1280

# Toolbar buttons (x from screenshot, y=137 center of toolbar row)
$toolY = 137/788
$btn = @{
  "New"  = @(251/1280, $toolY)
  "Open" = @(308/1280, $toolY)
  "Save" = @(366/1280, $toolY)
  "Undo" = @(431/1280, $toolY)
  "Redo" = @(487/1280, $toolY)
  "Back" = @(555/1280, $toolY)
  "Front"= @(528/1280, $toolY)
}
# Tools panel buttons (x=328 center, y from screenshot)
$toolsX = 328/1280
$tool = @{
  "Text"        = 239/788
  "Image"       = 272/788
  "Rectangle"   = 306/788
  "Ellipse"     = 339/788
  "Line"        = 372/788
  "Barcode"     = 405/788
  "Placeholder" = 439/788
}

Step "01 Home shot" { Save-Shot "01-home.png" }

Step "02 nav to Template Editor" { Click-Rel $navX $nav["Template Editor"] }
Step "03 shot empty editor" { Save-Shot "02-editor-empty.png" }

Step "04 add Text" { Click-Rel $toolsX $tool["Text"] }
Step "05 add Rectangle" { Click-Rel $toolsX $tool["Rectangle"] }
Step "06 add Barcode" { Click-Rel $toolsX $tool["Barcode"] }
Step "07 shot with elements" { Save-Shot "03-editor-elements.png" }

Step "08 Undo" { Click-Rel $btn["Undo"][0] $btn["Undo"][1] }
Step "09 Undo again" { Click-Rel $btn["Undo"][0] $btn["Undo"][1] }
Step "10 shot after 2x undo" { Save-Shot "04-after-undo.png" }
Step "11 Redo" { Click-Rel $btn["Redo"][0] $btn["Redo"][1] }

Step "12 switch to Back" { Click-Rel $btn["Back"][0] $btn["Back"][1] }
Step "13 shot back side" { Save-Shot "05-editor-back.png" }
Step "14 switch to Front" { Click-Rel $btn["Front"][0] $btn["Front"][1] }

Step "15 nav to Settings" { Click-Rel $navX $nav["Settings"] }
Step "16 shot settings" { Save-Shot "06-settings.png" }

Step "17 nav to Template Library" { Click-Rel $navX $nav["Template Library"] }
Step "18 shot library" { Save-Shot "07-library.png" }

Step "19 nav to Data Import" { Click-Rel $navX $nav["Data Import"] }
Step "20 shot data import" { Save-Shot "08-dataimport.png" }

Step "21 nav to Batch Processing" { Click-Rel $navX $nav["Batch Processing"] }
Step "22 shot batch" { Save-Shot "09-batch.png" }

Step "23 back to Home" { Click-Rel $navX $nav["Home"] }

Start-Sleep -Milliseconds 500
$log = "$env:APPDATA\IDStack\logs\" + (Get-Date).ToString("yyyy-MM-dd") + ".log"
$errCount = 0
if (Test-Path $log) {
  $errCount = (Select-String -Path $log -Pattern "\[ERROR\]" | Measure-Object).Count
}
$results.Add("INFO  Errors in log: $errCount")

Write-Host "`n===== RESULTS ====="
$results | ForEach-Object { Write-Host $_ }
