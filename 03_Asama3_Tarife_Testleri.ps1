$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$law = Join-Path $root 'Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu.pdf'
$hpdf = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli.pdf'
$tariff = Join-Path $root 'Kaynaklar\02_Butce_ve_Tarifeler\2026_H_Cetveli_Dogrulanmis_OnHesap.json'
$devir = Join-Path $root 'docs\PROJE_DEVIR_NOTU_2026-11-09.md'
$project = Join-Path $root 'tests\GorevYolluklari.TariffChecks\GorevYolluklari.TariffChecks.csproj'
$reportDir = Join-Path $root 'Kaynaklar\99_Katalog_ve_Analiz'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
Write-Host '=== ASAMA 3: DOSYA / TARIFE DOGRULAMA ===' -ForegroundColor Cyan
if (Test-Path -LiteralPath $devir) { Write-Host '[OK] Devir notu bulundu.' -ForegroundColor Green }
else { Write-Warning 'Devir notu bulunamadi. Onceki ZIP acilis yolunu kontrol edin.' }
$rows = @()
foreach ($item in @(@('KAN-001', $law), @('BUT-002',$hpdf))) {
  $id = $item[0]; $path = $item[1]
  if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
    throw "$id eksik: $path. Dosyayi belirtilen klasore kaydedin."
  }
  $stream = [System.IO.File]::OpenRead($path)
  try {
    if ($stream.Length -lt 1024) { throw "$id boyutu supheli: $($stream.Length) bayt" }
    $buffer = New-Object byte[] 5
    [void]$stream.Read($buffer,0,5)
    if ([Text.Encoding]::ASCII.GetString($buffer) -ne '%PDF-') { throw "$id PDF degil (olasi HTML engel sayfasi)." }
  } finally { $stream.Dispose() }
  $sha = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
  $size = (Get-Item -LiteralPath $path).Length
  $rows += [pscustomobject]@{ KaynakID=$id; Dosya=$path; Bayt=$size; SHA256=$sha; KontrolTarihi=(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'); Sonuc='PDF-imza-ve-boyut-dogrulandi-icerik-incelenmedi' }
  Write-Host "[OK] $id PDF imzasi/boyut: $size bayt; SHA-256: $sha" -ForegroundColor Green
}
$report = Join-Path $reportDir ('ASAMA3_Dosya_Dogrulama_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.csv')
$rows | Export-Csv -LiteralPath $report -Delimiter ';' -NoTypeInformation -Encoding UTF8
Write-Host "[OK] Kaynak kontrol raporu: $report" -ForegroundColor Green
Write-Warning 'PDF imzasi ve SHA-256 belge iceriginin hukuki guncelligini veya tamligini kanitlamaz.'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET SDK bulunamadi.' }
if (-not (Test-Path -LiteralPath $tariff)) { throw "H cetveli tarife JSON bulunamadi: $tariff" }
Write-Host '=== ASAMA 3: C# GUNDELIK TARIFE TESTLERI ===' -ForegroundColor Cyan
& dotnet run --project $project --configuration Release -- $tariff
if ($LASTEXITCODE -ne 0) { throw "Tarife testleri basarisiz (kod $LASTEXITCODE)" }
Write-Host '[OK] Tarife testleri tamamlandi. Bu bir on hesap modulu, resmi bordro degil.' -ForegroundColor Green
