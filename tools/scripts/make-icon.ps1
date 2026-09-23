# Generates Assets/app.ico — an "ID" badge icon in multiple sizes.
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

$outDir = Join-Path $PSScriptRoot "..\..\src\IDStack\Assets"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outPath = Join-Path $outDir "app.ico"

$sizes = @(16, 24, 32, 48, 64, 128, 256)

function New-BadgeBitmap([int]$size) {
  $bmp = New-Object System.Drawing.Bitmap($size, $size)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

  $scaled = [float]($size / 256.0)

  # Rounded-rect badge background with vertical gradient
  $bgRect = [System.Drawing.RectangleF]::new(
    [float](8.0 * $scaled), [float](8.0 * $scaled),
    [float](240.0 * $scaled), [float](240.0 * $scaled))

  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = [float](96.0 * $scaled)
  $path.AddArc($bgRect.X, $bgRect.Y, $d, $d, 180, 90)
  $path.AddArc($bgRect.Right - $d, $bgRect.Y, $d, $d, 270, 90)
  $path.AddArc($bgRect.Right - $d, $bgRect.Bottom - $d, $d, $d, 0, 90)
  $path.AddArc($bgRect.X, $bgRect.Bottom - $d, $d, $d, 90, 90)
  $path.CloseFigure()

  $c1 = [System.Drawing.Color]::FromArgb(255, 37, 99, 235)
  $c2 = [System.Drawing.Color]::FromArgb(255, 29, 78, 216)
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bgRect, $c1, $c2, [float]90)
  $g.FillPath($brush, $path)

  $cardPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [float](10.0 * $scaled))
  $cx = [float](100.0 * $scaled); $cy = [float](60.0 * $scaled)
  $cw = [float](100.0 * $scaled); $ch = [float](136.0 * $scaled)
  $g.DrawRectangle($cardPen, $cx, $cy, $cw, $ch)

  $white = [System.Drawing.Brushes]::White
  $g.FillRectangle($white, [float](116.0*$scaled), [float](80.0*$scaled), [float](36.0*$scaled), [float](44.0*$scaled))
  $g.FillRectangle($white, [float](116.0*$scaled), [float](140.0*$scaled), [float](68.0*$scaled), [float](9.0*$scaled))
  $g.FillRectangle($white, [float](116.0*$scaled), [float](160.0*$scaled), [float](52.0*$scaled), [float](9.0*$scaled))

  $font = New-Object System.Drawing.Font("Segoe UI", [float](92.0*$scaled), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
  $fmt = New-Object System.Drawing.StringFormat
  $fmt.Alignment = [System.Drawing.StringAlignment]::Center
  $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
  $textRect = [System.Drawing.RectangleF]::new([float](12.0*$scaled), [float](118.0*$scaled), [float](90.0*$scaled), [float](120.0*$scaled))
  $g.DrawString("ID", $font, $white, $textRect, $fmt)

  $g.Dispose()
  return $bmp
}

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)

$imageBlobs = @()
foreach ($s in $sizes) {
  $bmp = New-BadgeBitmap $s
  $pngMs = New-Object System.IO.MemoryStream
  $bmp.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
  $imageBlobs += ,@($pngMs.ToArray(), $s)
  $bmp.Dispose()
  $pngMs.Dispose()
}

$dataOffset = 6 + $imageBlobs.Count * 16
foreach ($blob in $imageBlobs) {
  $pngBytes = $blob[0]; $s = $blob[1]
  $dim = if ($s -ge 256) { [Byte]0 } else { [Byte]$s }
  $bw.Write($dim); $bw.Write($dim)
  $bw.Write([Byte]0)
  $bw.Write([Byte]0)
  $bw.Write([UInt16]1)
  $bw.Write([UInt16]32)
  $bw.Write([UInt32]$pngBytes.Length)
  $bw.Write([UInt32]$dataOffset)
}

foreach ($blob in $imageBlobs) { $bw.Write($blob[0]) }

[System.IO.File]::WriteAllBytes($outPath, $ms.ToArray())
$bw.Dispose(); $ms.Dispose()
Write-Host "Icon written: $outPath ($((Get-Item $outPath).Length) bytes)"
