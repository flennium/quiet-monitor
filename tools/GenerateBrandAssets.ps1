$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function New-QuietMonitorBitmap([int]$width, [int]$height) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(11, 16, 23))

    $side = [Math]::Min($width, $height)
    $pad = [Math]::Max(2, [int]($side * 0.15))
    $rect = [System.Drawing.Rectangle]::new([int](($width - $side) / 2 + $pad), $pad, $side - 2 * $pad, $side - 2 * $pad)
    $blue = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(66, 135, 245))
    $graphics.FillEllipse($blue, $rect)

    $penWidth = [Math]::Max(2, [single]($side * 0.055))
    $whitePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, $penWidth)
    $whitePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $whitePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $cx = $width / 2; $cy = $height / 2; $r = $side * 0.19
    $graphics.DrawArc($whitePen, [single]($cx - $r), [single]($cy - $r), [single](2 * $r), [single](2 * $r), 35, 295)
    $graphics.DrawLine($whitePen, [single]($cx + $r * 0.55), [single]($cy + $r * 0.55), [single]($cx + $r * 1.05), [single]($cy + $r * 1.05))

    $mintPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(111, 212, 176), [Math]::Max(1, [single]($side * 0.025)))
    $y = [single]($height - $pad * 0.65)
    $graphics.DrawLine($mintPen, [single]($width * 0.28), $y, [single]($width * 0.72), $y)

    $mintPen.Dispose(); $whitePen.Dispose(); $blue.Dispose(); $graphics.Dispose()
    return $bitmap
}

$iconBitmap = New-QuietMonitorBitmap 256 256
$pngStream = [System.IO.MemoryStream]::new()
$iconBitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $pngStream.ToArray()
$ico = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($ico)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
$writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
$writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$png.Length); $writer.Write([uint32]22)
$writer.Write($png); $writer.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $root 'Installer\QuietMonitor.ico'), $ico.ToArray())
$writer.Dispose(); $ico.Dispose(); $pngStream.Dispose(); $iconBitmap.Dispose()

Write-Host 'Generated Installer\QuietMonitor.ico'
