# Original SnapMark capture-frame + markup-pen logo; no font or third-party art.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskAssets = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\SnapMark.App\Assets'
New-Item -ItemType Directory -Path $taskAssets -Force | Out-Null
$taskFrames = @()
foreach ($taskSize in @(16,20,24,32,40,48,64,128,256)) {
    $taskBitmap = [Drawing.Bitmap]::new($taskSize*4,$taskSize*4)
    $taskGraphics = [Drawing.Graphics]::FromImage($taskBitmap)
    $taskGraphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $taskGraphics.ScaleTransform($taskSize*4/24.0,$taskSize*4/24.0)
    $taskBackground = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#0F172A'))
    $taskPath = [Drawing.Drawing2D.GraphicsPath]::new()
    foreach ($taskArc in @(@(0,0,10,10,180),@(14,0,10,10,270),@(14,14,10,10,0),@(0,14,10,10,90))) { $taskPath.AddArc($taskArc[0],$taskArc[1],$taskArc[2],$taskArc[3],$taskArc[4],90) }
    $taskPath.CloseFigure(); $taskGraphics.FillPath($taskBackground,$taskPath)
    $taskCyan = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#38BDF8'),1.8)
    $taskCyan.StartCap = $taskCyan.EndCap = [Drawing.Drawing2D.LineCap]::Round
    foreach ($taskLine in @(@(5,9,5,5),@(5,5,9,5),@(15,5,19,5),@(19,5,19,9),@(19,15,19,19),@(19,19,15,19),@(9,19,5,19),@(5,19,5,15))) { $taskGraphics.DrawLine($taskCyan,[single]$taskLine[0],[single]$taskLine[1],[single]$taskLine[2],[single]$taskLine[3]) }
    $taskPen = [Drawing.Pen]::new([Drawing.Color]::White,3)
    $taskGraphics.DrawLine($taskPen,8.5,15.5,15.5,8.5)
    $taskGold = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#FBBF24'),3)
    $taskGraphics.DrawLine($taskGold,15.5,8.5,17,7)
    $taskNib = [Drawing.SolidBrush]::new([Drawing.Color]::White)
    $taskGraphics.FillPolygon($taskNib,[Drawing.PointF[]]@([Drawing.PointF]::new(6.5,17.5),[Drawing.PointF]::new(7.2,14.7),[Drawing.PointF]::new(9.3,16.8)))
    $taskSmall = [Drawing.Bitmap]::new($taskSize,$taskSize); $taskResize = [Drawing.Graphics]::FromImage($taskSmall)
    $taskResize.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $taskResize.DrawImage($taskBitmap,0,0,$taskSize,$taskSize)
    $taskStream = [IO.MemoryStream]::new(); $taskSmall.Save($taskStream,[Drawing.Imaging.ImageFormat]::Png)
    $taskFrames += ,@{Size=$taskSize;Bytes=$taskStream.ToArray()}
    if ($taskSize -eq 256) { $taskSmall.Save((Join-Path $taskAssets 'SnapMark.png'),[Drawing.Imaging.ImageFormat]::Png) }
    foreach ($taskObject in @($taskStream,$taskResize,$taskSmall,$taskNib,$taskGold,$taskPen,$taskCyan,$taskPath,$taskBackground,$taskGraphics,$taskBitmap)) { $taskObject.Dispose() }
}
$taskFile = [IO.File]::Create((Join-Path $taskAssets 'SnapMark.ico')); $taskWriter = [IO.BinaryWriter]::new($taskFile)
try {
    $taskWriter.Write([uint16]0); $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]$taskFrames.Count)
    $taskOffset = 6+16*$taskFrames.Count
    foreach ($taskFrame in $taskFrames) {
        $taskDimension = if ($taskFrame.Size -eq 256) {0} else {$taskFrame.Size}
        $taskWriter.Write([byte]$taskDimension); $taskWriter.Write([byte]$taskDimension); $taskWriter.Write([byte]0); $taskWriter.Write([byte]0)
        $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]32); $taskWriter.Write([uint32]$taskFrame.Bytes.Length); $taskWriter.Write([uint32]$taskOffset)
        $taskOffset += $taskFrame.Bytes.Length
    }
    foreach ($taskFrame in $taskFrames) { $taskWriter.Write([byte[]]$taskFrame.Bytes) }
} finally { $taskWriter.Dispose() }
