param([Parameter(Mandatory)][string]$CompilerPath,[string]$Version='0.8.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if($Version -notmatch '^\d+\.\d+\.\d+$'){throw 'Geçersiz sürüm.'}
$payload=Join-Path $root ('dist/installer-payload-'+[guid]::NewGuid().ToString('N'))
& dotnet publish (Join-Path $root 'src/GorevYolluklari.App') -c Release -r win-x64 --self-contained true -o $payload -p:DebugType=None -p:DebugSymbols=false
if($LASTEXITCODE -ne 0){throw 'Yayın derlenemedi.'}
Copy-Item -LiteralPath (Join-Path $root 'docs/ASAMA8_KULLANIM.md') -Destination (Join-Path $payload 'KULLANIM.md')
& $CompilerPath "/DAppVersion=$Version" "/DPayloadDir=$payload" (Join-Path $root 'installer/Gorev-Yolluklari-Hesaplama-Sistemi.iss')
if($LASTEXITCODE -ne 0){throw 'Kurulum derlenemedi.'}
Get-FileHash -LiteralPath (Join-Path $root "dist/installer/Gorev-Yolluklari-Hesaplama-Sistemi-Kurulum-$Version.exe") -Algorithm SHA256