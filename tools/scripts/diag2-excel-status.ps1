# Full editor PSD verification: launch app with IDSTACK_AUTO_OPEN, navigate to
# Template Editor, invoke Open (auto-hook loads the PSD), then screenshot.
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type @'
using System; using System.Runtime.InteropServices;
public class W { [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y); [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr i); }
'@

Get-Process IDStack -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
$env:IDSTACK_AUTO_OPEN = "D:\ID-Stack\tools\sample-data\demopsd.psd"
Start-Process "D:\ID-Stack\src\IDStack\bin\Debug\net48\IDStack.exe"
Start-Sleep -Seconds 6

function Click-El($el) {
  $r = $el.Current.BoundingRectangle
  $x = [int]($r.X + $r.Width / 2); $y = [int]($r.Y + $r.Height / 2)
  [W]::SetCursorPos($x, $y) | Out-Null
  Start-Sleep -Milliseconds 80
  [W]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
  [W]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
}

$proc = Get-Process IDStack -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $proc) { Write-Host "NO APP"; exit 1 }
[W]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500
$root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$proc.MainWindowHandle)

# Navigate to Template Editor
$items = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)))
foreach ($i in $items) { if ($i.Current.Name -eq "Template Editor") { Click-El $i; break } }
Start-Sleep -Seconds 2

# Invoke Open (auto hook loads the PSD without a dialog)
$btns = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Button)))
foreach ($b in $btns) {
  if ($b.Current.Name -eq "Open") {
    try { ($b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke(); Write-Host "Invoked Open" }
    catch { Click-El $b; Write-Host "Clicked Open (mouse)" }
    break
  }
}
Start-Sleep -Seconds 6

# Count Layers list items
$layers = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
  (New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)))
Write-Host ("ListItems=" + $layers.Count)

# Screenshot
[W]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500
$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
$g.Dispose()
$bmp.Save("D:\ID-Stack\tools\ui-test\e3-psd-canvas.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "saved e3-psd-canvas.png"
