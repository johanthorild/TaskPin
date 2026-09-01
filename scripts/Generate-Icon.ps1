param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\Assets\TaskPin.ico")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing.Common

function New-RoundedRectanglePath {
    param(
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $stream = [System.IO.MemoryStream]::new()

    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.ScaleTransform($Size / 256.0, $Size / 256.0)

        $shadowPath = New-RoundedRectanglePath 19 13 218 234 28
        $shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(72, 0, 0, 0))
        $graphics.FillPath($shadowBrush, $shadowPath)

        $bodyPath = New-RoundedRectanglePath 13 7 218 234 28
        $bodyBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 8, 78, 133))
        $graphics.FillPath($bodyBrush, $bodyPath)

        $facePath = New-RoundedRectanglePath 23 17 198 214 20
        $faceBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 17, 125, 200))
        $graphics.FillPath($faceBrush, $facePath)

        $paperPath = New-RoundedRectanglePath 57 34 145 188 10
        $paperBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 247, 250, 252))
        $graphics.FillPath($paperBrush, $paperPath)

        $headerBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 8, 78, 133))
        $graphics.FillRectangle($headerBrush, 77, 53, 105, 14)

        $boxPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 17, 125, 200), 6)
        $boxPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $linePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 79, 98, 113), 8)
        $linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

        foreach ($rowY in @(91, 133, 175)) {
            $graphics.DrawRectangle($boxPen, 77, $rowY, 22, 22)
            $graphics.DrawLine($linePen, 119, $rowY + 6, 181, $rowY + 6)
            $graphics.DrawLine($linePen, 119, $rowY + 18, 164, $rowY + 18)
        }

        $checkPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 87, 214, 161), 11)
        $checkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $checkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $checkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        [System.Drawing.PointF[]]$checkPoints = @(
            [System.Drawing.PointF]::new(73, 99),
            [System.Drawing.PointF]::new(85, 111),
            [System.Drawing.PointF]::new(106, 84)
        )
        $graphics.DrawLines($checkPen, $checkPoints)

        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $stream.Dispose()
        $shadowPath.Dispose()
        $shadowBrush.Dispose()
        $bodyPath.Dispose()
        $bodyBrush.Dispose()
        $facePath.Dispose()
        $faceBrush.Dispose()
        $paperPath.Dispose()
        $paperBrush.Dispose()
        $headerBrush.Dispose()
        $boxPen.Dispose()
        $linePen.Dispose()
        $checkPen.Dispose()
    }
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = foreach ($size in $sizes) {
    [pscustomobject]@{ Size = $size; Bytes = New-IconPng $size }
}

$outputDirectory = Split-Path $OutputPath -Parent
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$fileStream = [System.IO.File]::Create($OutputPath)
$writer = [System.IO.BinaryWriter]::new($fileStream)

try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    foreach ($image in $images) {
        $dimension = if ($image.Size -eq 256) { 0 } else { $image.Size }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$image.Bytes.Length)
        $writer.Write([uint32]$offset)
        $offset += $image.Bytes.Length
    }

    foreach ($image in $images) {
        $writer.Write([byte[]]$image.Bytes)
    }
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
}

Write-Output "Generated $OutputPath with $($images.Count) sizes."