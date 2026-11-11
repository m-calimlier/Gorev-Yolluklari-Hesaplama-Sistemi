param([Parameter(Mandatory)][string]$Desktop,[switch]$Clean)
$FolderName='Görev Yollukları Hesaplama Sistemi'
$ManagedFolders=@('Veriler','Yedekler','Ciktilar','Calisma')
$LegacyRoots=@()

$ErrorActionPreference='Stop'
function Assert-Leaf([string]$Name) {
 if([string]::IsNullOrWhiteSpace($Name) -or $Name -in @('.','..') -or $Name.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0){throw 'Geçersiz klasör adı.'}
}
function Assert-NoLinks([string]$Path) {
 $current=[IO.Path]::GetFullPath($Path)
 while($current){
  $item=Get-Item -LiteralPath $current -Force -ErrorAction SilentlyContinue
  if($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Bağlantılı klasör/dosya ile işlem yapılmaz: $Path"}
  $current=[IO.Path]::GetDirectoryName($current)
 }
}
Assert-Leaf $FolderName
foreach($name in $ManagedFolders){Assert-Leaf $name}
if([string]::IsNullOrWhiteSpace($Desktop) -or ![IO.Path]::IsPathRooted($Desktop)){throw 'Masaüstü bulunamadı.'}
$desktopPath=[IO.Path]::GetFullPath($Desktop).TrimEnd('\','/')
$root=[IO.Path]::GetFullPath((Join-Path $desktopPath $FolderName))
if([IO.Path]::GetDirectoryName($root) -ne $desktopPath){throw 'Hedef masaüstü dışında.'}
Assert-NoLinks $root
New-Item -ItemType Directory -Force -Path $root | Out-Null
$lockPath=Join-Path $root '.kurulum.lock'
$lock=[IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
 # Inspect all descendants before any recursive mutation; never follow junctions.
 $candidates=@()
 foreach($name in $ManagedFolders){$p=Join-Path $root $name;if(Test-Path -LiteralPath $p){$candidates+=$p}}
 foreach($old in $LegacyRoots){
  if(!$old){continue};$old=[IO.Path]::GetFullPath($old).TrimEnd('\','/')
  if($old -eq $root){continue}
  if(Test-Path -LiteralPath (Join-Path $old '.git')){throw 'Eski veri konumu bir kaynak kod deposu; otomatik işlem durduruldu.'}
  if($root.StartsWith($old+'\',[StringComparison]::OrdinalIgnoreCase) -or $old.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Eski ve yeni veri kökleri çakışıyor.'}
  if(Test-Path -LiteralPath $old){$candidates+=$old}
 }
 foreach($p in $candidates){
  Assert-NoLinks $p
  $queue=[Collections.Generic.Queue[string]]::new();$queue.Enqueue($p)
  while($queue.Count){foreach($child in Get-ChildItem -LiteralPath $queue.Dequeue() -Force){Assert-NoLinks $child.FullName;if($child.PSIsContainer){$queue.Enqueue($child.FullName)}}}
 }
 if($Clean){
  foreach($name in $ManagedFolders){
   $target=[IO.Path]::GetFullPath((Join-Path $root $name))
   if([IO.Path]::GetDirectoryName($target) -ne $root){throw 'Temizleme hedefi doğrulanamadı.'}
   if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target -Recurse -Force}
  }
  foreach($old in $LegacyRoots){
   if(!$old -or !(Test-Path -LiteralPath $old)){continue}
   $old=[IO.Path]::GetFullPath($old).TrimEnd('\','/');if($old -eq $root){continue}
   foreach($file in Get-ChildItem -LiteralPath $old -File -Recurse -Force){
    $target=[IO.Path]::GetFullPath($file.FullName)
    if(!$target.StartsWith($old+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Eski veri temizleme hedefi doğrulanamadı.'}
    Assert-NoLinks $target
    Remove-Item -LiteralPath $target
   }
  }
 }
 $moves=@()
 foreach($old in $LegacyRoots){
  if(!$old -or !(Test-Path -LiteralPath $old)){continue}
  $old=[IO.Path]::GetFullPath($old).TrimEnd('\','/');if($old -eq $root){continue}
  $key=([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($old)))).Replace('-','').Substring(0,16)
  foreach($file in Get-ChildItem -LiteralPath $old -File -Recurse -Force){
   $relative=$file.FullName.Substring($old.Length).TrimStart('\','/')
   $first=($relative -split '[\\/]')[0]
   $target=if(!$Clean -and $first -in $ManagedFolders){Join-Path $root $relative}else{Join-Path $root ('Yedekler/EskiKonumlar/'+$key+'/'+$relative)}
   Assert-NoLinks $target
   $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
   if((Test-Path -LiteralPath $target) -and (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash){
    $target=Join-Path $root ('Yedekler/EskiKonumlar/'+$key+'/'+$relative+'.'+$hash)
   }
   New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
   if(!(Test-Path -LiteralPath $target)){Copy-Item -LiteralPath $file.FullName -Destination $target}
   if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash){throw 'Kopya doğrulanamadı; eski veriler korundu.'}
   $moves+=@{Source=$file.FullName;Target=$target;Hash=$hash}
  }
 }
 # Verify the entire batch and recheck each source before removing only verified files.
 foreach($m in $moves){if((Get-FileHash -LiteralPath $m.Source -Algorithm SHA256).Hash -ne $m.Hash -or (Get-FileHash -LiteralPath $m.Target -Algorithm SHA256).Hash -ne $m.Hash){throw 'Taşıma sırasında veri değişti; kaynak korundu.'}}
 foreach($m in $moves){if((Get-FileHash -LiteralPath $m.Source -Algorithm SHA256).Hash -ne $m.Hash){throw 'Kaynak değişti.'};Remove-Item -LiteralPath $m.Source}
 foreach($name in $ManagedFolders){New-Item -ItemType Directory -Force -Path (Join-Path $root $name) | Out-Null}
} finally {$lock.Dispose();Remove-Item -LiteralPath $lockPath -ErrorAction SilentlyContinue}
