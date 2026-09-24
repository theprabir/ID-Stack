# UIA test that discovers elements by ControlType + name, with coordinate fallback.
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
$ErrorActionPreference = "Continue"

$script:ShotDir = "D:\ID-Stack\tools\ui-test"

Add-Type '
using System;
using System.Runtime.InteropServices;
public class W32c {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  public struct R { public int L, T, Rt, B; }
}'

function Get-App {
  Get-Process IDStack -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
}

function Get-Root {
  $proc = Get-App
  [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
}

function Dump-Sidebar {
  $root = Get-Root
  $list = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::List)))
  if (-not $list) { Write-Host "NO LIST FOUND"; return }
  $items = $list.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::ListItem)))
  foreach ($i in $items) {
    $r = $i.Current.BoundingRectangle
    Write-Host ("Item: '{0}'  rect=({1},{2})-({3},{4})" -f $i.Current.Name, [int]$r.X, [int]$r.Y, [int]($r.X+$r.Width), [int]($r.Y+$r.Height))
  }
}

function Click-ElementCenter([System.Windows.Automation.AutomationElement]$el) {
  $r = $el.Current.BoundingRectangle
  $x = [int]($r.X + $r.Width / 2)
  $y = [int]($r.Y + $r.Height / 2)
  [W32c]::SetCursorPos($x, $y) | Out-Null
  Start-Sleep -Milliseconds 60
  [W32c]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
  [W32c]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}

function Find-ByName([string]$name, [string]$type) {
  $root = Get-Root
  $cond = New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::$type)),
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, $name)))
  $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Save-Shot([string]$name) {
  $proc = Get-App
  if ([W32c]::IsIconic($proc.MainWindowHandle)) {
    [W32c]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
    Start-Sleep -Milliseconds 600
  }
  [W32c]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
  Start-Sleep -Milliseconds 400
  $r = New-Object W32c+R
  [W32c]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
  $w = $r.Rt - $r.L; $h = $r.B - $r.T
  if ($w -le 0 -or $h -le 0) { throw "window has no size" }
  $bmp = New-Object System.Drawing.Bitmap($w, $h)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  # PW_RENDERFULLCONTENT (2) captures DirectComposition/WPF content reliably.
  $ok = [W32c]::PrintWindow($proc.MainWindowHandle, $hdc, 2)
  $g.ReleaseHdc($hdc)
  if (-not $ok) { $g.Dispose(); $bmp.Dispose(); throw "PrintWindow failed" }
  $bmp.Save((Join-Path $script:ShotDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Host "shot: $name ($w x $h)"
}
