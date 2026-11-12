# Geçici / sürekli görev çekirdek ayrımı — 10.11.2026

## Uygulanan yapı
- GorevYolluklari.Contracts: ortak tarife ve ön hesap sözleşmeleri.
- GorevYolluklari.TemporaryDuty.Core: aynı gün, çok gün, konaklama ve seyahat aday süreleri.
- GorevYolluklari.PermanentDuty.Core: yer değiştirme ön hesabı.
- İki çekirdek yalnız Contracts projesine başvurur; birbirine veya üst Core projesine başvurmaz.
- Eski HarcirahPreviewEngine çağrıları uyumluluk katmanıyla korunur. Application doğrudan ilgili çekirdeği çağırır.
- Önceki kural dosyaları fiziksel yerlerinde korunur ve ilgili projede Compile/Link ile derlenir. Formüller değişmedi; seyahat motorunun bağımlılığı geçici çekirdeğe yönlendirildi.

## Arayüz ve veri güvenliği
Ana sayfada başlangıçta seçilmemiş hesaplama modülü bulunur. Seçim yapılmadan açma düğmesi pasiftir; gezinmedeki Geçici/Sürekli düğmesine basmak da açık bir seçimdir. Geçici panelde seyahat saatleri ve incelenmiş günler; sürekli panelde tarih aralığı, aile ve mesafe alanları açılır. Sürekli panel geçici görev dönüş saati veya konaklama defteri göstermez. Belge ekranı seçilmiş modülün kayıtlarını listeler.

JSON şeması ve mevcut görev türleri korunur; modül değiştirmek kayıtları dönüştürmez veya silmez. Genel personel, kanıt ve belge altyapısı ortaktır. Sürekli çekirdek henüz tam sürekli görev yolluğu toplamı üretmez: yalnız yer değiştirme bileşenidir. Kanıt ve resmî form kapıları korunur.

## İnternetten karşılaştırılan örnekler
Erişim: 10.11.2026. Kaynak dosyalarının tamamı depoya kopyalanmadı; sayısal olgular testlerde ve bağlantılar burada tutuldu.

1. [İstanbul Üniversitesi-Cerrahpaşa, Yurt içi harcırahları](https://cdn.iuc.edu.tr/FileHandler2.ashx?f=yurt-ici-harcirahlari-v.1.pdf), PDF sayfa 3: gündelik 880 TL, otel faturası 1500 TL; ilk dönem konaklama üst sınırı 1408 TL. ModuleChecks gerçek çok günlü geçici çekirdekte konaklama bileşenini karşılaştırır. Kaynak örneğinde işlem tarihi ve personel tarife bilgileri verilmediğinden testteki 15.01.2026, derece 4, ek gösterge 6400 ve inceleme referansları SENTETİK bağlamdır; kaynağın olguları değildir. Test bütün seyahat toplamını veya hak sahipliğini doğrulamaz.
2. [Karadeniz Teknik Üniversitesi, harcırah sunumu](https://www.ktu.edu.tr/dosyalar/sgdb_1760a.pdf), PDF sayfa 24–25, Örnek 1: eski 31 TL gündelik, 765 km, eş ve dört çocuk (5 aile ferdi). Yer değiştirme bileşeni: 620 + 1185,75 + 1240 = 3045,75 TL. Programın düşük seviyeli sürekli görev kuralına aynı sayılar verilir. Kaynaktaki 3651,75 TL TAM toplam yol ücretleri ve yol gündeliklerini de içerir; mevcut çekirdek bu tam toplamı hesaplıyor diye sunulmaz. Aile sabit unsurunda dört kişi sınırı ayrıca sınanır. Tarihsel tarife 2026 tarifesine eklenmedi; 2017 girdisinin iki çekirdekte de bloke edilmesi sınanır.

2026 tarifesi için ayrıca [SBB H Cetveli](https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf) temel kaynaktır. Üniversite rehberleri tek başına kanunun bütün uygulama koşullarının veya güncelliğinin kanıtı değildir.

## Kabul
Önceki 319 kontrol başlangıçta yeniden çalıştırıldı. Yeni modül testleri assembly sahipliğini, tek yönlü bağımlılıkları, eski API uyumluluğunu, iki dış kaynak bileşenini ve yıl engelini denetler. WPF testleri seçimsiz başlangıcı, modüle göre alanları ve kayıtların korunmasını denetler. Son çalıştırma sonuçları aşağıda tamamlanacaktır.

## Kalan sınır
Tam ve güncel 2026 sürekli görev ödeme örneğiyle uçtan uca resmî toplam mutabakatı yapılmış değildir. Kişisel verileri temizlenmiş geçici ve sürekli görev dosyaları sağlanırsa belge bazında bağımsız mutabakat eklenebilir. Çıktılar KONTROL TASLAĞI — ÖDEME BELGESİ DEĞİLDİR.

## Gerçek çalıştırma sonucu
- Komut: `& .\08_Asama8_IsAkisi_Testleri.ps1` — çıkış 0.
- Release: 0 uyarı, 0 hata.
- Aşama 2–7: 256/256; iş akışı: 32/32; PDF güvenlik: 4/4; modül ve dış kaynak: 15/15; WPF: 35/35. Toplam **342/342**. Önceki 319 kontrol korunmuştur.
- Bağımsız ek çalıştırma: `dotnet .\tests\GorevYolluklari.ModuleChecks\bin\Release\net10.0\GorevYolluklari.ModuleChecks.dll .` — 15/15, çıkış 0.
- Kural/tarife dosyalarında Git farkı yok: PreliminaryCalculation, SameDayPerDiemPreview, RelocationExpensePreview, DailyRateTariff2026, MultiDayDutyPreview2026.
- Kaynak yedeği: `%TEMP%/YollukModulAyrimi_519de0b1518349f09c3625d0c126f72d`; önceki Git HEAD ayrıca `%TEMP%/YollukModul_Oncesi_45237ce.bundle`.
- Bu değişiklikler için yeni Git commit/push veya release oluşturulmadı.

## Güncel taşınabilir paket
- Komut: `& .\scripts\Build-Portable.ps1` — çıkış 0; win-x64 self-contained tek EXE.
- Konum: `dist/GorevYolluklari_20261110_225505_c7e1289d/GorevYolluklari.exe`.
- SHA-256: `4F1D5A91257246A33A87BC16BD9EC71325FE2E361BBFC62E43480C792543273E`.
- İzole geçici veri kökü, geçersiz DOTNET_ROOT ve kapalı çoklu runtime aramasıyla başlatıldı; süreç 8 saniye boyunca çalıştı, ardından yalnız bu test süreci durduruldu. Bu gözlem temiz ikinci bilgisayar veya tam kullanıcı akışı kabulü değildir.
- Paket Veriler/Yedekler/Ciktilar klasörleri boş; mevcut masaüstü kayıtlarına dokunulmadı.
- `git diff --check` çıkış 0.
