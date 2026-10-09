$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function New-QuietMonitorBitmap([int]$width, [int]$height) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::FromArgb(9, 17, 27))

    $side = [Math]::Min($width, $height)
    $pad = [Math]::Max(2, [int]($side * 0.15))
    $rect = [System.Drawing.Rectangle]::new([int](($width - $side) / 2 + $pad), $pad, $side - 2 * $pad, $side - 2 * $pad)
    $cyan = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(89, 214, 231))
    $graphics.FillEllipse($cyan, $rect)

    $penWidth = [Math]::Max(2, [single]($side * 0.055))
    $inkPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(5, 19, 23), $penWidth)
    $inkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $inkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $x1 = [single]($width * 0.27); $x2 = [single]($width * 0.40); $x3 = [single]($width * 0.47); $x4 = [single]($width * 0.50)
    $x5 = [single]($width * 0.53); $x6 = [single]($width * 0.60); $x7 = [single]($width * 0.73)
    $mid = [single]($height * 0.52); $high = [single]($height * 0.31); $low = [single]($height * 0.73)
    $graphics.DrawBezier($inkPen, $x1, $mid, $x2, $high, $x3, $high, $x4, $mid)
    $graphics.DrawBezier($inkPen, $x4, $mid, $x5, $low, $x6, $low, $x7, $mid)

    $inkPen.Dispose(); $cyan.Dispose(); $graphics.Dispose()
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

$assetDirectory = Join-Path $root 'Assets'
$assetCount = 0
Get-ChildItem -LiteralPath $assetDirectory -Filter '*.png' -File | ForEach-Object {
    $existing = [System.Drawing.Image]::FromFile($_.FullName)
    $width = $existing.Width
    $height = $existing.Height
    $existing.Dispose()

    $assetBitmap = New-QuietMonitorBitmap $width $height
    $assetBitmap.Save($_.FullName, [System.Drawing.Imaging.ImageFormat]::Png)
    $assetBitmap.Dispose()
    $assetCount++
}

Write-Host "Generated Installer\QuietMonitor.ico and $assetCount Game Bar assets"
