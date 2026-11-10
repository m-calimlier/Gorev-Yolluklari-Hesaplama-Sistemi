param([switch]$YenidenIndir)

# Resmi kaynak dosyalari indirici; yolluk hesaplamasi yapmaz.
$ErrorActionPreference = 'Stop'
$proje = $PSScriptRoot
$kaynaklar = Join-Path $proje 'Kaynaklar'
$katalog = Join-Path $kaynaklar '99_Katalog_ve_Analiz\Kaynak_Katalogu.json'
if (-not (Test-Path -LiteralPath $katalog)) { throw 'Kaynak katalogu bulunamadi: ' + $katalog }
$kayitlar = Get-Content -LiteralPath $katalog -Raw -Encoding UTF8 | ConvertFrom-Json
$rapor = New-Object System.Collections.Generic.List[object]
try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch {}
foreach ($s in $kayitlar) {
    if ($s.tur -eq 'baglanti') { continue }
    $hedef = Join-Path $kaynaklar $s.hedefDosya
    $dizin = Split-Path -Parent $hedef
    New-Item -ItemType Directory -Path $dizin -Force | Out-Null
    if ((Test-Path -LiteralPath $hedef) -and -not $YenidenIndir) {
        $ozet = (Get-FileHash -LiteralPath $hedef -Algorithm SHA256).Hash
        Write-Host ('[MEVCUT] ' + $s.id + ' : ' + $hedef) -ForegroundColor Cyan
        $rapor.Add([pscustomobject]@{Id=$s.id;Durum='MEVCUT';Dosya=$hedef;SHA256=$ozet;Aciklama='Yeniden indirilmedi'})
        continue
    }
    $gecici = $hedef + '.indirme'
    if (Test-Path -LiteralPath $gecici) { Remove-Item -LiteralPath $gecici -Force }
    Write-Host ('[INDIR] ' + $s.id + ' : ' + $s.url) -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $s.url -OutFile $gecici -UseBasicParsing -TimeoutSec 75 -MaximumRedirection 5 -ErrorAction Stop
        $boyut = (Get-Item -LiteralPath $gecici).Length
        if ($boyut -lt 100) { throw 'Dosya 100 bayttan kucuk; belge gibi gorunmuyor.' }
        $stream = [System.IO.File]::OpenRead($gecici)
        try {
            $ilk = New-Object byte[] 8
            $null = $stream.Read($ilk, 0, 8)
        } finally { $stream.Dispose() }
        if ($s.tur -eq 'pdf') {
            $imza = [System.Text.Encoding]::ASCII.GetString($ilk, 0, 5)
            if ($imza -ne '%PDF-') { throw 'PDF imzasi bulunamadi; olasi kurum engel sayfasi.' }
        }
        if ($s.tur -eq 'xlsx') {
            if ($ilk[0] -ne 80 -or $ilk[1] -ne 75) { throw 'XLSX ZIP imzasi bulunamadi; olasi kurum engel sayfasi.' }
        }
        Move-Item -LiteralPath $gecici -Destination $hedef -Force
        $sha = (Get-FileHash -LiteralPath $hedef -Algorithm SHA256).Hash
        Write-Host ('[OK] ' + $s.id + ' - ' + $boyut + ' bayt') -ForegroundColor Green
        $rapor.Add([pscustomobject]@{Id=$s.id;Durum='OK';Dosya=$hedef;SHA256=$sha;Aciklama=([string]$boyut + ' bayt')})
    } catch {
        if (Test-Path -LiteralPath $gecici) { Remove-Item -LiteralPath $gecici -Force }
        $hata = $_.Exception.Message
        Write-Warning ('[HATA] ' + $s.id + ' : ' + $hata)
        $rapor.Add([pscustomobject]@{Id=$s.id;Durum='HATA';Dosya=$hedef;SHA256='';Aciklama=$hata})
    }
}
$raporDosyasi = Join-Path $kaynaklar ('99_Katalog_ve_Analiz\Indirme_Raporu_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.csv')
$rapor | Export-Csv -Path $raporDosyasi -NoTypeInformation -Delimiter ';' -Encoding UTF8
$ok = @($rapor | Where-Object {$_.Durum -eq 'OK'}).Count
$hata = @($rapor | Where-Object {$_.Durum -eq 'HATA'}).Count
Write-Host ("Tamamlandi. Yeni indirme: $ok; hata: $hata. Rapor: $raporDosyasi") -ForegroundColor Cyan
