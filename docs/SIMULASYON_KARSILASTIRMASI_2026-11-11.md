# Görev yolluğu simülasyonları

11 Kasım 2026. Kaynak, kullanıcının ilettiği Desktop.rar içindeki dört YOHEP v0.2a giriş ve sonuç çifti. Görüntüler geçici klasörde incelendi. Sayısal değerler uygulamanın WPF ekranında çağrılan TravelWorkflowService ile çalıştırıldı.

| Örnek | Gündelik, referans ve program | Karşılaştırılan kalem | YOHEP | Program | YOHEP brüt / net |
|---|---:|---|---:|---:|---:|
| 1 — Sürekli | 850,00 TL | Yer değiştirme | 37.527,50 TL | 37.527,50 TL | 42.227,50 / 41.906,99 TL |
| 2 — Sürekli | 870,00 TL | Yer değiştirme, yarım değişken unsur | 46.566,75 TL | Eş durumu incelemesi | 55.176,75 / 54.757,96 TL |
| 3 — Geçici | 880,00 TL | Görev gündeliği ve konaklama | 4.576,00 TL | 4.576,00 TL | 7.105,99 / 7.052,06 TL |
| 4 — Geçici | 890,00 TL | Görev gündeliği ve konaklama | 2.314,00 TL | 2.314,00 TL | 5.357,34 / 5.316,68 TL |

- Örnek 1. 17.000,00 TL kişi sabit, 8.500,00 TL aile sabit ve 12.027,50 TL mesafe. Referansın brütüne ayrıca 1.700,00 TL yol gündeliği ve 3.000,00 TL taşıt ücreti giriyor.
- Örnek 2. Ekrandaki yarım değişken unsur 11.766,75 TL. Buna 34.800,00 TL sabit unsur eklenince 46.566,75 TL çıkıyor. Bu seçenek testte eş nakli inceleme senaryosu olarak eşlendi. Program hak sahipliğini veya yarım unsur uygulamasını otomatik karara bağlamıyor.
- Örnek 3. İki görev günü 1.760,00 TL ve iki gece 2.816,00 TL. Ekrandaki 879,99 TL yol gündeliği, her 1/3 birim ayrı ayrı 293,33 TL'ye yuvarlandığında elde ediliyor. Üç birim ham toplandığında 880,00 TL.
- Örnek 4. Bir görev günü 890,00 TL ve bir gece 1.424,00 TL. Ekrandaki 593,34 TL yol gündeliği, iki 1/3 birim ayrı ayrı 296,67 TL'ye yuvarlandığında elde ediliyor. Ham toplam son aşamada kuruşa yuvarlandığında 593,33 TL. Bu iki gözlem YOHEP ekranlarıyla aritmetik eşleşmeyi gösterir.

Tam brüt ve net sütunu referans ekranından aktarılmıştır. Geçici görev yol gündeliği, yol ücretleri, diğer giderler ve damga vergisi bu çalıştırmanın otomatik hesap kapsamı dışındadır. Yuvarlama veya vergi kuralı değişikliği yapılmadı.

Görüntüler görev tarihlerini, saatlerini, geçmiş gün defterini ve inceleme kanıtlarını içermiyor. Test yalnız bileşenleri yalıtmak için Haziran 2026 tarihleri, ilk dönem konaklama sırası, sıfır önceki gün ve sentetik inceleme referansları kullandı. Ek gösterge aralıkları sırasıyla 0, 3600, 6400 ve 8000 ile temsil edildi. İnceleme işareti kaldırıldığında dört örneğin de tutar üretmediği doğrulandı. Kişisel kayıt oluşturulmadı. Resmî form ve ödeme mutabakatı açık.
