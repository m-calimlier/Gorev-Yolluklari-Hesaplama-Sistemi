# Görev Yollukları Hesaplama Sistemi

.NET 10 / WPF ile taşınabilir Windows uygulaması. 2026 yurt içi geçici görev ve sürekli görev yer değiştirme **ön hesabı**, personel/görev kayıtları, kanıt kontrolü ve PDF **kontrol taslakları**.

**KONTROL TASLAĞI — ÖDEME BELGESİ DEĞİLDİR.** Resmî hak sahipliği, mevzuat güncelliği ve Örnek 27/28 mutabakatı tamamlanmış değildir. İmza/ödeme onayı üretilmez. 2026 dışı tarife ve belirsiz/özel statü otomatik hesaplanmaz; m.43 adayları hak gününe çevrilmez.

## Çalıştırma

Geliştirici bilgisayarında Windows ve .NET 10 SDK gerekir:

```powershell
dotnet run --project src/GorevYolluklari.App --configuration Release
```

Son kullanıcı paketi:

```powershell
& .\scripts\Build-Portable.ps1
```

`dist/` altında benzersiz `win-x64`, self-contained paket oluşur. EXE'yi yazılabilir bir klasöre koyun; Runtime, SQL, SQLite veya Office kurmayın. `Kaynaklar`, `Veriler`, `Yedekler`, `Ciktilar` EXE yanında tutulur. Veri kökü ekranda gösterilir; geliştirmede bu kök `bin/Release/...` altındadır. İleri test/taşıma için açık `GOREV_YOLLUKLARI_ROOT` ortam değişkeni kullanılabilir; normal kullanımda gerekmez.

## Kullanım

1. Personel ve Kayıtlar bölümünde personel, derece, ek gösterge ve statüyü girin. Yeni personelin statüsü belirsizdir; uygunluğu varsayılmaz.
2. Geçici veya Sürekli Görev bölümünde görev, saat, mahalliyet ve kanıt referanslarını girin. İnceleme alanlarını gerçek değerlendirmeye göre işaretleyin.
3. Ön hesapla ile gerekçe ve ham `decimal` sonucu görün. Sürekli görev sonucu yalnız yer değiştirme bileşenidir. Çok günlü hesapta ayrı incelenmiş hak günleri ve geçmiş gün defteri gerekir.
4. Belgeler/İcmaller bölümünde belgeli tam kuruş giderleri ve **bağımsız** brüt/net kontrol tutarlarını girin. İncelemeyi kaydedip güncel ön hesabı alın; yalnız taslak PDF oluşturun.
5. Ctrl+S ile kayıtları saklayın. Yedekleme bölümünden tarihli yedek oluşturun/geri yükleyin. Geri yükleme mevcut dosyayı da korur. JSON yedekleri şifreli değildir.

Veri değişiklikleri önceki ön hesabı ve tutar incelemesini geçersiz kılar. Kayıtlar şema/revizyon kontrolü ve tek yazıcı kilidiyle korunur. Bozuk JSON sessizce boş listeye dönüştürülmez.

## Hibrit tablo ve XLSX

Gider tablosunda hücre düzenleme, Tab/Enter gezinme ve Ctrl+C desteklenir. TSV yapıştırma altı sütun bekler:

```text
SATIR-01 Gündelik ÖRNEK GİDER 850,50 DEMO-BELGE 6245 m.39
```

Alanları gerçek aktarımda **sekme** karakteriyle ayırın. Türler: Gündelik, Yol, Konaklama, Aile gündeliği, Aile yol, Yer değiştirme. En fazla 40 satır; tutarlar tam kuruş olmalıdır. Mükerrer kimlik veya hatalı hücre tüm aktarımı durdurur; onay/hesap alanları aktarılmaz.

XLSX ver ile uygulamanın `YOLLUK-GIDER-V1` değer şablonunu oluşturun. XLSX al bu sürümlü şablonu okur; genel amaçlı Excel dosyalarını, formülleri ve makroları kabul etmez. Excel.exe/Interop ve ek paket gerekmez.

## Testler

```powershell
& .\08_Asama8_IsAkisi_Testleri.ps1
```

Önceki 256 kontrol aynen korunur; kayıt/aktarım/iş akışı, PDF güvenlik ve WPF kontrolleri ayrıca çalışır. WPF kontrolleri yalnız sentetik veriyi geçici klasörde kullanır, ekran görüntülerini `Ciktilar/` altına yazar. PDF görsel incelemesi için isteğe bağlı `scripts/Check-PdfLayout.py` bulunur.

Tam kaynak ön kontrolleri yerel resmî PDF'leri gerektirir. GitHub deposuna resmî PDF/XLSX/HTML kopyaları, kişisel kayıtlar, yedekler, çıktılar veya derlemeler dahil edilmez. Yeni klonda `Kaynaklar/` arşivinizi kendi yedeğinizden getirin veya katalogdaki resmî kaynakları inceleyerek indirin; dosya özeti hukuki güncellik teyidi değildir.

## Mimari ve açık kabul kapıları

- `Core`: mevcut tarife/hesap motorları ve kayıt sözleşmeleri.
- `Application`: ön hesap/taslak akışı, snapshot geçerliliği, TSV/XLSX değer aktarımı.
- `Storage`: JSON atomik yazım, revizyon, tarihli yedek ve kurtarma.
- `Documents`: PDFsharp-WPF 6.2.4 ile yalnız kontrol taslakları.
- `App`: Türkçe lacivert–altın WPF ekranları; 1024×768, 1366×768, 1920×1080 profilleri.

[Aşama 8 planı](docs/ASAMA8_INCELEME_VE_UYGULAMA_PLANI_2026-11-10.md) ve [ilerleme kaydı](docs/ASAMA8_ILERLEME.md).

Açık işler: gerçek anonim örneklerle kuruş/yuvarlama ve form mutabakatı, güncel mevzuat incelemesi, BUT-003 katalog ayrımı, temiz ikinci Windows bilgisayarında çevrimdışı kabul ve gerçek yüksek DPI/klavye kullanıcı testi. Bu kapılar kapanmadan resmî ödeme kullanımına hazır sayılmaz.

Ek Ders Hakediş Sistemi ve DoğruFatura'dan yalnız genel tasarım/iş akışı örüntüleri esin alındı; kaynak kodu veya asset kopyalanmadı. Bu depoda açık kaynak lisansı verilmiş olduğu varsayılmamalıdır; üçüncü taraf paket lisansları ayrıca geçerlidir.


Geçici ve sürekli görev hesapları bağımsız çekirdek projelerindedir. Başlangıçta modül seçilir ve ilgili veri alanları açılır. 12.11.2026 doğrulamasında 396/396 kontrol geçti. WPF Release derlemesi 0 hata ve 0 uyarıyla tamamlandı. Kaynak örneklerinin kapsamı [modül ayrımı raporunda](docs/MODUL_AYRIMI_VE_ORNEK_DOGRULAMA_2026-11-10.md) açıklanır.
