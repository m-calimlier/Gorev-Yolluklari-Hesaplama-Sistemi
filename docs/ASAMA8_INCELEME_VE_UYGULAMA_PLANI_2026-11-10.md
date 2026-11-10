# Aşama 8 — Mevcut durum ve uygulanabilir geliştirme planı

Tarih: 10.11.2026. Bu belge bir uygulama önerisidir; aşağıdaki teslimler uygulanmış sayılmaz. Kullanıcı ilk incelemeden sonra kod düzeltmesine ve yol haritasının geliştirilmesine izin verdi. Bu turdaki ürün kodu değişikliği yalnız WPF derlemesindeki eksik `System.IO` içe aktarımıdır.

## Hedef ve mimari

Mevcut .NET 10 WPF uygulamasını, 2026 yurt içi geçici görev ve sürekli görev ön hesaplarını güvenli kayıt ve kontrol taslağı akışında kullanan taşınabilir uygulamaya dönüştürmek.

Önerilen yaklaşım: yerel WPF kaynak sözlükleri, hafif MVVM ve uygulama servisleri. `App → Application → Core / Storage / Documents`. Core hesaplamaları görünüm modellerine taşınmaz. Ön hesap, kanıt incelemesi ve belge hazırlama ayrı durumlar olarak tutulur.

Alternatif: bütün ekranları mevcut code-behind içinde büyütmek başlangıçta daha hızlıdır, ancak toplu giriş ve kayıt doğrulamasını sınamayı zorlaştırır. Büyük bir UI paketi eklemek mevcut kapsam için gerekli değildir. Önerim mevcut projeleri koruyarak küçük teslimlerle ilerlemektir.

Dayanak: `AGENTS.md`, `CODEX_DEVIR_2026-11-10.md`, Aşama 2–7 belgeleri ve gerçek kaynaklar. Eski belgelerdeki gelecek iş listeleri yeni kullanıcı talimatı veya tamamlanmış iş kanıtı değildir.

## İlk incelemenin doğrulanmış bulguları

| Aşama | Gerçek dosya durumu | İlk çalıştırma |
|---|---|---|
| 1 | WPF iskeleti, JSON yardımcıları, katalog ve yerel kaynaklar mevcut | İndirici incelendi; kaynakları değiştirmemek için çalıştırılmadı |
| 2 | m.39 / m.45 matematiksel ön hesapları | 11/11 |
| 3 | 2026 I-B JSON tarife yükleyici ve eşik kontrolleri | 23/23 |
| 4 | Tarife ile günübirlik / yer değiştirme entegrasyonu | 42/42 |
| 5 | İncelenmiş gün listesi üzerinden çok günlü / konaklama ön hesabı | 48/48 |
| 6 | Seyahat takvimi ve m.43 aday birimleri | 55/55 |
| 7 | Belge modeli, kuruş / avans / icmal ve PDF üretimi | 70/70 + 7/7 |

Toplam 256/256 benzersiz kontrol başarılı. Aşama 2 ve 3 betikleri kaynak ön kontrolleri için ayrıca çalıştırıldı; yinelenen testler toplamı artırmaz. SDK: 10.0.402.

PDF testi `Program.cs:18` içindeki `decision.Document ?? throw ...` düzeltmesi zaten mevcut. Her iki PDF dosyasında gerekli `using System.IO;` satırı mevcut. Test çalıştırmalarında CS8603 görülmedi.

İlk WPF Release derlemesi `MainWindow.xaml.cs:12–13` üzerinde dört CS0103 hatası verdi. Üretilmiş WPF global using dosyasında System.IO yok; App dosyasına açık içe aktarım eklendi. Hesaplama ve belge kodları değiştirilmedi. Değişiklik öncesi dosya Windows geçici klasöründeki `GorevYolluklari_Inceleme_dc37215f364c42fe8c84ca92524e5420` dizinine yedeklendi.

6245 ve H Cetveli SHA-256 değerleri devir notundaki değerlerle aynı. Bu, hukuki güncellik teyidi değildir; bu turda mevzuatın yürürlük karşılaştırması yapılmadı.

## Eksikler ve öncelikler

1. **Uygulama akışı yok:** MainWindow başlangıç kartları ve klasör açma düğmelerinden ibaret. App henüz Documents projesini referanslamıyor; hesaplama ve PDF ekranlara bağlı değil. README ve ProjectInfo eski başlangıç durumunu anlatıyor.
2. **Hesap kapsamı sınırlı:** m.45 sonucu tam sürekli görev toplamı değil. Çok günlü takvim adayları onaylı hak günleri değil. Özel statüler ve diğer yıl tarifeleri desteklenmiyor. Bunlar arayüz geliştirilerek tamamlanmış sayılamaz.
3. **Motor-belge bağlantısı tasarlanmalı:** Tarife motoru kategori içeren `BUT-002:2026:I-B_e` gibi referans döndürüyor; belge servisi tam olarak `BUT-002:2026:I-B` istiyor. Kategori ve genel tarife referansı ayrı tutulmalı. Ham kesirli tutarları iki basamağa biçimlendirmek, bağımsız kuruş mutabakatı yerine geçmez.
4. **İnceleme kanıtı zayıf:** Boolean işaretleri gerçek kanıtın varlığı veya hukuki yetki doğrulaması değildir. Kanıt referansı, inceleme tarihi, inceleyen beyanı ve gerekçe kayıtla birlikte saklanmalı. Veri değişince önceki inceleme geçerliliği kaldırılmalı.
5. **JSON altyapısı başlangıç düzeyinde:** Atomik geçici yazım, diske flush ve `.bak` var. Personel/görev modelleri, şema sürümü, kayıt revizyonu, eşzamanlı yazma koruması, tarihsel yedek ve geri yükleme hizmeti yok. `Yedekler/` klasörünü oluşturmak yedekleme sistemi kurmak değildir.
6. **Taşınabilir yol ayrımı:** `AppContext.BaseDirectory` kullanılıyor. Geliştirmede kaynak kökü yerine `bin/Release/.../Kaynaklar` aranır ve boş klasör oluşturulabilir. Uygulama veri kökünü açık göstermeli; kaynak bulunamazsa boş tarife veya sıfır kayıt varsaymamalı. Yayın betiği kaynakları paket köküne kopyalıyor; temiz bilgisayarda çalışma henüz doğrulanmadı.
7. **PDF kabulü eksik:** 7 test imza, asgari boyut, sayfa sayısı, yatay yön ve bazı ret koşullarını kontrol ediyor. Gerçek A4 ölçüsü, metin doğruluğu ve görsel çakışmayı kanıtlamıyor. Kodda kurum satırı y=80, taslak uyarı bandı y=71–96 aralığında; yerleşim çakışması riski görsel olarak doğrulanmalı. İcmal yazıcısı toplamların belgelerin toplamına eşitliğini yeniden doğrulamıyor; JSON'dan yüklenen kayıt doğrudan yazıcıya verilmemeli.
8. **Kaynak kataloğu:** BUT-003 hâlâ 8. Dönem Toplu Sözleşme adıyla kayıtlı; yerel alternatif Hakem Kurulu Kararı ayrı dosya. İçerik karşılaştırması yapılmadan katalog sessizce düzeltilmemeli.
9. **Resmî kabul kapısı açık:** Derece/ek gösterge, saat ve taşıt kolonları, aile kişi detayları, gün sayısı, yuvarlama, gerçek anonim örnekler ve güncel form eşlemesi tamamlanmadı. Avansın brütü aştığı durum halen bloke.

## Sabit sınırlar

- C# / .NET 10 / WPF; SQL, SQLite, servis ve Office Interop yok.
- Kalıcılık System.Text.Json; kayıt ve kaynaklar sessizce sıfırlanmaz veya eski ZIP'lerle değiştirilmez.
- `win-x64`, self-contained hedefi; son kullanıcıya Runtime veya Office kurulmaz.
- Hesaplamanın tek kaynağı mevcut C# motorları; tablo ve Excel yalnız giriş/aktarım/kontrol içindir.
- Bilinmeyen mahalliyet ve özel statü otomatik uygun kabul edilmez; 2026 dışı yıl bloke.
- m.43 adayları m.42/33(d) hak listesine otomatik çevrilmez.
- Tüm çıktılar görünür biçimde **KONTROL TASLAĞI — ÖDEME BELGESİ DEĞİLDİR** taşır. Resmî imza/ödeme yetkisi açılmaz.
- Branch, commit, push ve release kullanıcı açıkça istemedikçe yok.

## Örnek projelerden seçilecek desenler

Bağlı GitHub üzerinden iki projenin güncel README dosyaları okundu:

- [Ek Ders Hakediş Sistemi](https://github.com/mcalimlier/Ek-Ders-Hakedis-Sistemi): aynı kaydın form ve hibrit tablo görünümü, dondurulmuş hesap sonucu, bozuk kaydı sessizce boş göstermeme, tarih/klavye okunabilirliği ve temiz dağıtım yaklaşımı.
- [DoğruFatura](https://github.com/mcalimlier/DogruFatura): lacivert-altın kartlar, 1024×768 / 1366×768 / 1920×1080 ekran profilleri, işlem girişini salt okunur listeden ayırma ve geri yükleme öncesi koruyucu yedek.

Bunlar README üzerinden incelenen desenlerdir; bu turda kaynak kod aktarılmadı. Her iki kökte `LICENSE` isteği 404 döndü; bu sonuç başka adla lisans bulunmadığını göstermez. Kod/asset kopyalamadan önce gerçek lisans dosyası ve bileşen izinleri doğrulanmalı. Sabit Masaüstü veri yolu örüntüsü bu projenin taşınabilir kök tercihine aynen aktarılmamalı.

## Teslim sırası ve kabul ölçütleri

### 8.0 — Başlangıç doğrulaması ve derleme

- [x] Devir belgeleri, kaynaklar ve betik zincirini incele; 256 kontrolü çalıştır.
- [x] İlk başarısız WPF derlemesini kaydet; kaynak dosyayı yedekle ve yalnız System.IO içe aktarımını düzelt.
- [x] Düzeltme sonrası Release build: 0 hata, 0 uyarı, çıkış kodu 0. Tam 07 regresyon zinciri yeniden çalıştırıldı: 256/256, çıkış kodu 0. CS8603 görülmedi. Uyarılar ön hesap/taslak ve bekleyen resmî mutabakatla ilgili beklenen betik mesajlarıdır.

### 8.1 — Güvenli kayıt ve uygulama sözleşmeleri

Önerilen dosyalar: `src/GorevYolluklari.Core/Records/PersonnelRecord.cs`, `DutyRecord.cs`, `CalculationSnapshot.cs`; `src/GorevYolluklari.Storage/Records/JsonRecordRepository.cs`, `BackupService.cs`; `tests/GorevYolluklari.StorageChecks/`.

- [ ] Önce bozuk/null JSON, desteklenmeyen şema, mükerrer kimlik, revizyon çakışması ve başarısız yazma senaryolarını test et.
- [ ] Her kayıt için `SchemaVersion`, değişmez `Id`, `Revision`, oluşturma/değiştirme zamanı; görev için personel kimliği ve görev anındaki personel kopyasını sakla.
- [ ] `CalculationSnapshot` içinde girdi, motor/tarife sürümü, kategori, genel kaynak referansı, ham decimal sonuç, kanıt referansları ve inceleme durumunu sakla. Eski sonuç yeni tarifeyle kendiliğinden değişmesin.
- [ ] `JsonRecordRepository.Load<T>(Guid id)` ve `Save<T>(Guid id, T record, int expectedRevision)` sözleşmesiyle eski revizyonun yeni veriyi ezmesini engelle; tek yazıcı kilidi kullan. Dosya yollarını kullanıcı metninden üretme.
- [ ] Tarihli/benzersiz yedeği `Yedekler/` altında tut. `BackupService.Restore(string backupPath)` önce dosyayı ve şemayı doğrulasın, mevcut durumu ayrıca yedeklesin; hata halinde mevcut dosya korunsun.
- [ ] Kayıt yok, kaynak yok ve bozuk kayıt durumlarını ayrı göster; kullanıcıya veri kökünü göster.

Kabul: kaydet/kapat/aç aynı kaydı verir; bozulan kayıt boş listeye dönüşmez; ikinci yazıcı eski revizyonla kaydedemez; geri yükleme öncesi mevcut sürüm kurtarılabilir.

### 8.2 — WPF kabuğu ve görsel sistem

Önerilen dosyalar: `src/GorevYolluklari.App/Themes/Colors.xaml`, `Controls.xaml`, `ViewModels/MainViewModel.cs`, `Views/` ve mevcut `MainWindow.xaml`.

- [ ] Yerel kaynak sözlüklerine mevcut lacivert `#0B1730`, kart `#152643`, altın `#DBB65A` ve okunabilir metin renklerini taşı.
- [ ] Sol gezinme: Ana Sayfa, Geçici Görev, Sürekli Görev, Personel ve Kayıtlar, Belgeler/İcmaller, Kaynak/Tarifeler, Yedek/Ayarlar.
- [ ] Üstte aktif veri kökü ve tarife yılı; sonuçlarda Ön Hesap / Eksik Bilgi / İnceleme Gerekli / Taslak Hazır durumlarını metinle göster.
- [ ] Tab sırası, görünür odak, kısayollar, etiketler ve ekran okuyucu adları kur. Küçük ekranda menüyü daralt; uzun formu kaydır; kritik düğmeleri erişilebilir tut.
- [ ] 1024×768, 1366×768, 1920×1080 ve %125/%150 DPI'da kullanılabilir ekran alanına sığmayı kontrol et.

Kabul: tüm ekranlara klavyeyle erişilir; tarih alanları ve seçili tablo satırı okunur; durum yalnız renkle anlatılmaz.

### 8.3 — Personel ve tek görev ön hesap akışı

Önerilen dosyalar: `ViewModels/PersonnelViewModel.cs`, `TemporaryDutyViewModel.cs`, `PermanentDutyViewModel.cs`; `src/GorevYolluklari.Application/TravelPreviewService.cs`; `tests/GorevYolluklari.ApplicationChecks/`.

- [ ] `PreviewSameDay(SameDayDutyRequest)` / `PreviewRelocation(RelocationDutyRequest)` çağrılarını mevcut HarcirahPreviewEngine'e ileten ince uygulama servisi kur; formül kopyalama.
- [ ] Personel → görev bilgisi → kanıt/değerlendirme → ön hesap dökümü akışını kur; zorunlu veri eksikliğini ilgili alanda göster.
- [ ] Sürekli görev tutarını açıkça “yer değiştirme bileşeni ön hesabı” olarak adlandır; yol/aile seyahat kalemlerini tamamlanmış gösterme.
- [ ] Çok günlü takvimi aday olarak göster; incelenmiş hak günlerini ayrı girişte tut. Önceki gün defteri eksikse otomatik sıfır verme.
- [ ] Veri değişince hesap snapshot'ını ve ilgili inceleme durumunu geçersiz kıl; yeniden hesapla/kontrol etmeden taslak çıkışına izin verme.

Kabul: sentetik görevler mevcut motorla aynı ham sonucu verir; belirsiz mahalliyet, eksik kanıt, özel statü, 2027 tarihi ve eski snapshot çıkışı engellenir.

### 8.4 — Hibrit DataGrid ve kontrollü aktarım

Önerilen dosyalar: `Views/DutyGridView.xaml`, `ViewModels/DutyGridViewModel.cs`, `Import/TabularPasteParser.cs`; gerekirse ayrı `GorevYolluklari.Exchange` projesi.

- [ ] Form ve DataGrid aynı kayıt modelini düzenlesin. Tarih/saat, gider, konaklama ve kanıt satırları ayrı tablolarda açık başlıklarla sunulsun.
- [ ] Hücre düzenleme, Tab/Enter gezinme, çoklu satır seçimi, TSV kopyalama/yapıştırma, toplu ekleme ve işlem önizlemesi kur.
- [ ] `TabularPasteParser.Parse(string tsv)` satır/sütun hataları döndürsün; `tr-TR` tarih/ondalık, boş hücre, metin başındaki sıfırlar ve yinelenen kimlikleri sınasın. Hatalı toplu işlem sessizce kısmen kaydedilmesin.
- [ ] Hesaplanan tutarlar salt okunur olsun. Hesap formülleri ve inceleme işaretleri yapıştırılarak değiştirilemesin.
- [ ] XLSX ihtiyacını ayrı teslim olarak ele al: Office gerektirmeyen kütüphaneyi lisans/güncel paket incelemesiyle seç; şablon sürümü, sütun eşleme, formüllü hücre reddi ve tekrar içe aktarmada mükerrer kontrolü ekle. Paket bu inceleme turunda kurulmayacak.

Kabul: aynı veri formdan ve tablodan aynı sonucu üretir; Türkçe sayı/tarih dönüşümü veri kaybettirmez; hatalı yapıştırma önceki kayıtları değiştirmez.

### 8.5 — Kanıt kapısı ve taslak belgeler

Önerilen dosyalar: `src/GorevYolluklari.Application/DraftPreparationService.cs`, `Views/DraftReviewView.xaml`; mevcut Documents katmanına kontrollü bağlantı; yeni belge/PDF kontrolleri.

- [ ] `DraftPreparationService.Prepare(DraftDocumentRequest)` mevcut DraftDocumentService kontrolünü kullansın. Kategori referansı ayrı saklansın; belgeye genel kaynak referansı taşınırken izlenebilirlik korunsun.
- [ ] İki onay kutusunu otomatik true yapma; gerçek kanıt referanslarını ve inceleme kaydını iste. Bağımsız toplamı aynı uygulama toplamını kopyalayarak doldurma.
- [ ] Yuvarlama politikası kurum örnekleriyle kesinleşene kadar ham kesirleri otomatik ödeme satırına çevirme; belgeli tam kuruş satırları üzerinden kontrollü taslak akışını koru.
- [ ] JSON'dan yüklenen belge ve icmalleri yeniden doğrula; icmal toplamı her belgenin toplamıyla eşleşsin. Geçersiz tür/yıl, negatif net, değiştirilmiş satır, eksik kanıt ve tutarsız toplam testleri ekle.
- [ ] PDF'leri render ederek kurum/uyarı bandı, uzun adlar, satır taşması, 12/13/40 gider satırı ve 14/15 icmal kaydını görsel denetle; gerçek A4 ölçülerini ve taslak uyarısını ayrıca kontrol et.
- [ ] Çıktı adı benzersiz olsun; var olan PDF üzerine yazılmasın. Önizleme, PDF oluşturma ve klasör açma sonuçları açık bildirilsin.

Kabul: kanıt veya bağımsız toplam yoksa açıklamalı ret; geçerli sentetik veriden yalnız taslak PDF; her sayfada taslak uyarısı; resmî imza/ödeme işlemi yok.

### 8.6 — Taşınabilir yayın ve kapanış

- [ ] Önceki 256 kontrolü aynen koru; Storage/Application/aktarım kontrollerini ayrı say.
- [ ] WPF Release build ve görünür UI akışını doğrula; ekran boyutları/DPI/klavye ve yeniden açılış kontrollerini kaydet.
- [ ] `scripts/Build-Portable.ps1` ile benzersiz temiz hedefe win-x64 self-contained paket hazırla; Documents bağımlılığının pakette bulunduğunu doğrula. Kullanıcı verisi/yedek/çıktı dağıtıma girmesin; kaynak dağıtım izinlerini kontrol et.
- [ ] SDK, Runtime ve Office bulunmayan temiz ikinci Windows ortamında çevrimdışı açılış → kayıt → yeniden açılış → ön hesap → taslak PDF → yedek/geri yükleme akışını doğrula. Bu ortam yoksa kabulü bekliyor olarak bırak.
- [ ] README, ProjectInfo ve kullanım notlarını gerçek kapsamla eşleştir; açık mevzuat ve form mutabakatı işlerini ayrı tut.

## Doğrulama komutları

Proje kökünden:

```powershell
& .\07_Asama7_Belge_Taslaklari_Testleri.ps1
& .\02_Asama2_Kural_Testleri.ps1
& .\03_Asama3_Tarife_Testleri.ps1
dotnet build '.\src\GorevYolluklari.App\GorevYolluklari.App.csproj' --configuration Release
```

07 betiği 6 → 5 → 4 → 2/3 regresyon zincirini çağırır. 03 betiği ayrıca yeni tarihli CSV üretir; 07 yeni benzersiz DEMO klasörüne üç sentetik PDF kopyalar. 01 indirici ve publish bu ilk incelemede çalıştırılmadı.

## Teslim öncesi risk odakları

- Yazma yetkisi olmayan klasör / disk doluluğu: eski JSON korunmalı; anlaşılır hata verilmeli.
- Desteklenmeyen şema / bozuk JSON: sıfır kayıt gibi gösterilmemeli; kurtarma sunulmalı.
- Personel, tarih veya kanıt değişimi: eski sonuç/taslak yeniden incelemesiz kullanılamamalı.
- Toplu yapıştırma / XLSX formülü: hesap hücreleri ve inceleme kararları dış girdiden değişmemeli.
- Kaynak/kategori referansı ve ham/tam kuruş farkı: izlenebilirlik korunmalı; bağımsız kontrol taklit edilmemeli.

Önerilen sonraki teslim: bu plan değerlendirildikten sonra 8.1 ve 8.2 ile kayıt temeli ve gezinilebilir WPF kabuğu. Mevzuat yorumunu genişleten veya resmî ödeme çıktısını açan değişiklik bu planın kapsamında değildir.

## Uygulama kaydı — 10.11.2026

Kullanıcı planın uygulanmasına ve GitHub'a kaynak gönderimine izin verdi. 8.1–8.5 yazılım akışı ve 8.6 yerel yayın/test işleri uygulandı; son tam doğrulama 319/319. Temiz ikinci Windows, gerçek yüksek DPI/klavye ve hukuki/form mutabakatı kabul kapıları açıktır. Yukarıdaki kutular ilk plan kaydıdır; tek tek uygulanmış durum ve kanıtlar `ASAMA8_UYGULAMA_VE_GITHUB_RAPORU_2026-11-10.md` içindedir.
