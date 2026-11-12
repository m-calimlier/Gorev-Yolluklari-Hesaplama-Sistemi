# Örnek 27/28 odaklı sadeleştirme — 12.11.2026

## Amaç
Mevcut WPF arayüzünün lacivert–altın tasarımını, logosunu, sayfa ve kart stilini koruyup kullanıcının yalnız yolluk bildiriminde yer alacak bilgileri girmesini sağlamak.

**Belge türleri:**
- **Örnek 27:** Yurtiçi/yurtdışı geçici görev yolluğu bildirimi (bu uygulamada yalnız 2026 yurtiçi).
- **Örnek 28:** Yurtiçi sürekli görev yolluğu bildirimi.

Bunlar ödeme bordrosunun / onaylı muhasebe belgesinin kendisi değildir. Sürümde üretilen PDF'ler hâlen **KONTROL TASLAĞI** niteliğindedir.

## Kullanıcıya görünen düzenleme
- Ana menüde **Veri Girişi**, **Hesapla** ve **Yedekleme / Ayarlar** görünür. Önceden kaydedilmiş veriler, eski sayfa indeksleri ve hesap motorları silinmedi.
- Geçici görev giderlerinde: **Gündeliği giderlere aktar**, **Taşıt gideri ekle**, **Konaklama gideri ekle**.
- Sürekli görev giderlerinde: **Personel gündeliği**, **Personel taşıtı** ve aile ferdi varsa **Aile gündeliği**, **Aile taşıtı**. Hesaplanan **yer değiştirme** mevcut ayrı düğmeden aktarılmaya devam eder.
- Tek günlük geçici görevde çok günlük seyahat/hak günü kartı gösterilmez. Birden fazla gün seçildiğinde mevcut inceleme alanı görünür.
- Toplu TSV/XLSX düğmeleri normal gider ekranından kaldırıldı; alttaki veri işleme kodları eski dosyaların uyumu ve testler için korunur.
- Her bir gider için tutar ve belge dayanağı esas alınır. Form doğrulama, kanıt ve hak sahipliği incelemeleri atlanmaz.
- Gider toplamı hazır olduğunda **brüt + 2026 damga vergisi + avans + tahmini net** birlikte gösterilir. Taslak PDF alt satırında da damga ve tahmini ödeme vardır.

## Kullanıcıdan gerçek bildirimlerin aritmetik karşılaştırması
Kişisel ad, kimlik numarası, unvan, güzergâh veya asıl PDF'ler depoya kopyalanmamıştır.

| Belge | Gündelik (TL) | Taşıt (TL) | Yer değiştirme (TL) | Brüt (TL) | Damga (TL) | Net (TL) |
|---|---:|---:|---:|---:|---:|---:|
| Sürekli 1 | 860,00 | 1.020,00 | 21.672,00 | 23.552,00 | 178,76 | 23.373,24 |
| Sürekli 2 | 1.720,00 | 500,00 | 28.595,00 | 30.815,00 | 233,89 | 30.581,11 |
| Geçici 1 | 0,00 | 1.000,00 | 0,00 | 1.000,00 | Belgede yok | Belgede yok |
| Geçici 2 | 286,67 | 940,00 | 0,00 | 1.226,67 | Belgede yok | Belgede yok |
| Geçici 3 | 286,67 | 1.000,00 | 0,00 | 1.286,67 | Belgede yok | Belgede yok |

Not: Sürekli 1 yer değiştirme 17.200,00 sabit + 4.472,00 değişken; Sürekli 2 yer değiştirme 25.800,00 sabit + 2.795,00 değişken. Tablodaki geçici görev belgelerinde damga/net alanı gösterilmediği için bunlar referans belgeden doğrulanmadı.

2026 damga hesabı = `ROUND(brüt x 0,00759; 2)`. Örnek 1: 23.552,00 x 0,00759 = 178,76 TL. Örnek 2: 30.815,00 x 0,00759 = 233,89 TL. Yasal oran: 488 sayılı Kanun, (1) sayılı tablo IV/1-b.
Kaynaklar:
- https://www.pwc.com.tr/tr/hizmetlerimiz/vergi/vergi-portali/pratik-bilgiler/oranlar/damga-vergisi-oran-ve-tutarlari.html
- https://sgdb.isparta.edu.tr/assets/uploads/sites/95/files/2026-yilinda-gecerli-katsayilar-oranlar-ve-tutarlar-guncelleme-tarihi07012026-07012026.pdf

2026 H cetveli genel memur gündelikleri 850 / 860 / 870 / 880 / 890 TL: https://hesapla.dev/veri/harcirah-gundelikleri (doğrulanmış 2026 H cetveli özeti).

## Bu değişiklikle henüz tamamlanmayan noktalar
1. Görev başlangıç/bitiş saatinden m.39 gündelik hak sahipliği, m.42 süre sınırları ve özel statü incelemeleri otomatik nihai karara bağlanmaz.
2. Sürekli görevde aile fertlerinin ad, soyad, yakınlık ve her birinin ayrı bilet/gündelik satırları için model henüz mevcut değildir. Personel ve aile gündelikleri kullanıcı tarafından belge üzerinden kontrol edilip eklenir.
3. Örnek 27'nin gün bazlı seyahat satırları ve Örnek 28'in aile ferdi satırları bire bir resmî form formatında PDF'ye dökülmez. PDF görünümü kontrol taslağıdır.
4. PDF'nin iç veri sözleşmesindeki `NetTry` hâlen brüt-avanstır. Damga dahil net tutar bilgilendirme amaçlı ayrı hesaplanır; muhasebe mutabakatı için veri sözleşmesi ayrıca güncellenmelidir.
5. Bu çalışma dalı Windows/.NET 10 WPF derleme ortamında henüz çalıştırılmadı. Derleme, ekran davranışı ve gerçek belge kabul testi kullanıcı/CI ortamında tamamlanmalıdır.

## Windows üzerinde kontrol
```powershell
git fetch origin
git switch sadelestirme-bildirim-27-28-2026
dotnet build .\GorevYolluklari.slnx --configuration Release
dotnet run --project .\tests\GorevYolluklari.RuleChecks --configuration Release
```
Ana dalı değiştirmeden önce görsel kontrolü ve tam regresyon testini tamamlayın.
