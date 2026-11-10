$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$kanun = Join-Path $root 'Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu.pdf'
$hpdf = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli.pdf'
$tariff = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli_Dogrulanmis_OnHesap.json'
$project = Join-Path $root 'tests\GorevYolluklari.TimelineChecks\GorevYolluklari.TimelineChecks.csproj'
$regression = Join-Path $root '05_Asama5_CokGunlu_Konaklama_Testleri.ps1'
Write-Host '=== ASAMA 6: KAYNAK / PROJE KONTROLU ===' -ForegroundColor Cyan
foreach ($file in @($kanun,$hpdf,$tariff,$project,$regression)) {
  if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Eksik dosya: $file" }
  Write-Host "[OK] $(Split-Path -Leaf $file)" -ForegroundColor Green
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK 10.x bulunamadi.' }
Write-Host '=== ASAMA 6: SEYAHAT ZAMAN CIZELGESI C# TESTLERI ===' -ForegroundColor Cyan
& dotnet run --project $project --configuration Release -- $tariff
if ($LASTEXITCODE -ne 0) { throw 'ASAMA 6 testleri basarisiz.' }
Write-Host '=== REGRESYON: ASAMA 5 + 4 + 2/3 ===' -ForegroundColor Cyan
& $regression
if ($LASTEXITCODE -ne 0) { throw 'Onceki asama regresyon testleri basarisiz.' }
Write-Host '[OK] ASAMA 6 ve tum onceki test kontrolleri tamamlandi.' -ForegroundColor Green
Write-Warning 'Bu paket bir seyahat zaman cizelgesi / ON HESAPTIR. Resmi bordro ve odeme emri olusturmaz.'
