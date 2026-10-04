param([string]$InnoCompiler, [switch]$AppOnly)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildDir = Join-Path $repoRoot 'build'
$distDir = Join-Path $repoRoot 'dist'
New-Item -ItemType Directory -Path $buildDir,$distDir -Force | Out-Null
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path -LiteralPath $csc)) { throw '.NET Framework compiler not found. Windows 10/11 with .NET Framework 4.8 is required.' }
$icon = Join-Path $repoRoot 'assets\App.ico'
if (!(Test-Path -LiteralPath $icon)) { & (Join-Path $PSScriptRoot 'generate-icon.ps1') }
$app = Join-Path $buildDir 'Auto-Hotkeys.exe'
$sources = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src\AutoHotkeys') -Filter '*.cs' | Sort-Object Name | ForEach-Object FullName
$compileArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Runtime.Serialization.dll','/r:Microsoft.CSharp.dll',('/win32icon:' + $icon),('/resource:' + $icon + ',AutoHotkeys.App.ico'),('/out:' + $app)) + $sources
& $csc $compileArgs
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed' }
Write-Output "Built: $app"
if ($AppOnly) { return }
if (!$InnoCompiler) {
    $candidates = @((Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),(Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),(Join-Path $repoRoot '.tools\InnoSetup\ISCC.exe'))
    $InnoCompiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$InnoCompiler) { throw 'Install Inno Setup 6, or run scripts/get-build-tools.ps1, then build again.' }
& $InnoCompiler ('/DRepoRoot=' + $repoRoot) (Join-Path $repoRoot 'installer\Auto-Hotkeys.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
$versionLine = Get-Content -LiteralPath (Join-Path $repoRoot 'installer\Auto-Hotkeys.iss') | Where-Object { $_ -match '^#define AppVersion "([0-9.]+)"$' }
if (!$versionLine -or $versionLine -notmatch '^#define AppVersion "([0-9.]+)"$') { throw 'Installer version is missing' }
$installer = Join-Path $distDir ('Auto-Hotkeys-Setup-' + $Matches[1] + '.exe')
Get-FileHash -LiteralPath $installer -Algorithm SHA256 | ForEach-Object { $_.Hash.ToLowerInvariant() + '  ' + (Split-Path -Leaf $_.Path) } | Set-Content -LiteralPath (Join-Path $distDir 'SHA256SUMS.txt') -Encoding ascii
