# GÖREV YOLLUKLARI HESAPLAMA SİSTEMİ
## Proje devir notu, yapılanlar, kısıtlar ve ayrıntılı yol haritası

**Kayıt tarihi:** 09.11.2026  
**Durum:** Aşama 1 kaynak toplama büyük ölçüde tamamlandı; Aşama 2 matematiksel **ön hesap** testleri **11/11 başarılı**. Henüz mevzuat uyumlu nihai hesap / resmî bordro hazır değil.  
**Bu dosyanın görevi:** Yeni bir ChatGPT sohbetinde tüm proje bağlamını koruyup aynı noktadan devam etmeyi sağlamak.  
**Bilgi kaynağı:** Kullanıcının aktardığı Windows/PowerShell test çıktıları, önceki iki teslim ZIP paketinin içeriği, GitHub'da incelenen mevcut proje dosyaları ve 09.11.2026 tarihli güncel kaynak denetimi. Yerel PC dosyalarını asistan doğrudan görüntüleyemedi.

---

## 1. Projenin amacı, ilk kapsam ve başarı ölçütü

Türkiye'deki kamu personeli için **yurt içi geçici görev yolluğu** ve **yurt içi sürekli görev yolluğu** hesabını, özellikle MEB okul/kurum mali işlerinde kullanılabilecek biçimde gerçekleştiren Windows uygulaması.

İşlevler:
1. Personel, kadro/unvan/derece/ek gösterge ve görev/atama bilgilerini almak.
2. Hak sahipliği ve memuriyet mahalli dışı/içi değerlendirmesini mevzuat dayanağıyla yapmak.
3. Yıl ve yürürlük tarihine göre gündeliği belirlemek; geçici görev gündeliği, yol, konaklama, avans ve uygun diğer kalemleri hesaplamak.
4. Sürekli görevde personel, hak sahibi aile fertleri, yol, yer değiştirme ve KGM mesafesine bağlı kalemleri hesaplamak.
5. Resmî bildirim şablonları **Örnek 27 / Örnek 28**, ayrı hesap dökümü, istenirse toplu bordro/icmal, PDF/yazdırma çıktıları üretmek.
6. İnternet olmadan çalışmak, kullanıcı kayıtlarını saklamak, yedeklemek ve farklı bilgisayara taşınabilmek.
7. Her tutarın **hangi madde, hangi tarife, hangi belge ve hangi veriyle hesaplandığını** açıkça göstermek.

**İlk sürüm kapsamı:** Türkiye içi geçici ve sürekli görev. Yurt dışı yolluk, bütün özel statüler ve e-belge/onay entegrasyonu sonraki sürümlerde değerlendirilecek. Hak sahipliği açık olmayan özel durumlarda otomatik olumlu sonuç çıkarmak YASAK.

**Başarı ölçütü:** Gerçek, kimlik bilgileri temizlenmiş geçici ve sürekli görev örnekleriyle kalem ve kuruş düzeyinde mutabakat; resmî şablonların doğru doldurulması; olumlu/olumsuz/sınır senaryoları için otomatik test; uygulamanın ikinci Windows bilgisayarda harici veri tabanı, Office veya servis olmadan çalışması.

---

## 2. Kullanıcının değiştirilmemesi gereken tercihleri

- **Geliştirme/proje kök yolu (Windows):** `C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi`
- Tüm indirilen mevzuat, PDF, Excel, kurum görüşleri ve kataloglar **`Kaynaklar/`** altında kategorilere ayrılacak.
- **SQL Server, SQLite, veritabanı sunucusu veya harici veritabanı motoru YOK.** Kayıt biçimi yerel **JSON dosyaları**; gerektiğinde indeksleme vb. dosya tabanlı çözümler değerlendirilecek.
- Program **taşınabilir / çevrimdışı** olacak. Nihai paket Windows için `.NET 10`, `WPF`, **self-contained** yayınla harici .NET Runtime gerektirmeyecek; Office yüklü olmasını istemeyecek. Taşınabilir tek-EXE, veri/kaynak/çıktı klasörleriyle birlikte dağıtılabilir.
- Diğer projelerden alınacak yaklaşım: lacivert–altın WPF kart tasarımı, esnek ekran düzenleri, hızlı toplu giriş için Excel benzeri hibrit tablo, yerel dosya saklama, bozuk veriyi sessizce boş göstermeme, güvenli yedek/geri yükleme, PDF/Excel çıktı, güçlü testler.
- **Excel hesaplamanın asıl kaynağı değil**; NPOI gibi Office gerektirmeyen araçlarla içe/dışa aktarma, şablon ve kontrol için yardımcı araç. Ana muhasebe/harcırah matematiği C# sınıflarında ve `decimal` ile yapılacak.
- Kaynaklar lisans, telif, kişisel veriler ve mevzuat değişiklikleri bakımından kontrol edilmeden kopyalanmayacak / herkese açık GitHub paketine eklenmeyecek.
- Adım adım ilerleme: her önemli kurulum veya hesaplama aşaması sonrasında test, log ve kullanıcı onayı; sağlam olmayan sonuçla resmî ödeme bordrosu üretilmeyecek.

**Dikkat:** EXE yanında veri klasörü tutulması, `Program Files` gibi yazma izni sınırlı dizinlerde sorun çıkarır. Taşınabilir dağıtım kullanıcının yazabileceği dizinde çalışmalı. Parolayı yalnız Windows DPAPI'ye bağlamak, farklı PC'ye taşınabilirliği bozar; parolayla taşınabilir şifreli yedek ileride tasarlanmalı.

---

## 3. Proje klasörleri ve dosyalar

```text
C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi\
├── Kaynaklar\
│   ├── 01_Mevzuat\
│   │   └── 6245_Harcirah_Kanunu_2026_Kontrol_Kopyasi.html  (yardımcı, gayriresmî)
│   ├── 02_Butce_ve_Tarifeler\
│   ├── 03_Belgeler_ve_Sablonlar\
│   ├── 04_MEB_Gorusleri\
│   ├── 05_KGM_Mesafeler\
│   ├── 06_Diger_Resmi_Kaynaklar\
│   └── 99_Katalog_ve_Analiz\
│       ├── Kaynak_Katalogu.json / .csv
│       ├── Mevzuat_Kural_Matrisi.csv
│       ├── ASAMA2_KURAL_KATALOGU.json
│       └── Indirme_Raporu_20261109_215413.csv
├── src\
│   ├── GorevYolluklari.App\
│   ├── GorevYolluklari.Core\Rules\
│   └── GorevYolluklari.Storage\
├── tests\GorevYolluklari.RuleChecks\
├── docs\
│   ├── MIMARI_VE_YOL_HARITASI.md
│   └── ASAMA2_HESAPLAMA_KURALLARI.md
├── Veriler\
├── Yedekler\
├── Ciktilar\
├── scripts\Build-Portable.ps1
├── 01_Kaynaklari_Indir.ps1
└── 02_Asama2_Kural_Testleri.ps1
```

**Önemli:** Bu şema önceki ZIP paketlerinin dosya düzeninden çıkarılmıştır; Windows'ta dosyaların gerçekten mevcut olduğu, kullanıcının başarılı `Expand-Archive` ve test çıktılarıyla desteklenir. Bu belgeyi oluştururken kullanıcının Windows klasörleri uzaktan taranmadı. Bazı PDF dosyaları ve indirme raporu yalnız kullanıcının bilgisayarında bulunur.

### İlk iki teslim

- `GorevYolluklari_Baslangic_Paketi.zip`: WPF uygulama iskeleti, JSON depolama, yerel klasör yapısı, 01 kaynak indirme aracı, kaynak katalogları, H Cetveli JSON özeti, başlangıç mimarisi, taşınabilir publish aracı.
- `GorevYolluklari_Asama2_Kurallar_ve_Testler.zip`: `Rules/` altına üç C# dosyası, test projesi, 11 kontrol, kural kataloğu, aşama-2 notu, test PowerShell'i.

### WPF / teknik parçalar (doğrulanan)

- `src/GorevYolluklari.App/MainWindow.xaml`: lacivert ve altın tonlarda, dört başlangıç kartı içeren taslak pencere; **aktif yolluk hesap ekranı henüz değil**.
- `GorevYolluklari.Core/Rules/SameDayPerDiemPreview.cs`: sadece aynı takvim gününde, geceleme olmadan gündelik **ön hesabı**.
- `GorevYolluklari.Core/Rules/RelocationExpensePreview.cs`: 45. maddedeki üç yer değiştirme kaleminin **ön hesabı**, seyahat kalemleri hariç.
- `GorevYolluklari.Core/Rules/PreliminaryCalculation.cs`: `DutyStationAssessment`, `DraftStatus`, `DraftCalculation` durum ve sonuç modelleri.
- `GorevYolluklari.Storage/PortablePaths.cs`: `AppContext.BaseDirectory` altında `Veriler`, `Yedekler`, `Ciktilar`, `Kaynaklar`.
- `GorevYolluklari.Storage/JsonFileStore.cs`: JSON okuma/yazma, geçici dosya, aynı diskte `File.Replace` ile `.bak` (varsa); bozuk JSON sessizce yok sayılmaz. Henüz çok kullanıcılı kilit, şema migrasyonu, gelişmiş şifreleme ve kurtarma testleri yapılmadı.
- `scripts/Build-Portable.ps1`: `win-x64` self-contained single-file publish hedefi; **Windows publish/çalıştırma doğrulaması henüz raporlanmadı**. `Kaynaklar` kopyalanır; gerçek veri klasörlerinin herkese açık pakete karışmaması kontrol edilmeli.

---

## 4. Kaynak indirmenin kesin bilinen durumu

Kullanıcı tarafından 09.11.2026 21:54 civarında çalıştırılan `01_Kaynaklari_Indir.ps1` komutunun ekran çıktısı:

**15 indirme denemesi; 13 başarılı, 2 zaman aşımı.**

| ID | Açıklama | Son durum |
|---|---|---|
| KAN-001 | 6245 sayılı Kanunun resmî PDF'si | **Eksik:** mevzuat.gov.tr zaman aşımı |
| BUT-001 | 2026 Bütçe Kanunu PDF | Başarılı |
| BUT-002 | 2026 H Cetveli PDF | Başarılı |
| BUT-003 | 2025/1 Hakem Kurulu Kararına ilişkin Resmî Gazete PDF'si | İlk adreste zaman aşımı; kullanıcı daha sonra alternatif PDF indirme komutunu **başarılı** bildirdi |
| BUT-004 | TBB üzerinden 8. dönem toplu sözleşme rehberi | Başarılı |
| BEL-001 | Merkezî Yönetim Harcama Belgeleri Yönetmeliği / şablonlar | Başarılı |
| MEB-001 | 2026 büyükşehir harcırah görüşü | Başarılı |
| MEB-002 | 2026 geçici görev yolluğu görüşü | Başarılı |
| MEB-003 | Aile fertleri / mutat taşıt | Başarılı |
| MEB-004 | Sözleşmeli sürekli görev | Başarılı |
| MEB-005 | Sürekli görev görüşü | Başarılı |
| MEB-006 | Hangi kurum öder görüşü | Başarılı |
| MEB-007 | İkametgâh/toplu sözleşme görüşü | Başarılı |
| KGM-001 | İller arası mesafe `.xlsx` | Başarılı |
| KGM-002 | İlçeler arası mesafe `.xlsx` | Başarılı, ~28 MB |

**Önemli katalog hatası:** İlk kaynak kataloğunda BUT-003'ün adı 8. Dönem Toplu Sözleşme olarak geçiyor; indirilen adresin belgesi **2025/1 Hakem Kurulu Kararı**. Bu, asıl 8. Dönem Toplu Sözleşme metniyle aynı belge değildir; dosya tipi ve içeriği tek tek doğrulanıp katalogda ayrılmalı.

Kullanıcının başarılı bildirdiği **alternatif BUT-003 konumu**:
`Kaynaklar\02_Butce_ve_Tarifeler\2025_1_Hakem_Kurulu_Karari.pdf`

Kullanıcı daha sonra hukuk sitesi HTML metnini aşağıdaki adla başarıyla kaydetti:
`Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu_2026_Kontrol_Kopyasi.html`

**Bu HTML kaynak, KAN-001 resmî PDF'nin yerine geçmez.** Komut yalnızca sayfanın 6245 ve 45. madde gibi kelimeleri içerdiğini doğruladı; mevzuatın tamamını resmî metinle satır satır karşılaştırmadı.

İlk indirme raporu: `Kaynaklar\99_Katalog_ve_Analiz\Indirme_Raporu_20261109_215413.csv` (kullanıcının PC'sinde).

### Doğrulanacak resmî kaynaklar ve bağlantılar

- **6245 yürürlükteki metin:** https://www.mevzuat.gov.tr/MevzuatMetin/1.3.6245.pdf  
- **Mevzuat ana sayfası / arama:** https://www.mevzuat.gov.tr/
- **TBMM resmî kanun durumu:** https://www5.tbmm.gov.tr/develop/owa/kanunlar.durumu?kanun_no=6245
- **2026 değişikliğinin aslı:** 7588 sayılı Kanun, kabul 02.07.2026, Resmî Gazete 11.07.2026, sayı 33307; **md.3, 6245 md.15'i değiştirir**: https://www.resmigazete.gov.tr/eskiler/2026/07/20260711-5.htm
- **Yardımcı/gayriresmî 6245 derlemesi:** https://www.hukuksalegitim.com/mevzuat/6245-sayili-harcirah-kanunu
- **2026 H Cetveli:** https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf
- **SBB 2026 bütçe kanunları:** https://www.sbb.gov.tr/merkezi-yonetim-butce-kanunlari/
- **KGM uzaklıklar:** https://www.kgm.gov.tr/Sayfalar/KGM/SiteTr/Root/Uzakliklar.aspx
- **MEB mali görüşler:** https://sgb.meb.gov.tr/gorusler/gorusler.html

**2026 değişiklik kontrolü (09.11.2026):** 7588 sayılı Kanunun 3. maddesinin yalnız 6245 md.15'e ilişkin değişiklik yaptığını resmî Resmî Gazete HTML sayfasından teyit ettik. Ön hesapta ele alınan md.39 / md.45 bu değişiklikle doğrudan değiştirilmedi. Bu, bütün kanunun resmî güncel metninin bütünüyle karşılaştırıldığı anlamına gelmez.

### Kullanıcının 6245'i doğrulamak için yapabileceği kesin kontrol

1. `mevzuat.gov.tr` üzerinden **6245 – Harcırah Kanunu** araması yap; mümkünse yürürlükteki konsolide metni aç ve PDF olarak kaydet.
2. KAN-001 hedef dosya adı: `Kaynaklar\01_Mevzuat\6245_Harcirah_Kanunu.pdf`. Eski/eksik kopyanın üstüne doğrulamasız yazma.
3. Belgede kanun numarası **6245** ve başlığı **Harcırah Kanunu**; şu maddelerin bulunması kontrol edilsin: **3 (memuriyet mahalli), 4, 5, 10, 14, 33, 39, 42, 43, 44, 45, 47, 56, 59, 60**.
4. 2026 değişikliğini `7588 md.3` üzerinden `6245 md.15` ile karşılaştır; Resmî Gazete yayım tarihi **11.07.2026**.
5. PDF'nin **indirme tarihi, erişim adresi, SHA-256 özeti** katalogda kaydedilsin. İndirme gerçekleşmezse **"resmî kopya teyidi bekleniyor"** olarak bırak.
6. PDF dosyasını kod içinden doğrudan bir ödeme kuralı olarak işletme. Önce her kuralın kaynak maddesini ve istisnalarını ayrı testlerle bağla.

---

## 5. Aşama 2: Kod ve test sonucu

Kullanıcının 09.11.2026 tarihli PowerShell çıktısı:

```text
=== ASAMA 2: KAYNAK ve KURAL KONTROLU ===
[OK] Yardimci kanun kopyasi mevcut (resmi metin yerine gecmez).
[OK] Kural katalogu JSON gecerli.
10.0.402
=== C# KURAL KONTROLLERI ===
[OK] 39. madde - 1/3 ön hesap
[OK] 39. madde - 2/3 ön hesap
[OK] Memuriyet mahalli belirsizse hesap blokajı
[OK] Memuriyet mahalli içi gündelik yok
[OK] 13:00 eşitliği incelemeye gider
[OK] Geceleme özel kurala yönlendirilir
[OK] 45. madde - 20x+20x+%5 x km
[OK] 45. madde - aile bileşeninde 40 gündelik tavan
[OK] Eşin aynı güzergâhta memur nakli manuel inceleme
[OK] Hak sahipliği incelenmeden taşınma hesabı yok
[OK] Negatif kilometre engellenir
TOPLAM: 11 / 11 KONTROL BAŞARILI
UYARI: Bu sonuçlar test amaçlı ön hesaptır, resmî ödeme bordrosu değildir.
```

**Test komutları (PC):**

```powershell
$kok = 'C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $kok '02_Asama2_Kural_Testleri.ps1')
# Doğrudan C# testleri:
dotnet run --project (Join-Path $kok 'tests\GorevYolluklari.RuleChecks\GorevYolluklari.RuleChecks.csproj') --configuration Release
```

**11/11 testi doğru yorumlama:** Kodun seçilen sentetik girdilerde beklenen sonuçları verdiği gösterildi. Tam kanuni uygunluk, H Cetveli otomatik tarife seçimi, gerçek örneklerle karşılaştırma, kuruş yuvarlaması ve bütün özel durumlar **henüz doğrulanmadı**.

**Ön hesap matematiği ve sınırlar:**

- `SameDayPerDiemPreview`: Aynı gün için 13:00 ve 19:00 aralıklarının geçilip geçilmediğine göre `0/3`, `1/3`, `2/3` tutar üretebilir. Tam eşitlik, belge eksikliği, mahalliyet belirsizliği, çok günlük durumlar incelemeye yönlendirilir. **Maddenin bütün uygulama şartları ve günübirlik özel hâlleri henüz test edilmedi.**
- `RelocationExpensePreview`: hak sahipliği onaylanmış varsayılan personel için `20 × gündelik + min(hak sahibi kişi sayısı, 4) × 10 × gündelik + km × 0,05 × gündelik` değerini *yalnız yer değiştirme bileşeni olarak* ön hesaplar. Aile yol masrafı ve yol gündelikleri yoktur. Eş de memursa istisna gerekir. **Resmî bordro toplamı değildir.**
- Çıktı `UnroundedAmount` değerindedir: kuruş yuvarlama yöntemi henüz mevzuat ve gerçek belgeler üzerinden kesinleştirilmedi.
- Testlerde kullanılan günlük `900` ve `1000` TL tutarlar **sentetiktir**, 2026 kanuni tarife değildir.

---

## 6. Bir sonraki çalışma: Aşama 3 — 2026 tarife motoru ve kaynak doğrulama

**Öncelik sırası (aynı aşama içinde):**

### 3.1 Resmî metin ve katalog düzeltmesi

- KAN-001 resmî kanun PDF'si tarayıcı üzerinden temin edilebiliyorsa kaydet, SHA-256 ve tarihini kaydet; ulaşılamıyorsa engeli dürüstçe raporla.
- BUT-003 Hakem Kurulu Kararı'nı 8. Dönem Toplu Sözleşme'den ayrı katalogla; önce mevcut indirilen dosyaların doğru PDF/XLSX olduğunu, gerçek içeriklerini ve tarihlerini doğrula.
- H Cetveli (BUT-002) kategorilerini doğrudan **resmî cetvele** göre kontrol et, gerekiyorsa JSON özetini düzelt. Kullanıcının proje dizininde bulunan kaynaklar asıl referanstır.
- Kurallara madde, geçerlilik başlama/bitiş tarihi, kaynak belge, istisna ve belirsizlik durumu ekle.

### 3.2 Tarife seçimi sınıfı

- `TariffCatalog`, `DailyAllowanceResolver`, `TariffDecision` gibi ayrı .NET Core bileşenleri kur.
- Girdiler: işlem tarihi/yılı, personel statüsü, kadro/aylık derecesi, varsa ek gösterge ve özel statü. Yanlış/yetersiz bilgiye **hesap yapılamaz** sonucu ver.
- 2026 için mevcut `Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli_Veri_Ozeti.json` okunabilir; **taslak JSON** olduğu ve cetvele karşı kesin doğrulanmadığı unutulmasın.
- Üst kademe özel gündelikler ve özel statüler `genel memur` kategorileriyle otomatik eşleştirilmesin.
- Tarifeler yıllara göre sürümlensin; 2027 değerleri eklenince 2026 kayıtları **değişmemeli**.
- Sonuçta seçilen tarifenin `kaynakID`, kural yılı, tarih, kategori, referans ve gerekçesini kullanıcı görebilsin.

### 3.3 Sınır / regresyon testleri

- Bütün derece-ek gösterge eşiklerinin iki yanını test et: `3599/3600`, `6399/6400`, `7999/8000` (ilgili statü ve bütçe cetveli kategorisi uygun olduğunda).
- Sınır tarihleri: `31.12.2025`, `01.01.2026`, `31.12.2026`, `01.01.2027` (gelecek tarife yokken **blokaj**).
- Eksik derece/ek gösterge, hatalı personel statüsü, negatif tutar, yanlış cetvel yılı, yinelenen tarife kaydı ve kaynak değişimi.
- Hesaplanan gündelik değeri 39 ve 45. madde taslaklarına kaynaklı olarak bağla; bağımsız `decimal` testleri.
- Testler sonrası çıktı: **başarı sayısı, hata ayrıntısı, henüz işlenmeyen istisnalar**.

### 3.4 Teslim standardı

- Var olan klasöre **yalnız yeni/düzeltilmiş** kaynak kodu/katalog/test dosyalarını ekleyen paket; eski JSON verileri ve indirilen resmî kaynaklar üzerine koşulsuz yazılmasın.
- Windows PowerShell ile **tek komut test**. Başarılı değilse kullanıcıya düzeltme öncesi resmî hesap tamamlandı denmesin.
- Uygulamanın temel arayüzü bu aşamada kısmi olabilir; önce hesap doğruluğu.

---

## 7. Aşama 4–9: Ayrıntılı yol haritası ve kabul ölçütleri

| Aşama | Öncelikli işler | Kabul kapısı |
|---|---|---|
| 4 — Hak sahipliği motoru | 6245 md.3/4/10/14, MEB büyükşehir ve memuriyet mahalli görüşleri; personel ve atama statüsü; belge bazlı karar ağacı | İstisnalar için `Uygun/Değil/İnceleme`, her karara kaynak maddesi, açıklama ve test |
| 5 — Geçici görev tam hesap | md.33,39,42,43; gündelik/seyahat günü; görev süresi 90/180 gün; geceleme/konaklama; ulaşım; avans | Sınır günleri, saatleri, tarife yılları, gerçek anonim örneklerle mutabakat |
| 6 — Sürekli görev tam hesap | md.44–47; aile uygunluğu, birlikte taşınma, eşin memuriyeti, ayrılış/başlayış, KGM il/ilçe mesafeleri; yol ve yer değiştirme kalemleri | Resmî mesafe, aile 40 kat sınırı, eş istisnası, gerçek anonim örneklerle mutabakat |
| 7 — Belge üretimi | Yönetmelik ve güncel örnekler; Örnek 27 / Örnek 28 PDF, ayrı kurum bordrosu ve toplu icmal; kalem açıklaması, imza alanları | Belge şablonu görsel ve alan karşılaştırması, PDF okunabilirlik, kuruş/mahsup eşitliği |
| 8 — Tam WPF/hibrit giriş | Personel ve görev formları, tarihler, hibrit Excel benzeri toplu giriş; NPOI şablonu; lacivert-altın, erişilebilirlik/duyarlı ekran | 1024×768, 1366×768, 1920×1080 ve yüksek DPI; veri kaybı/yanlış giriş testleri |
| 9 — Güvenlik/dağıtım | JSON şema sürümü, yedek/geri al, taşınabilir şifreleme, kişisel veri temizliği; self-contained EXE; otomatik build/test | Temiz ikinci Windows makinesinde çevrimdışı çalışır, bağımlılık kurdurmaz; paket kişisel kayıt içermez |

**Paralel zorunlu iş:** Mevzuat ve tarife sürümü değişikliklerinin kayıt altına alınması; her değişiklikte test/veri/çıktı etkisinin değerlendirilmesi.

### Çıktıların hukuki niteliği

- Hesap dökümü ve PDF üretmek = **programın belge hazırlaması**.
- Yetkili kamu personelinin gerekli kontrolleri yapması = **idari/mali uygunluk denetimi**.
- MYS/HYS gibi sistemlerde onay/ödeme = **ayrı resmî süreç**. Program bundan otomatik onay sonucunu çıkarmayacak.

---

## 8. Önceki projelerden aktarılacak örnekler

**GitHub proje 1:** https://github.com/mcalimlier/Ek-Ders-Hakedis-Sistemi  
İncelenen dosyalar: `README.md`, `src/Hakedis.App/Hakedis.App.csproj`. `net10.0-windows`, WPF, `PDFsharp-WPF 6.2.4`; README'de NPOI ile gömülü Excel şablonu, kayda/çıktıya/yedeğe ilişkin yaklaşımlar, hibrit tablo ve PDF üretimi var. İlgili modüller için lisans/bağımlılık durumunu kontrol et.

**GitHub proje 2:** https://github.com/mcalimlier/DogruFatura  
README'de .NET 8 WPF, lacivert–altın kartlar, üç ekran profili, yerel JSON/arşiv/yedek, PDF/yazdırma ve test/dağıtım yaklaşımı bulunuyor. İhtiyaç duyulan desenleri al; **bütün harici UI kütüphanesini zorla entegre etme**.

### Mimari prensibi

`UI (WPF)` → `Application Services` → `Core (tarifeler, yolluk ve mevzuat kuralları)` + `Storage (JSON)` + `Documents (PDF/Excel)`.

Hesap sonuçlarının `CalculationSnapshot` benzeri değişmez bir kaydında şu bilgiler bulunmalı: girdi özeti, kullanılan cetvel yılı, kural sürümü, kaynak belge ID'leri, kaynak tarihleri, kalemler, `decimal` sonuçlar, ara hesaplar, yuvarlama, olası istisna uyarıları, kullanıcı ve oluşturma tarihi. Sonradan cetvel güncellenmesi geçmiş çıktıyı değiştirmemeli.

---

## 9. Bu devir dosyasının kapsamadıkları ve dürüst durum özeti

- Kullanıcının Windows diskindeki gerçek PDF, XLSX ve HTML dosyaları bu sohbet ortamına yüklenmiş değil; loglardaki başarılı indirme beyanı, dosyaların içeriğini bütünüyle incelediğimiz anlamına gelmez.
- 11 C# testi **kullanıcının makinesinde geçti**; testlerin kapsamı açıklanan ön hesap senaryolarıyla sınırlı.
- Kanunun resmî 6245 PDF'si halen eksik, yardımcı HTML'nin tam konsolide metin doğrulaması tamamlanmadı.
- Tarife motoru, gerçek tüm hak sahipliği kuralları, tam geçici/sürekli hesap, resmî bordro ve gerçek PDF şablon doldurma **henüz yapılmadı**.
- `Build-Portable.ps1` mevcut ama programın self-contained EXE halinde gerçek Windows smoke testinin başarı çıktısı henüz kullanıcıdan gelmedi.
- Katalogdaki BUT-003 başlığı düzeltilmeli, yerel alternatif kopya bütünlük/kaynak doğrulaması yapılmalı.
- 2026 dışındaki yılların cetvelleri ve özel statü tabloları henüz tamam değil.

---

## 10. Yeni sohbet açınca yapıştırılacak başlangıç mesajı

> `GÖREV YOLLUKLARI HESAPLAMA SİSTEMİ` projemize kaldığımız yerden devam edelim. Projenin ayrıntılı durumu, kaynakları, klasör yapısı, başarılmış testleri ve yol haritası eklediğim `Gorev_Yolluklari_Proje_Devir_ve_Yol_Haritasi_2026-11-09.md` içindedir. **Önce bu dosyayı tamamen oku.** Geliştirme yolumuz `C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi`. SQL/SQLite/harici servis yok; taşınabilir .NET 10 WPF, yerel JSON, hibrit Excel ve lacivert–altın tasarım istiyorum. Aşama 2'nin 11/11 matematik testleri geçti. Resmî 6245 PDF henüz teyitsiz; yardımcı HTML kopyası var. İlk iş Aşama 3: kaynak/katalog düzeltme, 2026 H Cetveli tarife motoru, sınır testleri. Her değişikliği doğrula, yarım işleri tamamlandı diye sunma; adım adım ilerle. Gerekirse önceki iki ZIP paketini veya kaynak kod dosyalarını da yüklememi iste.

**Önemli:** Bu `.md` dosyası çalışma geçmişini ve kararları taşır; kaynak kodun, PDF/Excel kaynaklarının veya canlı Windows bilgisayarının yerini almaz. Yeni sohbet kod düzenleyecekse, güncel proje ZIP'i ya da erişilebilir GitHub deposu ayrıca gerekir.

---

## 11. Yeni oturum için kontrol listesi

- [x] Başlangıç klasör yapısı ve ilk kaynak katalogları hazırlanmış.
- [x] 13 dosya indirilmiş; BUT-003 daha sonra alternatif konumdan başarılı bildirilmiş.
- [x] 6245 yardımcı HTML kaydedilmiş (resmî onay değil).
- [x] .NET 10.0.402 mevcut; Aşama 2 C# testleri 11/11 başarılı.
- [ ] KAN-001 resmî 6245 PDF ve SHA-256 doğrulaması.
- [ ] BUT-003 adı/içeriğinin ayrı 8. dönem ve Hakem Kurulu kaydı olarak düzeltilmesi.
- [ ] 2026 H Cetveli kategorileri resmî belge üzerinden tarife sınıfına bağlansın.
- [ ] Tarife seçici sınır ve yıl testleri geçsin.
- [ ] Hak sahipliği ve mahalliyet karar ağacı tamamlanıp test edilsin.
- [ ] Geçici görev tam hesap ve bağımsız testler.
- [ ] Sürekli görev tam hesap ve bağımsız testler.
- [ ] Örnek 27/28 ve toplu bordro/ödeme icmal PDF çıktısı.
- [ ] Gerçek anonim örneklerle tutar ve belge mutabakatı.
- [ ] Çevrimdışı taşınabilir EXE ve yeni PC smoke testi.

**Sıradaki somut iş:** KAN-001/belge kataloğu kontrolünün yanı sıra **2026 H Cetveli tarife motorunu** yazmak, negatif/eksik veri/ek gösterge/derece/tarih sınırlarında test etmek ve kullanıcıya tek komutla sonuç doğrulatmak.
