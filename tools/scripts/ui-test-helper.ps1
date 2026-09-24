# UI test helper: clicks UI Automation peers inside the ID Stack window and captures screenshots.
# Usage: . ui-test-helper.ps1  then call functions.
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing

$script:Proc = $null
$script:ShotDir = "D:\ID-Stack\tools\ui-test"

function Get-App {
  if (-not $script:Proc -or $script:Proc.HasExited) {
    $script:Proc = Get-Process IDStack -ErrorAction SilentlyContinue |
      Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
  }
  return $script:Proc
}

function Find-Element([string]$automationId, [string]$name, [string]$controlType) {
  $proc = Get-App
  if (-not $proc) { throw "App not running" }
  $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
  $cond = New-Object System.Windows.Automation.AndCondition(
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
      [System.Windows.Automation.ControlType]::$controlType)),
    (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, $name)))
  return $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Invoke-Click([System.Windows.Automation.AutomationElement]$el) {
  if (-not $el) { throw "Element not found" }
  $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
  $invoke.Invoke()
}

function Select-Item([System.Windows.Automation.AutomationElement]$el) {
  if (-not $el) { throw "Element not found" }
  $sel = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
  $sel.Select()
}

function Expand-Collapse([System.Windows.Automation.AutomationElement]$el) {
  if (-not $el) { throw "Element not found" }
  $exp = $el.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
  $exp.Expand()
}

function Save-Shot([string]$name) {
  $proc = Get-App
  if (-not $proc) { throw "App not running" }
  Add-Type '
  using System;
  using System.Runtime.InteropServices;
  public class W32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    public struct R { public int L, T, Rt, B; }
  }'
  [W32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
  Start-Sleep -Milliseconds 400
  $r = New-Object W32+R
  [W32]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
  $bmp = New-Object System.Drawing.Bitmap(($r.Rt - $r.L), ($r.B - $r.T))
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
  $out = Join-Path $script:ShotDir $name
  $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Host "shot: $name"
}
