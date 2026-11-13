param(
    [string]$CompilerPath,
    [string]$Version = '0.9.0'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Surum uc sayili olmalidir. Ornek: 0.9.0'
}

# Ek Ders Hakediş Sistemi: tek EXE, temiz yayin paketi ve otomatik ISCC tespiti.
# Eski kurulum AppId/kurulum dizini ve veri konumu degistirilmez.
if (-not $CompilerPath) {
    $candidatePaths = @()
    $onPath = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($onPath) { $candidatePaths += $onPath.Source }
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)}, $env:LOCALAPPDATA)) {
        if (-not [string]::IsNullOrWhiteSpace($base)) {
            $candidatePaths += (Join-Path $base 'Inno Setup 6\ISCC.exe')
            $candidatePaths += (Join-Path $base 'Programs\Inno Setup 6\ISCC.exe')
        }
    }
    $candidatePaths += (Join-Path $root 'artifacts\tools\InnoSetup\ISCC.exe')
    $CompilerPath = $candidatePaths | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) {
    throw 'Inno Setup 6.6+ bulunamadi. Resmi Inno Setup 6 kurun veya -CompilerPath ISCC.exe dosyasini verin.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Gelisitirme bilgisayarinda .NET 10 SDK bulunamadi.'
}

$payload = Join-Path $root ('dist\installer-payload-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $payload -Force | Out-Null
$project = Join-Path $root 'src\GorevYolluklari.App\GorevYolluklari.App.csproj'
& dotnet publish $project -c Release -r win-x64 --self-contained true -o $payload `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Tek EXE derlemesi basarisiz.' }

$guide = Join-Path $root 'docs\ASAMA8_KULLANIM.md'
Copy-Item -LiteralPath $guide -Destination (Join-Path $payload 'KULLANIM.md')
$expected = @(
    'GorevYolluklari.exe',
    'KULLANIM.md',
    'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli_Dogrulanmis_OnHesap.json'
)
foreach ($name in $expected) {
    if (-not (Test-Path -LiteralPath (Join-Path $payload $name) -PathType Leaf)) {
        throw "Yayinda gereken dosya eksik: $name"
    }
}
# Yalniz program, yardim ve dogrulanmis tarife; asla kullanici verisi/yedek/PDF.
$unexpected = @(
    Get-ChildItem -LiteralPath $payload -File -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($payload.TrimEnd('\').Length + 1)
        $relative -notin $expected
    }
)
if ($unexpected.Count -gt 0) {
    throw ('Yayinda beklenmeyen dosyalar var; kurulum durduruldu: ' +
        ($unexpected.Name -join ', '))
}

$setup = Join-Path $root 'installer\Gorev-Yolluklari-Hesaplama-Sistemi.iss'
& $CompilerPath "/DAppVersion=$Version" "/DPayloadDir=$payload" $setup
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup derlemesi basarisiz.' }
$installer = Join-Path $root "dist\installer\Gorev-Yolluklari-Hesaplama-Sistemi-Kurulum-$Version.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
    throw 'Kurulum dosyasi olusmadi.'
}
Get-Item -LiteralPath $installer | Select-Object FullName, Length
Get-FileHash -LiteralPath $installer -Algorithm SHA256
