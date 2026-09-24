# Dump the current UIA tree of the running IDStack window: buttons, list items, and texts.
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$ErrorActionPreference = "Continue"

$proc = Get-Process IDStack -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $proc) { Write-Host "APP NOT RUNNING"; exit 1 }

$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)

$buttons = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Button)))
Write-Host ("BUTTONS ({0}):" -f $buttons.Count)
foreach ($b in $buttons) { Write-Host ("  '{0}'" -f $b.Current.Name) }

$combos = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ComboBox)))
Write-Host ("COMBOS ({0}):" -f $combos.Count)
foreach ($c in $combos) { Write-Host ("  '{0}'" -f $c.Current.Name) }

$texts = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Text)))
Write-Host ("TEXTS ({0}) [first 40]:" -f $texts.Count)
$n = 0
foreach ($t in $texts) {
  if ($n -ge 40) { break }
  Write-Host ("  '{0}'" -f $t.Current.Name)
  $n++
}
