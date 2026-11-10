# Aşama 4 — Tarife + görev türü ön hesaplarının entegrasyonu

**Tarih:** 09.11.2026  
**Durum:** Kod hazırlandı. Windows üzerinde derleme ve yeni testler, kullanıcının yerel PowerShell çıktısı gelene kadar **doğrulanmış sayılmaz**.

## Amaç

Aşama 3'teki `DailyRateTariff2026` sınıfı, Aşama 2'deki `SameDayPerDiemPreview` ve `RelocationExpensePreview` sınıflarıyla yeni `HarcirahPreviewEngine` aracılığıyla birleştirilir. Önceki sınıflara, mevcut JSON'lara, WPF ekranlarına ve kullanıcı verilerine dokunulmaz.

## Girdiler ve güvenlik

- 2026 yılı **H Cetveli I-B genel memur/hizmetli** kapsamı dışında gündelik otomatik seçilmez.
- Günübirlik geçici görev: görev tarihi, ayrılış/dönüş yerel saati, kurum incelemesi, memuriyet mahalli durumu ve dayanak kaydı gerekir. Tarih uyuşmazlığı, geceleme, sınır saatleri ve belirsizlikler inceleme gerektirir.
- Sürekli görev: görev tarihi, tarifeyi belirleyen personel bilgisi, teyit edilmiş aile ferdi sayısı, mesafe kaynağı ve hak sahipliği incelemesi gerekir. Eşin aynı güzergâhta memur olarak nakli durumu otomatik sonuçlandırılmaz.
- Tutarlar `decimal` ile **yuvarlanmamış ön hesap** olarak taşınır. Resmî bordro, kesinti/avans, yol/konaklama ödemeleri ve PDF üretimi yoktur.
- Testlerde kullanılan örnek kişi, görev oluru ve kilometre değerleri **tamamı sentetiktir**; gerçek ödeme örneği değildir.
- Bu modüller herhangi bir hukuki hak sahipliği onayı **vermez**. İnceleme işaretlerinin doğru beyanından operatör sorumludur.

## Testler

`04_Asama4_Birlesik_OnHesap_Testleri.ps1` önce **Aşama 4 entegrasyon** kontrollerini, ardından Aşama 2'deki 11 testi ve Aşama 3'teki 23 testi yeniden çalıştırır. Hata varsa ilk başarısız noktada durur. Hiçbir veri kaydı veya PDF üretmez.

## Sıradaki geliştirme

1. Resmî 6245 PDF'sinde güncel 2026 metin kontrolünün hukukî madde karşılaştırmasını belgelendir.
2. 6245 m.39'un sınır saatleri yorumunu, MEB uygulamalarını ve örnekleri karşılaştırarak netleştir.
3. Gecelemeli/çok günlü geçici görev, 90/180 gün, konaklama sınırları ve yol kalemlerini ayrı doğrulanmış modüller olarak ekle.
4. m.45 eş nakli/yer değiştirme, aile hak sahipliği ve ücretli yolculuk ayrıntılarını doğrula.
5. KGM XLSX'den il/ilçe mesafeleri için bağımsız, sürümlü, çevrimdışı veri çıkartıcısı oluştur; kullanıcıya resmî kaynak/kilometre göster.
6. Örnek 27/28 belgelerini ve PDF'leri gerçek örneklerle doğruladıktan sonra aç.
7. JSON kayıt sürümü, güvenli yedek, kişisel veri koruma ve çevrimdışı WPF giriş ekranlarını ekle.

## Kaynaklar

- Resmî Kanun: `Kaynaklar/01_Mevzuat/6245_Harcirah_Kanunu.pdf`
- 2026 H Cetveli PDF: `Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli.pdf`
- Aşama 3 doğrulanmış tarife verisi: `Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli_Dogrulanmis_OnHesap.json`
- 6245 m.39, m.45; 2026 merkezi yönetim bütçe kanunu H Cetveli.

**Önemli:** `BUT-003` Kaynak Kataloğundaki toplu sözleşme/Hakem Kurulu Kararı adlandırması henüz düzeltilmedi; bu belgeye dayanan otomatik hesap modülü bulunmuyor.
