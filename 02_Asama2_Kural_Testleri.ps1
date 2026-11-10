param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$law = Join-Path $root 'Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu_2026_Kontrol_Kopyasi.html'
$rule = Join-Path $root 'Kaynaklar\99_Katalog_ve_Analiz\ASAMA2_KURAL_KATALOGU.json'
$proj = Join-Path $root 'tests\GorevYolluklari.RuleChecks\GorevYolluklari.RuleChecks.csproj'
Write-Host '=== ASAMA 2: KAYNAK ve KURAL KONTROLU ===' -ForegroundColor Cyan
if (-not (Test-Path -LiteralPath $law)) {
    Write-Warning '6245 Kanun HTML kontrol kopyasi bulunamadi. Kaynak teyidi eksik.'
} else {
    $content = Get-Content -LiteralPath $law -Raw -Encoding UTF8
    if ($content -notmatch '6245' -or $content -notmatch '(?i)Madde[\s\S]{0,70}39') {
        Write-Warning 'Kanun kontrol kopyasi metin incelemesi gerektiriyor.'
    } else { Write-Host '[OK] Yardimci kanun kopyasi mevcut (resmi metin yerine gecmez).' -ForegroundColor Green }
}
if (-not (Test-Path -LiteralPath $rule)) { throw 'Kural katalogu bulunamadi.' }
$null = Get-Content -LiteralPath $rule -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Host '[OK] Kural katalogu JSON gecerli.' -ForegroundColor Green
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Warning 'dotnet SDK bulunamadi. Bu ortamda derleme ve test yapilamadi.'
    Write-Host 'Kaynak dosyalar yerli yerinde. Gelistirme icin .NET 10 SDK gerekli.'
    exit 2
}
& dotnet --version
Write-Host '=== C# KURAL KONTROLLERI ===' -ForegroundColor Cyan
& dotnet run --project $proj --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Testler basarisiz. Cikis kodu: $LASTEXITCODE" }
Write-Host '[OK] Asama 2 matematiksel on hesap testleri gecti.' -ForegroundColor Green
Write-Host 'UYARI: Resmi bordro veya hukuki onay uretilmedi.' -ForegroundColor Yellow
