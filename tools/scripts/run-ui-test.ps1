# Full UI click-through test of ID Stack. Produces screenshots in tools\ui-test
# and prints a PASS/FAIL line per step.
$ErrorActionPreference = "Continue"
. "$PSScriptRoot\ui-test-helper.ps1"

$results = New-Object System.Collections.Generic.List[string]
function Step([string]$name, [scriptblock]$action) {
  try {
    & $action
    Start-Sleep -Milliseconds 700
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
  $proc = Get-App
}
Write-Host "Testing window of PID $($proc.Id)"

# --- Home page ---
Step "Home: screenshot" { Save-Shot "01-home.png" }

# --- Navigate to Template Editor via sidebar ---
Step "Nav: Template Editor" {
  $item = Find-Element $null "Template Editor" "ListItem"
  if (-not $item) { throw "sidebar item not found" }
  Select-Item $item
}
Step "Editor: screenshot" { Save-Shot "02-editor-empty.png" }

# --- Add elements via Tools panel ---
Step "Editor: add Text" { Invoke-Click (Find-Element $null "T  Text" "Button") }
Step "Editor: add Rectangle" { Invoke-Click (Find-Element $null "▭  Rectangle" "Button") }
Step "Editor: add Barcode" { Invoke-Click (Find-Element $null "▤  Barcode" "Button") }
Step "Editor: screenshot with elements" { Save-Shot "03-editor-elements.png" }

# --- Undo twice ---
Step "Editor: Undo" { Invoke-Click (Find-Element $null "Undo" "Button") }
Step "Editor: Undo again" { Invoke-Click (Find-Element $null "Undo" "Button") }
Step "Editor: screenshot after undo" { Save-Shot "04-editor-after-undo.png" }
Step "Editor: Redo" { Invoke-Click (Find-Element $null "Redo" "Button") }

# --- Switch to Back side ---
Step "Editor: switch to Back" {
  $back = Find-Element $null "Back" "RadioButton"
  if (-not $back) { throw "Back radio not found" }
  $sel = $back.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
  $sel.Select()
}
Step "Editor: back side screenshot" { Save-Shot "05-editor-back.png" }
Step "Editor: switch to Front" {
  $front = Find-Element $null "Front" "RadioButton"
  $sel = $front.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
  $sel.Select()
}

# --- Settings page ---
Step "Nav: Settings" {
  $item = Find-Element $null "Settings" "ListItem"
  Select-Item $item
}
Step "Settings: screenshot" { Save-Shot "06-settings.png" }

# --- Other pages render as placeholders ---
Step "Nav: Template Library" {
  $item = Find-Element $null "Template Library" "ListItem"
  Select-Item $item
}
Step "Library: screenshot" { Save-Shot "07-library.png" }
Step "Nav: Data Import" {
  $item = Find-Element $null "Data Import" "ListItem"
  Select-Item $item
}
Step "DataImport: screenshot" { Save-Shot "08-dataimport.png" }
Step "Nav: Batch Processing" {
  $item = Find-Element $null "Batch Processing" "ListItem"
  Select-Item $item
}
Step "Batch: screenshot" { Save-Shot "09-batch.png" }

# --- Error log check ---
Start-Sleep -Milliseconds 500
$log = "$env:APPDATA\IDStack\logs\" + (Get-Date).ToString("yyyy-MM-dd") + ".log"
$errCount = 0
if (Test-Path $log) {
  $errCount = (Select-String -Path $log -Pattern "\[ERROR\]" -SimpleMatch:$false | Measure-Object).Count
}
$results.Add("INFO  Errors in log after run: $errCount")
Write-Host "Errors in log after run: $errCount"

Write-Host "`n===== RESULTS ====="
$results | ForEach-Object { Write-Host $_ }
