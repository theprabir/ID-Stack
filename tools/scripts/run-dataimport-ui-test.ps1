# End-to-end UI test for the Data Import flow (0.3.2):
# front + back designs, ONE Excel sheet, photo folder -> mapping (front/back)
# -> preview & validate.
# Launches the app itself with IDSTACK_AUTO_* env hooks (native Win32 dialogs
# cannot be scripted from a background session) so the hooks reach the app.
. "$PSScriptRoot\run-uia-click-test.ps1"

# Kill any stale instance, then start the app WITH the env hooks set.
Get-Process IDStack -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
$env:IDSTACK_AUTO_DESIGN      = "D:\ID-Stack\tools\sample-data\sample-badge.idcard"
$env:IDSTACK_AUTO_DESIGN_BACK = "D:\ID-Stack\tools\sample-data\sample-badge-back.idcard"
$env:IDSTACK_AUTO_IMPORT      = "D:\ID-Stack\tools\sample-data\employees.csv"
$env:IDSTACK_AUTO_PHOTOS      = "D:\ID-Stack\tools\sample-data\photos"
Start-Process "D:\ID-Stack\src\IDStack\bin\Debug\net48\IDStack.exe"
Start-Sleep -Seconds 6

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
function Get-ButtonByNamePrefix([string]$prefix) {
  $root = Get-Root
  $cond = New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::Button)),
    (New-Object System.Windows.Automation.PropertyCondition -ArgumentList @(
      [System.Windows.Automation.AutomationElement]::NameProperty, ($prefix + "*"))))
  return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}
function Click-El($el) {
  if (-not $el) { throw "element is null" }
  # Prefer UIA Invoke: synthetic mouse clicks can be swallowed by window
  # activation (first click only focuses the window).
  try {
    $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    return
  } catch { }
  Click-ElementCenter $el
}

Step "D01 nav Data Import" { Click-El (Get-NavItem 3) }
Step "D02 shot import step" { Save-Shot "d1-import-step.png" }

Step "D03 import front design" {
  $btn = Get-ButtonByName "Browse Front Design"
  if (-not $btn) { throw "Browse Front Design button not found" }
  Click-El $btn
}
Step "D04 shot front loaded" { Save-Shot "d2-front-loaded.png" }

Step "D05 import back design" {
  $btn = Get-ButtonByName "Browse Back Design"
  if (-not $btn) { throw "Browse Back Design button not found" }
  Click-El $btn
}
Step "D06 shot back loaded" { Save-Shot "d3-back-loaded.png" }

Step "D07 import Excel" {
  $btn = Get-ButtonByName "Browse Data File"
  if (-not $btn) { throw "Browse Data File button not found" }
  Click-El $btn
}
Step "D08 shot excel loaded" { Save-Shot "d4-excel-loaded.png" }

Step "D09 import photos" {
  $btn = Get-ButtonByName "Browse Photo Folder"
  if (-not $btn) { throw "Browse Photo Folder button not found" }
  Click-El $btn
}
Step "D10 shot import complete" { Save-Shot "d5-import-complete.png" }

Step "D11 Next: Mapping" {
  $btn = Get-ButtonByName "Next Mapping"
  if (-not $btn) { throw "Next Mapping button not found" }
  Click-El $btn
}
Step "D12 shot front mapping" { Save-Shot "d6-front-mapping.png" }

Step "D13 switch to back mapping" {
  $radio = Find-ByName "Back Mapping Tab" "RadioButton"
  if (-not $radio) { throw "Back Mapping Tab radio not found" }
  Click-El $radio
}
Step "D14 shot back mapping" { Save-Shot "d7-back-mapping.png" }

Step "D15 Next: Preview" {
  $btn = Get-ButtonByName "Next Preview"
  if (-not $btn) { throw "Next Preview button not found" }
  Click-El $btn
}
Step "D16 shot preview+validate" { Save-Shot "d8-preview-validate.png" }

Write-Host "`n===== RESULTS ====="
foreach ($r in $results) { Write-Host $r }
$failCount = ($results | Where-Object { $_ -like "FAIL*" }).Count
Write-Host ("TOTAL: {0}  FAIL: {1}" -f $results.Count, $failCount)
