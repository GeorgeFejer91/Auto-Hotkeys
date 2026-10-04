$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class AutoHotkeysIconHandle {
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle);
}
'@
$assetsDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'assets'
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
$image = New-Object Drawing.Bitmap(64,64)
$graphics = [Drawing.Graphics]::FromImage($image)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([Drawing.Color]::Transparent)
$background = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(32,76,122))
$white = New-Object Drawing.SolidBrush([Drawing.Color]::White)
$graphics.FillRectangle($background,3,8,58,48)
$font = New-Object Drawing.Font('Segoe UI',22,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
$graphics.DrawString('AH',$font,$white,10,10)
foreach ($x in @(11,22,33,44)) { $graphics.FillRectangle($white,$x,43,8,5) }
$handle = $image.GetHicon()
try {
    $icon = [Drawing.Icon]::FromHandle($handle)
    $stream = [IO.File]::Create((Join-Path $assetsDir 'App.ico'))
    try { $icon.Save($stream) } finally { $stream.Dispose(); $icon.Dispose() }
} finally { [AutoHotkeysIconHandle]::DestroyIcon($handle) | Out-Null; $font.Dispose(); $white.Dispose(); $background.Dispose(); $graphics.Dispose(); $image.Dispose() }
