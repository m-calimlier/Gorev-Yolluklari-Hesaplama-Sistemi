$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$kanun = Join-Path $root 'Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu.pdf'
$hpdf = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli.pdf'
$tariff = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli_Dogrulanmis_OnHesap.json'
$proj = Join-Path $root 'tests\GorevYolluklari.IntegrationChecks\GorevYolluklari.IntegrationChecks.csproj'
Write-Host '=== ASAMA 4: KAYNAK VE KOD DOSYALARI ===' -ForegroundColor Cyan
foreach ($dosya in @($kanun, $hpdf, $tariff, $proj)) {
  if (-not (Test-Path -LiteralPath $dosya -PathType Leaf)) { throw "Eksik dosya: $dosya" }
  Write-Host "[OK] $(Split-Path -Leaf $dosya)" -ForegroundColor Green
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK gerekli.' }
Write-Host '=== ASAMA 4: BIRLESIK C# TESTLERI ===' -ForegroundColor Cyan
& dotnet run --project $proj --configuration Release -- $tariff
if ($LASTEXITCODE -ne 0) { throw "ASAMA 4 testleri basarisiz. Kod: $LASTEXITCODE" }
Write-Host '=== REGRESYON: ASAMA 2 ===' -ForegroundColor Cyan
& dotnet run --project (Join-Path $root 'tests\GorevYolluklari.RuleChecks\GorevYolluklari.RuleChecks.csproj') --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'ASAMA 2 regresyon testi BASARISIZ.' }
Write-Host '=== REGRESYON: ASAMA 3 ===' -ForegroundColor Cyan
& dotnet run --project (Join-Path $root 'tests\GorevYolluklari.TariffChecks\GorevYolluklari.TariffChecks.csproj') --configuration Release -- $tariff
if ($LASTEXITCODE -ne 0) { throw 'ASAMA 3 regresyon testi BASARISIZ.' }
Write-Host '[OK] ASAMA 4 ve önceki ASAMA 2/3 testleri tamamlandi.' -ForegroundColor Green
Write-Warning 'Bu yazilim halen ON HESAPTIR. Resmi yolluk bildirimi/bordro uretmez.'
