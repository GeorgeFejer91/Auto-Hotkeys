$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildDir = Join-Path $repoRoot 'build'
$app = Join-Path $buildDir 'Auto-Hotkeys.exe'
if (!(Test-Path -LiteralPath $app)) { & (Join-Path $PSScriptRoot 'build.ps1') -AppOnly }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$tests = Join-Path $buildDir 'AutoHotkeys.Tests.exe'
& $csc /nologo /target:exe /platform:anycpu ('/r:' + $app) /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Runtime.Serialization.dll ('/out:' + $tests) (Join-Path $repoRoot 'tests\AutoHotkeys.Tests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& $tests
if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed' }
