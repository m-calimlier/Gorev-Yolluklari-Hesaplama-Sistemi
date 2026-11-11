param([switch]$SkipUi)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
$env:UseSharedCompilation = 'false'
$dotnetExecutable = (Get-Command dotnet -CommandType Application).Source
& $dotnetExecutable build (Join-Path $root 'GorevYolluklari.slnx') --configuration Release --disable-build-servers -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { throw 'WPF Release derlemesi basarisiz.' }
# Tüm projeler yukarıda derlendi. Eski betiklerdeki testleri aynen çalıştırırken
# her dotnet run için yeniden NuGet/derleme sunucusuna bağlanmayı önle.
# Seçenekler programın -- argümanlarından ÖNCE eklenir; test girdileri korunur.
function dotnet {
    $runArguments = @($args)
    if ($runArguments[0] -eq 'run') {
        $runArguments = @('run', '--no-build', '--no-restore') + $runArguments[1..($runArguments.Count - 1)]
    }
    & $dotnetExecutable @runArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet islemi basarisiz: $LASTEXITCODE" }
}
& (Join-Path $root '07_Asama7_Belge_Taslaklari_Testleri.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Asama 2-7 regresyonu basarisiz.' }
foreach ($check in @('WorkflowChecks','PdfSafetyChecks','ModuleChecks')) {
    & dotnet run --project (Join-Path $root ('tests/GorevYolluklari.' + $check)) --configuration Release -- $root
    if ($LASTEXITCODE -ne 0) { throw "Asama 8 $check basarisiz." }
}
if (-not $SkipUi) {
    $uiOutput = Join-Path $root ('Ciktilar/ASAMA8_UI_' + [Guid]::NewGuid().ToString('N'))
    & dotnet run --project (Join-Path $root 'tests/GorevYolluklari.UiChecks') --configuration Release -- $root $uiOutput
    if ($LASTEXITCODE -ne 0) { throw 'Asama 8 WPF kontrolleri basarisiz.' }
}
Write-Host '[OK] Asama 8 ve onceki regresyonlar basarili.' -ForegroundColor Green
Write-Warning 'Yalniz on hesap ve kontrol taslagi. Resmi belge/mevzuat ve temiz ikinci bilgisayar kabulu ayri kapilardir.'
