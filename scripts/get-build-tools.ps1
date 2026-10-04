$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$toolsDir = Join-Path $repoRoot '.tools'
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
$download = Join-Path $toolsDir 'innosetup-6.7.3.exe'
Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' -OutFile $download
$expected = '9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732'
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Inno Setup download checksum mismatch' }
$compilerDir = Join-Path $toolsDir 'InnoSetup'
$proc = Start-Process -FilePath $download -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER',('/DIR="' + $compilerDir + '"'),'/NOICONS') -WindowStyle Hidden -Wait -PassThru
if ($proc.ExitCode -ne 0) { throw "Build-tool setup failed: $($proc.ExitCode)" }
Write-Output "Compiler installed at $compilerDir\ISCC.exe"
