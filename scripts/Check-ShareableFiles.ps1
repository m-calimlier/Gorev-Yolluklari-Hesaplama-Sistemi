$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$files = @(& git -C $root -c core.quotepath=false ls-files --cached)
if ($LASTEXITCODE -ne 0 -or $files.Count -eq 0) { throw 'Denetlenecek Git index dosyasi yok.' }
$issues = @()
foreach ($file in $files) {
    if ($file -match '^(Veriler|Yedekler|Ciktilar)/' -and $file -notmatch '/\.gitkeep$') { $issues += $file + ': kullanici verisi'; continue }
    if ($file -match '(^|/)(bin|obj|dist)/|\.bak|\.pfx$|\.key$|(^|/)\.env($|\.)|^Kaynaklar/.*\.(pdf|xlsx|html|url)$') { $issues += $file + ': paylasim disi dosya'; continue }
    $path = Join-Path $root $file
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $matches = Select-String -LiteralPath $path -Pattern '(ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,}|sk-(?:proj-)?[A-Za-z0-9_-]{30,}|-----BEGIN [A-Z ]*PRIVATE KEY-----|\bTR[0-9]{24}\b|\b[1-9][0-9]{10}\b)'
    if ($matches) { $issues += $file + ': olasi sir/kisisel kimlik; icerik rapora yazilmadi' }
}
if ($issues.Count -gt 0) { $issues | ForEach-Object { Write-Warning $_ }; throw 'Paylasim denetimi basarisiz.' }
Write-Host ("[OK] {0} Git dosyasi paylasim disi veri ve yaygin sir/kimlik oruntuleri acisindan kontrol edildi." -f $files.Count) -ForegroundColor Green
