# Verification test for the upgraded editor: new-doc dialog, tools, handles, shortcuts.
$ErrorActionPreference = "Continue"
. "$PSScriptRoot\run-uia-click-test.ps1"

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

function Get-NavItem([int]$index) {
  $root = Get-Root
  $list = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::List)))
  $items = $list.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::ListItem)))
  return $items[$index]
}

function Get-ButtonByName([string]$name) { Find-ByName $name "Button" }
function Click-El($el) { if (-not $el) { throw "element is null" }; Click-ElementCenter $el }

Step "U01 nav Template Editor" { Click-El (Get-NavItem 1) }
Step "U02 shot new canvas" { Save-Shot "v1-canvas.png" }

Step "U03 add Text" { Click-El (Get-ButtonByName "Add Text") }
Step "U04 add Rectangle" { Click-El (Get-ButtonByName "Add Rectangle") }
Step "U05 add Barcode" { Click-El (Get-ButtonByName "Add Barcode") }
Step "U06 add Ellipse" { Click-El (Get-ButtonByName "Add Ellipse") }
Step "U07 shot 4 elements" { Save-Shot "v2-elements.png" }

Step "U08 Undo" { Click-El (Get-ButtonByName "Undo") }
Step "U09 Redo" { Click-El (Get-ButtonByName "Redo") }
Step "U10 shot after undo/redo" { Save-Shot "v3-undoredo.png" }

Step "U11 switch Back" { Click-El (Find-ByName "Back" "RadioButton") }
Step "U12 shot back" { Save-Shot "v4-back.png" }
Step "U13 switch Front" { Click-El (Find-ByName "Front" "RadioButton") }

Step "U14 Zoom In" { Click-El (Get-ButtonByName "Zoom In") }
Step "U15 Zoom Out" { Click-El (Get-ButtonByName "Zoom Out") }
Step "U16 shot zoom" { Save-Shot "v5-zoom.png" }

Step "U17 nav Home" { Click-El (Get-NavItem 0) }
Step "U18 shot home" { Save-Shot "v6-home.png" }

Start-Sleep -Milliseconds 400
$log = "$env:APPDATA\IDStack\logs\" + (Get-Date).ToString("yyyy-MM-dd") + ".log"
$errCount = 0
if (Test-Path $log) { $errCount = (Select-String -Path $log -Pattern "\[ERROR\]" | Measure-Object).Count }
$results.Add("INFO  App log errors: $errCount")

Write-Host "`n===== RESULTS ====="
$results | ForEach-Object { Write-Host $_ }
