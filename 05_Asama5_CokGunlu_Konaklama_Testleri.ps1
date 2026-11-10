$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$tariff = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli_Dogrulanmis_OnHesap.json'
$kanun = Join-Path $root 'Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu.pdf'
$hpdf = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli.pdf'
$project = Join-Path $root 'tests\GorevYolluklari.MultiDayChecks\GorevYolluklari.MultiDayChecks.csproj'
$regression = Join-Path $root '04_Asama4_Birlesik_OnHesap_Testleri.ps1'
Write-Host '=== ASAMA 5: DOSYA / KAYNAK KONTROLU ===' -ForegroundColor Cyan
foreach ($file in @($kanun, $hpdf, $tariff, $project, $regression)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Eksik: $file" }
    Write-Host "[OK] $(Split-Path -Leaf $file)" -ForegroundColor Green
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK kurulu degil.' }
Write-Host '=== ASAMA 5: COK GUNLU / KONAKLAMA TESTLERI ===' -ForegroundColor Cyan
& dotnet run --project $project --configuration Release -- $tariff
if ($LASTEXITCODE -ne 0) { throw 'ASAMA 5 kontrolu BASARISIZ.' }
Write-Host '=== REGRESYON: ASAMA 4 + ASAMA 2/3 ===' -ForegroundColor Cyan
& $regression
if ($LASTEXITCODE -ne 0) { throw 'Eski asama regresyon kontrolleri BASARISIZ.' }
Write-Host '[OK] ASAMA 5 ve onceki kontroller tamamlandi.' -ForegroundColor Green
Write-Warning 'Sadece 2026 yurtici, genel memur tarifesi ve ON HESAP. Resmi bordro/pdF uretilmedi.'
