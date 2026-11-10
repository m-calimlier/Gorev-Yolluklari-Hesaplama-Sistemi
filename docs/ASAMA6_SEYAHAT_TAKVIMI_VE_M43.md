# Aşama 6 — Seyahat/görev zaman çizelgesi ve m.43 aday süre hesabı

**Durum:** Windows üzerinde kullanıcı testi bekliyor. Eski dosyalar değiştirilmeden yeni bir C# zaman çizelgesi sınıfı ve testler eklenmiştir. **Bordro ve kesin ödeme hesabı oluşturulmaz.**

## Kapsam

- **Günübirlik görev:** Gerçek gidiş ve dönüş saatleri doğrulanır. Aynı takvim günü içinde kalıyorsa önceki Aşama 4 `HarcirahPreviewEngine.PreviewSameDay` ile **6245 m.39** ön hesaplamasına yönlendirilir. m.43 süre birimleri günübirlik ödemeye ayrıca eklenmez.
- **Çok günlü görev:** Dört farklı tarih-saat çiftinin mantığı ayrıştırılır: taşıt hareket/varış (gidiş), fiilî görev başlangıç/bitiş ve taşıt hareket/varış (dönüş). Görev günleri, seyahat günleri ve çakışan takvim günleri ayrı **aday listeler** olarak gösterilir. Aynı takvim gününü iki kez saymak yasaktır.
- **6245 m.43 aday süreleri:** Taşıt hareketinden varışa kadar 24 saatlik tam bölümler + kalan kesrin yukarı tamamlanmasıyla **gidiş ve dönüş ayrı ayrı** aday seyahat süre birimi üretilir. Bu birimler doğrudan tahakkuk, m.42 gün sırası veya 33(d) konaklama gün sayısı değildir. Fiilî olaylar ayrıca değerlendirilir.
- **Hukuki bloke:** Yetkili incelemesi, personel yılı, görev mahalli, bilet/olur/saat kanıtı, saat dilimi ve kronolojik tutarlılık olmadan tutar hesaplanmaz. Çok günlü görevde bu aşamada **tutar hesaplanmaz**; Aşama 5'in onaylanmış hak günleri zorunluluğu korunur.

## Kaynaklar

1. 6245 sayılı Kanun m.39 — memuriyet mahalli dışı günübirlik saatlere göre 1/3, 2/3 veya gecelik durumda tam gündelik. Resmî nüsha: `Kaynaklar/01_Mevzuat/6245_Harcirah_Kanunu.pdf` (SHA-256, 3. aşamada kullanıcı bilgisayarında alınmıştır).
2. 6245 sayılı Kanun m.42 — aynı şahıs/yer/iş için 90/180 gün sınırı, belirli istisnalar.
3. 6245 sayılı Kanun m.43 — seyahat edilen taşıtın hareketinden varışına her 24 saat veya kesri, süre hesabında gün sayılır.
4. [TBB — Harcırah Kanunu ve uygulama açıklamaları](https://www.tbb.gov.tr/sites/default/files/online/kitaplar/aciklamali_ve_ictihatli_harcirah_kanunu/files/basic-html/page197.html); [6245 konsolide metin](https://www.lexpera.com.tr/mevzuat/kanunlar/harcirah-kanunu-6245); [Resmî mevzuat PDF'si](https://www.mevzuat.gov.tr/MevzuatMetin/1.3.6245.pdf).

## Özellikle çözümlenmemiş mevzuat konuları

- 39. maddede **gecenin geçirilmesi** kriterinin uygulanması, günübirlik olmayan durumlar; farklı görev yerlerine gidilmesi, hafta sonu/izin/rapor günleri.
- Bir yıllık m.42 dönemi, önceki görevlendirme günleri ve m.42 istisnalarının gerçek personel kayıtlarıyla kontrolü.
- m.43 yolculuk süresi ile m.39 gündelik günlerinin birlikte uygulanma sınırları; uçak/otobüs aktarmaları, beklemeler ve seyahat günü çakışmaları.
- Yerel saatte yaz/kış saati veya saat dilimi geçişi şüphesi, kanıtı belirsiz ulaşım saatleri.
- Saatlere ilişkin kanıtları gerçek görev oluru ve belgeli örneklerle doğrulama.
- Yasal yuvarlama, yol giderleri, avans, vergi durumu ve diğer istisnalar.

## Mimarinin korunması

`src/GorevYolluklari.Core/Integration/TravelTimelinePreview2026.cs` yeni bir sınıftır. Eski hesaplama sınıfları, şablonlar, JSON/Excel verileri ve `Veriler` / `Yedekler` / `Kaynaklar` değiştirilmez.

Kayıt yapılması gerekirse sonraki aşamada dosya tabanlı JSON kullanılacaktır. Harici veritabanı, Office veya servis gerekmeyecek.

## Çalıştırma

```powershell
$zip = "$env:USERPROFILE\Downloads\GorevYolluklari_Asama6_Seyahat_Takvimi.zip"
Expand-Archive -LiteralPath $zip -DestinationPath "$env:USERPROFILE\Desktop" -Force
& "$env:USERPROFILE\Desktop\Görev Yollukları Hesaplama Sistemi\06_Asama6_Seyahat_Takvimi_Testleri.ps1"
```

Başarılı testler yeni modül ve Aşama 2–5 kontrollerini birlikte çalıştırır. Test başarısı **mevzuat açısından nihai doğruluk ispatı değildir**; gerçek bordro örnekleriyle kuruş düzeyinde mutabakat olmadan resmî çıktı açılmayacaktır.

## Sonraki sohbet için başlangıç

> Görev Yollukları Hesaplama Sistemi projemize 7. aşamadan devam edelim. 1–6. aşama C# ön hesap testlerinin Windows çıktılarını kontrol ederek başlayalım. Proje `C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi` dizininde. Harici SQL/SQLite kullanma; yalnız taşınabilir WPF/.NET 10, JSON kayıt, gerekirse hibrit Excel, lacivert-altın arayüz, resmî PDF örnek 27/28. 6. aşamanın zaman çizelgesini m.42/43 hak günlerine **otomatik bağlama**; önce gerçek olurlarla doğrula. `docs/ASAMA6_SEYAHAT_TAKVIMI_VE_M43.md` dosyasındaki sınırlamaları koru.
