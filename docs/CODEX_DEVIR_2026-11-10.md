# Codex devir dosyası — Görev Yollukları Hesaplama Sistemi

**Hazırlanma tarihi:** 10 Kasım 2026  
**Kapsam:** ChatGPT ile Aşama 1–7'de yapılan işlerin Codex'e aktarımı; **Aşama 8 başlangıcı**  
**Kritik not:** Aşağıdaki test sonuçları kullanıcının Windows PowerShell çıktılarından / beyanından alınmıştır. Codex gerçek dosyaları yeniden incelemeli ve regresyonu kendi ortamında çalıştırmalıdır.

## 1. Projenin konumu ve hedefi

Windows proje kökü:

```text
C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi
```

MEB ve diğer kamu kurumlarında kullanılmak üzere **yurt içi geçici ve sürekli görev yolluğu** ön hesaplama, kanıt/hak sahipliği denetimi, personel kayıtları, görevlendirme bilgileri ve PDF taslakları hazırlayan taşınabilir uygulama. Yurt dışı yolluklar ilk sürümde yok.

Teknik kararlar:

- C# / **.NET 10 WPF**, Windows odaklı; mevcut kodu koruyarak geliştirme.
- Harici **SQL veya SQLite kullanmadan**, yerel JSON dosyaları; `Veriler`, `Yedekler`, `Ciktilar` ve `Kaynaklar` klasörleri.
- `win-x64`, self-contained dağıtım. Kullanıcıya Office veya .NET Runtime kurdurulmamalı. Geliştirici cihazda .NET SDK ve paket indirmesi gerekebilir.
- Hesaplama **C# iş kurallarıyla** yapılır. Excel benzeri arayüz, Excel içe/dışa aktarım ve bağımsız kontrol yardımcıdır. Excel.exe/Interop gerekmemeli.
- Arayüz lacivert–altın, yerel WPF stilleriyle; Ek Ders Hakediş Sistemi ve DoğruFatura'nın iyi desenlerini seçerek kullan. Büyük bir UI kütüphanesini toptan entegre etme.
- Yasal uygunluk ve resmî ödeme onayı uygulamadan ayrı bir süreçtir.

## 2. Kaynak klasörü ve hukuk

Resmî belgeler kullanıcı tarafından `Kaynaklar/` içine indirilmiştir:

```text
Kaynaklar/
  01_Mevzuat/            6245_Harcirah_Kanunu.pdf
  02_Butce_ve_Tarifeler/  2026 bütçe kanunu, H Cetveli, toplu sözleşme ve hakem kurulu belgeleri
  03_Belgeler_ve_Sablonlar/  Harcama Belgeleri Yönetmeliği ve PDF şablonları
  04_MEB_Gorusleri/       Büyükşehir, geçici/sürekli görev, sözleşmeli vb.
  05_KGM_Mesafeler/       ilmesafe.xlsx, ilcemesafe.xlsx
  06_Diger_Resmi_Kaynaklar/
  99_Katalog_ve_Analiz/   kaynak kataloğu, kural matrisi, indirme/doğrulama raporları
```

Kaynak indirme ilk çalışmada **13 başarılı / 2 zaman aşımı** idi. Daha sonra `BUT-003` için alternatif bir PDF kaydedildi, `KAN-001` için yardımcı HTML ve ardından tarayıcıdan resmî 6245 PDF indirildi. Aşama 3'te kullanıcı PDF imzalarını ve SHA-256 değerlerini doğruladı:

- 6245 PDF: `D2DC0D7269AD5610B2D898C41190328A55B64AB9633AAA1AEAD4B00450C8C17C`
- 2026 H Cetveli: `A40F65A5621E3FC4988F8BF9C644A0157BB50C8AEF9124EDB26D79F72C52BA2C`

Bu SHA-256 değerleri **dosya tutarlılığını** kontrol etmeye yarar; hukuki güncelliği doğrulamaz. Güncel 6245, 2026 H Cetveli, 8. Dönem Toplu Sözleşme/Hakem Kurulu Kararı, MEB görüşleri ve Merkezî Yönetim Harcama Belgeleri Yönetmeliği ayrıca mevzuat bakımından denetlenmelidir. Özellikle memuriyet mahalli ve özel statülerde otomatik hak kazanımı varsayılmamalı.

## 3. Tamamlanan aşamalar / kullanıcının bildirdiği testler

| Aşama | Konu | Yeni kontrol | Bildirilen durum |
|---|---|---:|---|
| 1 | Yerel kaynak kataloğu, resmi belge indirme, WPF/JSON başlangıç | — | Belgeler toplandı; PDF doğrulandı |
| 2 | 6245 m.39 günübirlik ve m.45 taşınma ön hesaplama | 11 | 11/11 başarılı |
| 3 | 2026 H Cetveli tarife seçimi, belge imza ve SHA-256 | 23 | 23/23 başarılı |
| 4 | Tarife + geçici/sürekli ön hesap entegrasyonu | 42 | Toplam 76/76 başarılı |
| 5 | Çok günlü görev, m.42 90/180 gün, 2026 konaklama aday sınırları | 48 | Toplam 124/124 başarılı |
| 6 | Seyahat takvimi, 39/43. madde aday seyahat günleri ve kontroller | 55 | Toplam 179/179 başarılı |
| 7 | Örnek 27/28 **taslak** belge modeli, kuruş/avans, icmal ve PDF kontrolleri | 70+7 | Toplam **256/256 başarılı** bildirildi |

**Aşama 7 derleme geçmişi (çok önemli):**

1. `src/GorevYolluklari.Documents/TravelDraftPdfWriter.cs` dosyasında `File`, `Path`, `Directory`, `InvalidDataException` hataları oluştu. Kullanıcı `using System.IO;` ekledi.
2. Ardından `tests/GorevYolluklari.PdfChecks/Program.cs` dosyasında aynı hata görüldü; kullanıcı oraya da `using System.IO;` ekledi.
3. Kullanıcı bütün testlerin başarılı olduğunu, yalnızca PDF test `Program.cs` satır 18'de **CS8603** olası null dönüş uyarısı kaldığını bildirdi.
4. `return decision.Document;` ifadesini `return decision.Document ?? throw new InvalidOperationException("Taslak belge olusturulamadi.");` ile değiştiren PowerShell düzeltmesi sunuldu. **Kullanıcı henüz bu son düzeltmenin sonucunu paylaşmadı.** Gerçek dosyayı önce kontrol et; gerekirse güvenli şekilde düzelt ve testleri yeniden çalıştır.

Bu sonuçlar *testlerin geçtiğini* gösterir; ödeme hak sahipliği, istisnalar, vergiler, yuvarlama ve resmî şablon mutabakatının tamamlandığını **göstermez**.

## 4. Klasör ve dosya haritası

Önce gerçek dosya ağacını tara; aşağıdakiler beklenen önemli konumlardır:

```text
README.md
AGENTS.md                           # Bu aktarım paketinden
01_Kaynaklari_Indir.ps1
02_Asama2_Kural_Testleri.ps1
03_Asama3_Tarife_Testleri.ps1
04_Asama4_Birlesik_OnHesap_Testleri.ps1
05_Asama5_CokGunlu_Konaklama_Testleri.ps1
06_Asama6_Seyahat_Takvimi_Testleri.ps1
07_Asama7_Belge_Taslaklari_Testleri.ps1
docs/
  PROJE_DEVIR_NOTU_2026-11-09.md
  ASAMA2_HESAPLAMA_KURALLARI.md
  ASAMA3_2026_H_CETVELI.md
  ASAMA4_TARIFE_HESAP_ENTEGRASYONU.md
  ASAMA5_COK_GUNLU_KONAKLAMA_MEVZUAT.md
  ASAMA6_SEYAHAT_TAKVIMI_VE_M43.md
  ASAMA7_BELGE_TASLAKLARI_VE_KABUL_KAPISI.md
  CODEX_DEVIR_2026-11-10.md        # Bu dosya
src/
  GorevYolluklari.App/             # mevcut temel WPF arayüzü
  GorevYolluklari.Core/            # hesaplama, tarife, seyahat, taslak belge sözleşmesi
  GorevYolluklari.Storage/         # yerel JSON altyapısı
  GorevYolluklari.Documents/       # PDFsharp-WPF ile taslak PDF
 tests/
  GorevYolluklari.RuleChecks/
  GorevYolluklari.TariffChecks/
  GorevYolluklari.IntegrationChecks/
  GorevYolluklari.MultiDayChecks/
  GorevYolluklari.TimelineChecks/
  GorevYolluklari.DocumentChecks/
  GorevYolluklari.PdfChecks/
 scripts/Build-Portable.ps1
 Kaynaklar/, Veriler/, Yedekler/, Ciktilar/
```

Mevcut PDF belge katmanı `PDFsharp-WPF` **6.2.4** kullanır. İlk WPF `MainWindow.xaml` basit lacivert/altın panolar içerir ve hâlâ **başlangıç/placeholder** ibareleri bulunduruyor olabilir. Codex, 8. aşamada bu ekranı gerçek iş akışına evriltmeli; mevcut uygulamanın tamamını başka UI kütüphanesiyle değiştirmemeli.

## 5. Başlatma, doğrulama ve riskler

PowerShell'de proje köküne geç:

```powershell
Set-Location -LiteralPath 'C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi'
& .\07_Asama7_Belge_Taslaklari_Testleri.ps1
```

Bu script sırasıyla belge-model testlerini, gerçek PDF testlerini ve 6. aşama ile önceki regresyonları çalıştırır. `dotnet` SDK ve NuGet erişimi gerekir. Eski raporda **256 kontrol başarılı** bildirilmiştir; Codex kendi çalıştırma sonucunu ayrıca kaydetmelidir.

WPF derleme kontrolü (teslim edilen başlangıç proje yolu):

```powershell
dotnet build '.\src\GorevYolluklari.App\GorevYolluklari.App.csproj' --configuration Release
```

Üretilen PDF'lerin örnek dizini: `Ciktilar/ASAMA7_DEMO/` — yalnız örnek personel verileri. **Kullanıcı verilerini yayınlama ve repoya commit etme.** `Veriler/`, `Yedekler/`, kişisel `Ciktilar/`, kurum bilgilerinin bulunduğu PDF/Excel dosyalarının Git kapsamını kontrol et. `Kaynaklar/` içindeki resmî PDF'leri dağıtmadan önce telif/dağıtım uygunluğunu da incele.

## 6. Aşama 8 — Planlanan gerçek WPF uygulaması

Aşama 8'i küçük teslimlerle yürüt, önce **salt-okunur inceleme + tasarım planı** yap ve kullanıcıdan onay al:

1. **Mevcut durumu keşfet:** WPF ekran, Core servisleri ve Storage yeteneklerini haritala; gerçekten hangi hesaplama API'leri tamam ve hangileri yalnız aday/ön hesap, belirle. PDF taslak giriş modelini ve geçerlilik kapılarını incele.
2. **Tasarım planı:** Lacivert–altın görsel sistem, sol menü/üst durum, panel kartları, klavye erişimi, hata durumları. 1024×768 / 1366×768 / 1920×1080 ölçülerini dikkate al. İhtiyaca göre WPF `DataGrid` ile Excel benzeri hücre düzenleme, kopyala-yapıştır, toplu ekleme / kontrol; Excel'i ancak gerçekten faydalıysa bağımsız aktarım olarak ekle.
3. **Ekranlar:** Ana sayfa; Geçici Görev; Sürekli Görev; Personel & Kayıtlar; Belgeler/İcmaller; Kaynak ve Tarifeler; Yedekleme/Ayarlar. Ön hesap ile nihai belgelendirme ayrı ve görsel olarak açık olmalı.
4. **Güvenli örnek akış:** Personel ve görev için demo verilerle giriş, validasyon, C# ön hesap API'sine bağlantı, uyarı ve gerekçe gösterimi, belge taslağı önizlemesi; onaylanmamış verileri gerçek ödeme belgesine dönüştürme.
5. **Kayıt/geri yükleme:** JSON dosyalarına güvenli yazım, yedekleme, bozuk dosyada kullanıcı uyarısı; aşama için yalnız gerekli en az kapsamı uygula. Kayıtların hangi dizinde tutulduğunu taşınabilir dağıtım hedefiyle doğrula.
6. **Test/kalite:** Önceki 256 kontrole dokunma. UI/servis düzeyi yeni testleri ekle; WPF uygulamasını derle; üretilecek çıktıları görsel incelemeye sun. Derleme uyarılarını raporla.
7. **Aşama raporu:** Değişen dosyalar, çalıştırma komutları, testler, açık hukuki konular, PDF taslak durumu ve sonraki teslim planını Markdown olarak kaydet.

**Aşama 8 kabul ölçütü:** Kullanıcı, açılır ve anlaşılır WPF ekranlarda demo geçici/sürekli görev verilerini girebilmeli, hukuken şüpheli durumlarda neden bloke edildiğini görmeli, yalnız `KONTROL TASLAĞI` olarak işaretlenmiş PDF'i üretebilmeli; testler başarısız olmamalı. Kullanıcı onayı olmadan tam ölçekli yeniden tasarım veya otomatik GitHub push yapma.

## 7. Resmî çıktı için açık kabul kapısı

Şu anda **Örnek 27 ve Örnek 28 birebir resmî form eşlemesi tamamlanmadı**. Devam eden eksikler:

- Derece / ek gösterge / gündelik tutarının form kolonlarına otomatik aktarımı.
- Gidiş/dönüş saat, rota, taşıt cinsi/ücreti ve gün sayısı satırları.
- Sürekli görev aile fertleri / taşınma satırlarının resmî form yapısına aktarımı.
- Gerçek anonim yolluk dosyalarıyla hesap + kuruş yuvarlama ve mahsup mutabakatı.
- Form güncelliği, imza/yetki alanları, özel statü ve hak sahipliği incelemesi.
- Ekranlarda her taslak PDF ve icmal için görünür **resmî ödeme belgesi değil** uyarısı.

Bu kapı bitmeden gerçek resmî ödeme bordrosu üretilmiş gibi davranılmamalıdır.

## 8. Codex'in ilk görevi için önerilen komut

```text
Bu çalışma dizini Görev Yollukları Hesaplama Sistemi'dir. Önce kökteki
AGENTS.md ve docs/CODEX_DEVIR_2026-11-10.md dosyalarını, ardından ilgili
Aşama 6-7 belgelerini oku. Önce kod değiştirmeden mevcut src/tests/scripts
mimarisini incele, 07_Asama7_Belge_Taslaklari_Testleri.ps1 ile regresyonu
çalıştır ve WPF Release build'i doğrula. 256/256 geçmiş sonuçla farkları,
CS8603 uyarısının gerçek durumunu, eksik resmî form alanlarını ve 8. aşama
uygulama planını raporla. Veriler/Yedekler/Kaynaklar/Ciktilar üzerinde
değişiklik yapma; yeni paket, branch, commit veya push oluşturma. Önce
benden onay al, sonra küçük parçalarda 8. aşamayı uygula.
```

## 9. Önceki projenin kalan devir notları

- `docs/PROJE_DEVIR_NOTU_2026-11-09.md` (Aşama 1–3 ayrıntıları; zaman içinde eski kalmış olabilir)
- `docs/ASAMA6_SEYAHAT_TAKVIMI_VE_M43.md`
- `docs/ASAMA7_BELGE_TASLAKLARI_VE_KABUL_KAPISI.md`

Çelişkide **gerçek kod ve test çıktısı** önceliklidir; resmî mevzuat doğrulanmadan mali/hukuki iddia üretme.
