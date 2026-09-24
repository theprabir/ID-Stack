# Final click-through test: clicks elements via UIA bounding rectangles.
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

function Get-ButtonByName([string]$name) {
  Find-ByName $name "Button"
}

function Click-El($el) {
  if (-not $el) { throw "element is null" }
  Click-ElementCenter $el
}

# Nav indices: 0=Home 1=Template Editor 2=Template Library 3=Data Import 4=Batch Processing 5=Settings
Step "T01 nav Template Editor" { Click-El (Get-NavItem 1) }
Step "T02 shot editor" { Save-Shot "e1-empty.png" }

Step "T03 add Text" { Click-El (Get-ButtonByName "T  Text") }
Step "T04 add Rectangle" { Click-El (Get-ButtonByName "▭  Rectangle") }
Step "T05 add Barcode" { Click-El (Get-ButtonByName "▤  Barcode") }
Step "T06 shot elements" { Save-Shot "e2-elements.png" }

Step "T07 Undo" { Click-El (Get-ButtonByName "Undo") }
Step "T08 Undo" { Click-El (Get-ButtonByName "Undo") }
Step "T09 shot after undo" { Save-Shot "e3-after-undo.png" }
Step "T10 Redo" { Click-El (Get-ButtonByName "Redo") }

Step "T11 switch Back" {
  $radio = Find-ByName "Back" "RadioButton"
  if (-not $radio) { throw "no Back radio" }
  Click-El $radio
}
Step "T12 shot back" { Save-Shot "e4-back.png" }

Step "T13 zoom in" { Click-El (Get-ButtonByName "+") }
Step "T14 zoom out" { Click-El (Get-ButtonByName "-") }

Step "T15 nav Settings" { Click-El (Get-NavItem 5) }
Step "T16 shot settings" { Save-Shot "e5-settings.png" }

Step "T17 nav Home" { Click-El (Get-NavItem 0) }
Step "T18 shot home" { Save-Shot "e6-home.png" }

Start-Sleep -Milliseconds 400
$log = "$env:APPDATA\IDStack\logs\" + (Get-Date).ToString("yyyy-MM-dd") + ".log"
$errCount = 0
if (Test-Path $log) { $errCount = (Select-String -Path $log -Pattern "\[ERROR\]" | Measure-Object).Count }
$results.Add("INFO  App log errors: $errCount")

Write-Host "`n===== RESULTS ====="
$results | ForEach-Object { Write-Host $_ }
