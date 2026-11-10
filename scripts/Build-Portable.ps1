param([string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$proje = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$csproj = Join-Path $proje 'src\GorevYolluklari.App\GorevYolluklari.App.csproj'
$surum = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$hedef = Join-Path $proje ('dist\GorevYolluklari_' + $surum)
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 10 SDK gelistirme bilgisayarinda kurulu olmali.' }
if (Test-Path -LiteralPath $hedef) { throw 'Yayin hedefi mevcut; uzerine yazilmayacak.' }
dotnet publish $csproj --configuration Release --runtime $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false --output $hedef
if ($LASTEXITCODE -ne 0) { throw 'Publish basarisiz.' }
$kaynakHedef = Join-Path $hedef 'Kaynaklar'
New-Item -Path $kaynakHedef -ItemType Directory -Force | Out-Null
Copy-Item -Path (Join-Path $proje 'Kaynaklar/*') -Destination $kaynakHedef -Recurse -Force
Copy-Item -LiteralPath (Join-Path $proje 'docs/ASAMA8_KULLANIM.md') -Destination (Join-Path $hedef 'KULLANIM.md')
foreach ($ad in @('Veriler','Yedekler','Ciktilar')) { New-Item -Path (Join-Path $hedef $ad) -ItemType Directory -Force | Out-Null }
Write-Host ('Tasınabilir test derlemesi: ' + $hedef) -ForegroundColor Green
Write-Warning 'On hesap ve KONTROL TASLAGI surumu. Resmi bordro degildir; temiz ikinci Windows bilgisayarinda kabul testi gereklidir.'
