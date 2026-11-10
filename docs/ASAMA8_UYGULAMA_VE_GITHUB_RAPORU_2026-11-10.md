# Aşama 8 uygulama ve GitHub teslim raporu

Tarih: 10.11.2026. Sürüm: 0.8.0. Hukuki nitelik: **yalnız ön hesap ve KONTROL TASLAĞI — ÖDEME BELGESİ DEĞİLDİR**.

## Uygulanan işler

| Plan | Teslim edilen davranış |
|---|---|
| 8.1 | Personel/görev kayıt sözleşmeleri, derin kopyalı hesap snapshot'ı, JSON şema ve revizyon kontrolü, tek yazıcı kilidi, atomik yazım, tarihli yedek ve koruyucu geri yükleme |
| 8.2 | Lacivert–altın yerel WPF stil sözlüğü; yedi bölüm; okunabilir giriş/tablo renkleri; kaydırılabilir küçük ekran düzeni; Ctrl+S ve üç pencere profili |
| 8.3 | Mevcut C# motorlarına bağlı günübirlik, sürekli görev yer değiştirme ve incelenmiş çok günlü ön hesap; gerekçeli blokaj; ayrı m.43 aday seyahat takvimi |
| 8.4 | Hücre düzenleme/kopyalama ve doğrulamalı TSV toplu giriş; sürümlü değer şablonuyla Office gerektirmeyen XLSX içe/dışa aktarımı; formül reddi |
| 8.5 | Gerçek inceleme beyanı ve bağımsız tam kuruş toplamları isteyen taslak hazırlama; eski snapshot reddi; PDF/temel icmal doğrulaması; mevcut PDF'yi ezmeme |
| 8.6 | Tek komut test kapısı, self-contained win-x64 yayın, aynı makinede ayrı veri köküyle EXE açılış denemesi, kaynak paylaşım denetimi ve GitHub Desktop bağlantısı |

Güzergâh, tarih, personel ve ilgili kanıt alanı değişiklikleri ilgili incelemeleri yenilemeyi gerektirir. Bir görevde değişiklik, ilgisiz başka görevin hesap snapshot'ını silmez. Seyahat adayları, hak günü ve tutar satırlarına otomatik aktarılmaz.

SQL, SQLite, Office Interop veya başka UI çatısı eklenmedi. XLSX işlemleri System.IO.Compression / XML ile sınırlı değer şablonunu işler; genel amaçlı Excel dosyası okuyucusu değildir. Örnek projelerden kaynak kodu/asset kopyalanmadı; yalnız genel arayüz, kayıt ve yedek desenleri uygulandı.

## Korunan hesap altyapısı

Rules, Tariffs ve Integration altındaki **yedi mevcut hesaplama/tarife dosyası** değişiklik öncesi yedekle SHA-256 açısından birebir aynı doğrulandı. Önceki testler silinmedi, azaltılmadı veya atlanmadı.

PDF yazıcısında yalnız yerleşim ve güvenlik güçlendirmeleri yapıldı: kurum satırı taslak uyarı bandının altına alındı; icmal toplamları belge toplamlarıyla yeniden karşılaştırılıyor; geçersiz tür, negatif net ve kesirli kuruş yeniden reddediliyor. Bu değişiklikler resmî form kabulü anlamına gelmez.

## Test kanıtı

Son tam kapı: `08_Asama8_IsAkisi_Testleri.ps1`, çıkış kodu **0**.

| Kontrol grubu | Başarılı |
|---|---:|
| Aşama 2 kurallar | 11/11 |
| Aşama 3 tarifeler | 23/23 |
| Aşama 4 entegrasyon | 42/42 |
| Aşama 5 çok günlü/konaklama | 48/48 |
| Aşama 6 seyahat | 55/55 |
| Aşama 7 belge modeli | 70/70 |
| Aşama 7 PDF | 7/7 |
| Yeni kayıt/aktarım/iş akışı | 32/32 |
| Yeni PDF güvenliği | 4/4 |
| Yeni WPF kontrolleri | 27/27 |
| **Toplam** | **319/319** |

WPF kontrolü 1024×768, 1366×768, 1920×1080 ölçülerinde yedi bölümü açıp PNG üretir. Onaysız demo blokajını, JSON kaydını, görevler arasında snapshot izolasyonunu ve güzergâh değişince incelemenin yenilenmesini de sınar. Bunlar gerçek yüksek DPI ve klavye kullanıcı kabulünün yerine geçmez.

Tam kapı ilk derlemesinde eski belge test yardımcısında CS8602 görüldü. Yardımcının false koşulda dönmediğini bildiren `DoesNotReturnIf(false)` açıklaması eklendi; ardından ilgili proje **0 hata, 0 uyarı** ile yeniden derlendi ve **70/70** kontrol tekrar geçti. Test koşulları ve sayısı değişmedi. PDF testindeki önceki CS8603 düzeltmesinin zaten uygulanmış olduğu doğrulandı.

Görsel PDF kontrolünde eski kurum/uyarı çakışması PNG üzerinde görüldü ve konum testi başarısız oldu. Yeni PDF yatay A4 ölçüsü, taslak uyarısı ve kurum konumu testinden geçti; yeni PNG ayrıca gözle incelendi.

Geliştirici test kayıtları Windows geçici klasöründe: `GorevYolluklari_Asama8_Final_3.log`, `GorevYolluklari_Asama8_UyariKontrol.log`, `GorevYolluklari_Asama8_Yayin_Son.log`. Ekranlar `Ciktilar/ASAMA8_UI_1cd11b2af07d4a7a8b50edbf5b354868/` altında, Git kapsamı dışındadır.

## Taşınabilir paket

Son paket:

```text
dist/GorevYolluklari_20261110_215025_7d9aa73d/
  GorevYolluklari.exe
  KULLANIM.md
  Kaynaklar/
  Veriler/
  Yedekler/
  Ciktilar/
```

- Hedef: win-x64; self-contained; tek EXE yayın; trimming kapalı.
- EXE: 141.722.308 bayt.
- SHA-256: `BD86D313E00C7A37CE6B73A07C97FA2961024E67F6E2C882883D75702AF4825D`.
- EXE son ürün kaynaklarından daha yeni; OptionalDateInput ve PreviewTimeline bağlantılarının pakette bulunduğu doğrulandı.
- Paket içindeki Veriler, Yedekler ve Ciktilar boş doğrulandı; kullanıcı kayıtları ve test çıktıları pakete alınmadı. İç içe Kaynaklar/Kaynaklar klasörü yok.
- EXE ayrı, sentetik bir veri köküyle ve geçersiz DOTNET_ROOT / kapalı çok katmanlı arama ortamında çalıştırıldı; açık kaldığı ve yanıt verdiği görüldü, test süreci sonlandırıldı. **Bu, SDK/Runtime/Office olmayan temiz ikinci Windows makinesi kabul testi değildir.**

İlk ara yayın klasörü de dist altında korunur; son deneme için yukarıdaki paket kullanılmalıdır. SHA-256 yalnız dosya bütünlüğü içindir; hukuki uygunluk veya imza kanıtı değildir.

## GitHub ve yerel klasör

- Depo: [mcalimlier/Gorev-Yolluklari-Hesaplama-Sistemi](https://github.com/mcalimlier/Gorev-Yolluklari-Hesaplama-Sistemi), **özel**.
- Yerel klasör aynı masaüstü konumunda korunarak Git deposuna dönüştürüldü; ana dal `main`, uzak ad `origin`.
- GitHub Desktop'ın kendi `github open` komutuyla bu klasör kaydedildi; uygulama ağacında proje adı ve main dalı gözlendi.
- Kaynaklar, testler ve arındırılmış Markdown belgeleri Git kapsamındadır. Kişisel kayıtlar, yedekler, çıktılar, bin/obj/dist, ham kaynak PDF/XLSX/HTML dosyaları ve yerel düzeltme yedekleri kapsam dışındadır. Bunlar yerelde korunur; GitHub kişisel veri yedeği değildir.
- Paylaşım öncesinde `scripts/Check-ShareableFiles.ps1` ile dosya kapsamı, yaygın sır ve kimlik/IBAN örüntüleri tarandı. Bu örüntü taraması tek başına her türlü kişisel veri bulunmadığının matematiksel garantisi değildir.
- GitHub release veya resmî dağıtım yayını oluşturulmadı; taşınabilir paket yereldedir.

## Değişen dosya grupları

- Yeni: Core/Records, Application katmanı, Storage/Records, App/Themes ve ViewModels; yeni üç kontrol projesi.
- Güncellenen: WPF App/MainWindow, proje referansları/sürüm, PortablePaths, PDF yazıcısı ve belge test yardımcısının null akış açıklaması.
- Yeni/güncellenen araçlar: GorevYolluklari.slnx, 08 test kapısı, taşınabilir yayın, paylaşım taraması, isteğe bağlı PDF yerleşim kontrolü, gitignore/gitattributes.
- Belgeler: README, kısa kullanım, ilerleme, uygulama planı, bu rapor ve AGENTS durum notu.

Değişiklik öncesi kaynak yedeği Windows geçici klasöründeki `GorevYolluklari_Asama8_2152c9e034d1457e83ecfecfab22dc87` dizininde tutuldu. Mevcut Veriler/Yedekler/Kaynaklar dosyaları başka ZIP'lerle değiştirilmedi.

## Açık kabul kapıları ve sonraki adım

1. Temiz ikinci Windows bilgisayarında çevrimdışı açılış → kayıt → tekrar açılış → ön hesap → taslak PDF → yedek/geri yükleme kabulü.
2. Gerçek %125/%150 DPI ve klavye kullanıcı testi; kaynakları bulunmayan yeni klonun kurulum/kaynak geri getirme denemesi.
3. Güncel mevzuat ve BUT-003 katalog ayrımı; özel statü/hak sahipliği incelemesi; gerçek anonim dosyalarla kuruş/yuvarlama ve Örnek 27/28 alan/form mutabakatı.
4. Parola tabanlı taşınabilir şifreli yedek ayrı sonraki kapsamdır; mevcut JSON/yedekler şifreli değildir.

Bağımsız alt ajan aracı bulunmadığından son kaynak/plan incelemesi yazar tarafından yapıldı. Bu bir bağımsız kod incelemesi değildir. Resmî ödeme kullanımına hazır olunduğu iddia edilmez.
