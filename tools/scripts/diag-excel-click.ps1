# Select Import tab via pattern, then mouse-click Browse Data File and observe.
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @'
using System; using System.Runtime.InteropServices;
public class W { [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y); [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr i); }
'@

function Click-El($el) {
  $r = $el.Current.BoundingRectangle
  $x = [int]($r.X + $r.Width / 2); $y = [int]($r.Y + $r.Height / 2)
  [W]::SetCursorPos($x, $y) | Out-Null
  Start-Sleep -Milliseconds 80
  [W]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
  [W]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
}

$proc = Get-Process IDStack -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$proc.MainWindowHandle)

$items = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)))
foreach ($i in $items) { if ($i.Current.Name -eq "1. Import") { ($i.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)).Select() } }
Start-Sleep -Milliseconds 1200

$btn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::Button)),
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, "Browse Data File")))))
if (-not $btn) { Write-Host "btn gone"; exit 1 }
Write-Host ("Clicking at " + $btn.Current.BoundingRectangle.ToString())
Click-El $btn
Start-Sleep -Seconds 5

# Look for status text changes — dump all Text elements containing "loaded" or "rows".
$texts = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Text)))
foreach ($t in $texts) {
  $n = $t.Current.Name
  if ($n -match "rows|loaded|Imported|photo") { Write-Host ("TXT: " + $n) }
}
