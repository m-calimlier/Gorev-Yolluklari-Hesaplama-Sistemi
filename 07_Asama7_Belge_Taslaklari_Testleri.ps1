$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$law = Join-Path $root 'Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu.pdf'
$forms = Join-Path $root 'Kaynaklar\03_Belgeler_ve_Sablonlar'
$prev = Join-Path $root '06_Asama6_Seyahat_Takvimi_Testleri.ps1'
$coreProject = Join-Path $root 'tests\GorevYolluklari.DocumentChecks\GorevYolluklari.DocumentChecks.csproj'
$pdfProject = Join-Path $root 'tests\GorevYolluklari.PdfChecks\GorevYolluklari.PdfChecks.csproj'
Write-Host '=== ASAMA 7: RESMI KAYNAK VE DOSYA KONTROLU ===' -ForegroundColor Cyan
foreach($f in @($law,$prev,$coreProject,$pdfProject)) {
 if(-not (Test-Path -LiteralPath $f -PathType Leaf)) { throw "Eksik: $f" }
 Write-Host "[OK] $(Split-Path -Leaf $f)" -ForegroundColor Green
}
if(-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK gelistirme bilgisayarinda bulunamadi.' }
# Resmi yonetmelik kullanicinin kaynaklari icinde bulunuyor mu?
$official = @(Get-ChildItem -LiteralPath $forms -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.pdf' })
if($official.Count -eq 0) { Write-Warning 'Resmî Örnek 27/28 karşılaştırması için yönetmelik PDF dosyası bulunamadı.' }
else { Write-Host "[OK] Sablon klasorunde PDF dosyasi sayisi: $($official.Count)" -ForegroundColor Green }
Write-Host '=== ASAMA 7: BELGE MODELI / KURUS / MAHSUP TESTLERI ===' -ForegroundColor Cyan
& dotnet run --project $coreProject --configuration Release
if($LASTEXITCODE -ne 0) { throw 'Asama 7 belge modeli testleri BASARISIZ.' }
Write-Host '=== ASAMA 7: GERCEK PDF OLUSTURMA TESTLERI ===' -ForegroundColor Cyan
& dotnet run --project $pdfProject --configuration Release -- (Join-Path $root 'Ciktilar\ASAMA7_DEMO')
if($LASTEXITCODE -ne 0) { throw 'Asama 7 PDF olusturma testleri BASARISIZ.' }
Write-Host '=== REGRESYON: ASAMA 6 ve ONCESI ===' -ForegroundColor Cyan
& $prev
if($LASTEXITCODE -ne 0) { throw 'Onceki asama testleri BASARISIZ.' }
Write-Host '[OK] Asama 7 taslak model, PDF ve onceki regresyon kontrolleri BASARILI.' -ForegroundColor Green
Write-Warning 'PDFler KONTROL TASLAGIDIR. Resmi Ornek 27/28 ile birebir alan/mizanpaj ve gercek bordro mutabakati BEKLIYOR.'
