# End-to-end UI test for the redesigned Data Import flow (0.3.1):
# import design + Excel + photos, mapping, preview & validate.
# Launches the app itself with IDSTACK_AUTO_* env hooks (native Win32 dialogs
# cannot be scripted from a background session) so the hooks reach the app.
. "$PSScriptRoot\run-uia-click-test.ps1"

# Kill any stale instance, then start the app WITH the env hooks set.
Get-Process IDStack -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
$env:IDSTACK_AUTO_DESIGN = "D:\ID-Stack\tools\sample-data\sample-badge.idcard"
$env:IDSTACK_AUTO_IMPORT = "D:\ID-Stack\tools\sample-data\employees.csv"
$env:IDSTACK_AUTO_PHOTOS = "D:\ID-Stack\tools\sample-data\photos"
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
function Click-El($el) { if (-not $el) { throw "element is null" }; Click-ElementCenter $el }

function Click-StepTab([string]$name) {
  $root = Get-Root
  $items = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::ListItem)))
  foreach ($i in $items) {
    if ($i.Current.Name -eq $name) { Click-El $i; return }
  }
  throw "$name tab not found"
}

Step "D01 nav Data Import" { Click-El (Get-NavItem 3) }
Step "D02 shot import step" { Save-Shot "d1-import-step.png" }

Step "D03 import design (auto path)" {
  $btn = Get-ButtonByName "Browse Design"
  if (-not $btn) { throw "Browse Design button not found" }
  Click-El $btn
}
Step "D04 shot design loaded" { Save-Shot "d2-design-loaded.png" }

Step "D05 import Excel (auto path)" {
  $btn = Get-ButtonByName "Browse Data File"
  if (-not $btn) { throw "Browse Data File button not found" }
  Click-El $btn
}
Step "D06 shot excel loaded" { Save-Shot "d3-excel-loaded.png" }

Step "D07 import photos (auto path)" {
  $btn = Get-ButtonByName "Browse Photo Folder"
  if (-not $btn) { throw "Browse Photo Folder button not found" }
  Click-El $btn
}
Step "D08 shot import complete" { Save-Shot "d4-import-complete.png" }

Step "D09 Next: Mapping" {
  $btn = Get-ButtonByName "Next Mapping"
  if (-not $btn) { throw "Next Mapping button not found" }
  Click-El $btn
}
Step "D10 shot mapping" { Save-Shot "d5-mapping.png" }

Step "D11 Next: Preview" {
  $btn = Get-ButtonByName "Next Preview"
  if (-not $btn) { throw "Next Preview button not found" }
  Click-El $btn
}
Step "D12 shot preview+validate" { Save-Shot "d6-preview-validate.png" }

Write-Host "`n===== RESULTS ====="
foreach ($r in $results) { Write-Host $r }
$failCount = ($results | Where-Object { $_ -like "FAIL*" }).Count
Write-Host ("TOTAL: {0}  FAIL: {1}" -f $results.Count, $failCount)
