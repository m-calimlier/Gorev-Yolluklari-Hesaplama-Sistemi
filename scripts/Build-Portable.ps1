param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
if ($Runtime -notin @('win-x64', 'win-arm64')) { throw 'Desteklenen Windows hedefleri: win-x64, win-arm64.' }
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\GorevYolluklari.App\GorevYolluklari.App.csproj'
$release = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$target = Join-Path $root ('dist\GorevYolluklari_' + $Runtime + '_' + $release)
$archive = $target + '.zip'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Gelistirici bilgisayarinda .NET 10 SDK bulunmali.' }
if (Test-Path -LiteralPath $target) { throw 'Yayin hedefi mevcut; uzerine yazilmadi.' }
if (Test-Path -LiteralPath $archive) { throw 'ZIP hedefi mevcut; uzerine yazilmadi.' }
& dotnet publish $project -c Release -r $Runtime --self-contained true -o $target `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Yayin basarisiz.' }
Copy-Item -LiteralPath (Join-Path $root 'docs\ASAMA8_KULLANIM.md') -Destination (Join-Path $target 'KULLANIM.md')
$expected = @(
    'GorevYolluklari.exe',
    'KULLANIM.md',
    'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli_Dogrulanmis_OnHesap.json'
)
foreach ($name in $expected) {
    if (-not (Test-Path -LiteralPath (Join-Path $target $name) -PathType Leaf)) {
        throw "Yayinda zorunlu dosya eksik: $name"
    }
}
$unexpected = @(
    Get-ChildItem -LiteralPath $target -File -Recurse -Force | Where-Object {
        $relative = $_.FullName.Substring($target.TrimEnd('\').Length + 1)
        $relative -notin $expected
    }
)
if ($unexpected.Count) { throw ('Kullanici verisi veya beklenmeyen dosya nedeniyle yayin durduruldu: ' + ($unexpected.Name -join ', ')) }
Compress-Archive -Path (Join-Path $target '*') -DestinationPath $archive
Get-Item -LiteralPath $archive | Select-Object FullName, Length
Get-FileHash -LiteralPath $archive -Algorithm SHA256
Write-Host 'Tasınabilir Windows paketi hazir. Kullanici kayitlari, yedekleri ve PDF ciktisi paketlenmez.' -ForegroundColor Green
