# Aşama 5 — Çok günlü geçici görev ve konaklama (2026)

**Durum:** Uygulama içi hukuki/ödeme onayı DEĞİL; kontrollü matematiksel ön hesap. Aşama 4 tarife motoru ve 2–3–4 regresyon testleri korunur.

## Mevzuat çerçevesi

- **6245 m.42(a):** Yurt içinde bir yıllık dönemde aynı şahıs, aynı yer, aynı iş için ilk 90 gün tam, takip eden 90 gün 2/3; 180 günün üstünde gündelik yoktur. Aralar sınırı sıfırlamaz. Kanunda m.42 için *istisnalar* bulunur, bu sürüm bunları otomatik sınıflandırmaz.
- **6245 m.33(d):** Uygun statüde ve gündelik hak eden personelin yurt içindeki belgelendirilmiş yatacak yer gideri, belge bedeli aşılmaksızın uygun üst sınıra kadar ayrıca değerlendirilir. m.33(b) özel rejimine bu ön hesap uygulanmaz.
- **2026 H Cetveli I numaralı bölüm dipnot (1):** m.33(d) kapsamındaki konaklama için ilk 10 gün **gündeliğin %160'ı**; takip eden 80 gün **gündeliğin %50'si**; sonraki 90 gün **müstehak gündeliğin %40'ı**. İlk 90 günden sonra m.42'nin 2/3'ü devreye girdiğinde son grubun hesabı da buna göre azalır.
- **6245 m.39 ve m.43:** Günübirlik seyahatler, seyahat günleri, varış/ayrılış ve geceleme kriterlerinin hak kazanılan gün listesine etkisi ayrı yorum gerektirir; otomatik takvim sayacı yazılmadı.

## Resmî referanslar

1. `Kaynaklar/01_Mevzuat/6245_Harcirah_Kanunu.pdf` — resmî kanun PDF'si (kullanıcının aşama 3 SHA-256 kontrolünden geçti; hukuki güncellik içerik bakımından ayrıca incelenmeli).
2. `Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli.pdf` — 2026 H cetveli, I-B ve dipnot (1).
3. https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf — resmî bütçe belgesi.
4. https://www.mevzuat.gov.tr/MevzuatMetin/1.3.6245.pdf — resmî konsolide kanun adresi.
5. [Ticaret Bakanlığı 100 Soruda Harcırah Rehberi](https://denetim.ticaret.gov.tr/data/63da6bb713b876d06090b3f0/100%20Soruda%20Harc%C4%B1rah%20Rehberi.pdf) — uygulama açıklaması; eskimiş oranlar kaynak alınmamalıdır.

## Mimarinin sınırları

- Yeni `MultiDayDutyPreviewEngine2026`, **eski** `SameDayPerDiemPreview`, `RelocationExpensePreview`, `DailyRateTariff2026`, `HarcirahPreviewEngine` sınıflarını değiştirmez.
- Yalnız 2026, yurt içi ve 2026 H Cetveli I-B genel memur/hizmetli grubunda çalışır.
- Gün listesi *hak kazanılan ve m.43 açısından incelenmiş tarihler* olarak sunulmalıdır. Takvimdeki tüm günlerin otomatik gündeliğe hak kazandığını varsaymak HATALIDIR.
- Önceki bir yıllık döneme ait aynı kişi/yer/iş görev günleri harici bir onaylı kayıtla tespit edilmeden hesap olmaz. Hangi bir yıllık dönemin esas alınacağı hukuki yorum gerektirir; kod kendi başına bir yıllık dönem başlangıcı belirlemez.
- Başka bir görev/ara dönem nedeniyle 33(d) konaklama gün sırası ile m.42 gün sırası farklılaşıyorsa sistem **manuel incelemeye bloke eder**; konaklamanın ilk 10 gününü yeniden açmaz. Bu ihtiyatlı sınırlama, farklı görevlendirmeler için tüm hukukî olasılıkları çözmez.
- Her incelenmiş tarihe en fazla bir konaklama bedeli bağlanabilir. Toplu faturalar gece bazında ayrıştırılmalıdır. Gelen/geceleyen gün uyuşmazlığı, farklı personele ait fatura, arazi/tüzel kurum konaklamaları, m.33(b) rejimi, kamu misafirhaneleri, ücretsiz yatacak yer, m.41 indirimleri ve özel statüler daha sonraki kurallarda incelenecektir.
- Doğrulanmamış faturadan konaklama hesabı çıkarılmaz. Yol masrafı, avans, aile ve kesin belge hesapları bu aşamada kapsam dışıdır.
- Gündelik ve konaklama toplamlarında **kuruş yuvarlaması yapılmaz**. Sonraki belge üretim aşamasında mevzuata uygun yuvarlama ve karşılaştırma belirlenecektir.
- Çıktıda `IsOfficialPaymentDocument=false` kalır.

## Örnekler (850 TL genel memur gündeliği)

| Hak kazanılan gün sıra no | Gündelik ön hesap | Konaklama üst sınırı (33/d) |
|--:|--:|--:|
| 1 | 850 | 1.360 |
| 10 | 850 | 1.360 |
| 11 | 850 | 425 |
| 90 | 850 | 425 |
| 91 | 850 × 2/3 | (850 × 2/3) × 40% ≈ 226,67 |
| 180 | 850 × 2/3 | (850 × 2/3) × 40% ≈ 226,67 |
| 181 | m.42 gündeliği yok | ön hesap bloğu; diğer özel durumlar incelenir |

**Örnekler tavan tutardır:** Gece faturası daha düşükse fatura bedeli esas alınır. Yuvarlanmış gösterim ile dahili ham değer farklıdır.

## Sonraki işler

1. m.43 seyahat günü süresi, mesafe/gidiş-dönüş ve m.39 günübirlik ayrımını uçtan uca ele alma.
2. Aynı kişi/yer/iş kapsamındaki dönem eşleştirme, geçiş ve geçmiş gün veri modeli (JSON, kuruma ait gizli bilgi korunarak).
3. Aile/yer değiştirme, m.41, istisna, emeklilik, özel statü ve 2027 tarifesi.
4. Fiilî görevlendirme ve fatura örnekleriyle birebir mutabakat; yuvarlama politikasının sabitlenmesi.
5. Yalnız doğrulanmış sonuçlarla Örnek 27/28 resmi bildirim şablonlarının PDF çalışmaları.

## Test ve kurulum

```
Expand-Archive -LiteralPath "$env:USERPROFILE\Downloads\GorevYolluklari_Asama5_CokGunlu_Konaklama.zip" -DestinationPath "$env:USERPROFILE\Desktop" -Force
& "$env:USERPROFILE\Desktop\Görev Yollukları Hesaplama Sistemi\05_Asama5_CokGunlu_Konaklama_Testleri.ps1"
```

Son kullanıcı bilgisayarında `dotnet run` çıktısı başarılı olduğunda önceki 76 kontrol ve bu aşamanın testleri tekrar çalışmış olur. Bu paket bu üretim ortamında Windows/.NET üzerinde derlenememiştir; test sonucunu kullanıcı sistemi belirler.
