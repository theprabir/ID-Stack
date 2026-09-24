# Coordinate-based UI test driver: clicks by relative position inside the ID Stack window.
# Reliable where UIA name lookup fails (custom-listed ListBox items).
param()
Add-Type -AssemblyName System.Windows.Forms, System.Drawing

$script:ShotDir = "D:\ID-Stack\tools\ui-test"

Add-Type '
using System;
using System.Runtime.InteropServices;
public class W32b {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint data, UIntPtr extra);
  public struct R { public int L, T, Rt, B; }
}'

function Get-App {
  Get-Process IDStack -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
}

function Send-Click([int]$x, [int]$y) {
  [W32b]::SetCursorPos($x, $y) | Out-Null
  Start-Sleep -Milliseconds 80
  [W32b]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)  # down
  [W32b]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)  # up
}

function Click-Rel([double]$relX, [double]$relY) {
  $proc = Get-App
  if (-not $proc) { throw "App not running" }
  [W32b]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
  Start-Sleep -Milliseconds 250
  $r = New-Object W32b+R
  [W32b]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
  $x = [int]($r.L + ($r.Rt - $r.L) * $relX)
  $y = [int]($r.T + ($r.B - $r.T) * $relY)
  Send-Click $x $y
}

function Save-Shot([string]$name) {
  $proc = Get-App
  if (-not $proc) { throw "App not running" }
  [W32b]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
  Start-Sleep -Milliseconds 400
  $r = New-Object W32b+R
  [W32b]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
  $bmp = New-Object System.Drawing.Bitmap(($r.Rt - $r.L), ($r.B - $r.T))
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
  $out = Join-Path $script:ShotDir $name
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Host "shot: $name"
}
