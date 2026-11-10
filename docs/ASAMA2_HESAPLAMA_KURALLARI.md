# Aşama 2 — Hesaplama kuralları, istisnalar, doğrulama

Tarih: 09.11.2026. Durum: **ön hesap kodları eklendi, resmî hesaplama onayı yok.**

## Kullanılan kaynaklar

- 6245 sayılı Harcırah Kanunu md.3, 10, 39, 42, 43, 44, 45, 47. Yetkili kanun metni: https://www.mevzuat.gov.tr/MevzuatMetin/1.3.6245.pdf
- Yardımcı güncel metin (resmî asıl yerine geçmez): https://www.hukuksalegitim.com/mevzuat/6245-sayili-harcirah-kanunu
- MEB mali görüşler: https://sgb.meb.gov.tr/gorusler/gorusler.html
- 2026 H Cetveli: https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf
- KGM: https://www.kgm.gov.tr/Sayfalar/KGM/SiteTr/Root/Uzakliklar.aspx

## Güvenlik kapısı — her hesaplamadan önce

1. Personelin statüsü ve ilgili harcırah hakkı kanun ve toplu sözleşme hükümlerine göre incelenir.
2. Görev türü ve memuriyet mahalli içi/dışı sınıflandırması gerekçeli olarak kaydedilir. Sadece ilçe farklılığı otomatik kanıt değildir.
3. Tarife yılı, gündelik tutarı ve yürürlük dönemi yetkili kaynakla eşleştirilir.
4. KGM mesafesi, aile hak sahipliği, eş görevlendirmesi ve diğer istisnalar uygunluk kontrolünden geçer.
5. Sadece 'onaylı' kayıtlar ödeme/Örnek-27/Örnek-28 çıktısına geçer. Bu aşamadaki kod hiçbir resmî belge üretmez.

## Uygulananlar (şimdilik matematiksel ön hesap)

- `SameDayPerDiemPreview`: m.39; geceleme olmayan aynı gün gündelik ön hesabında 13:00/19:00 aralıklarını esas alır.
- Sınır saatlerine tam eşitlik, belirsiz mahalliyet, geceleme veya tarife referansı eksikliği manuel inceleme/istisna sonucuna gider.
- `RelocationExpensePreview`: m.45; 20 × gündelik, hak sahibi aile ferdi başına 10 × gündelik (en çok 40 ×), kilometre başına %5 × gündelik. Yalnızca bu üç kalemin toplamıdır; **tam sürekli görev yolluğu değildir**.
- Eşin aynı güzergâhta memur olarak atanması / aile hak sahipliği belirsizliği otomatik toplamayı durdurur.
- `decimal` hesap kullanılır; resmî yuvarlama adımı henüz belirlenmediği için `UnroundedAmount` yalnız önizlemedir.
- Testte geçen `900` ve `1000` gündelik değerleri tamamen sentetiktir, 2026 tarifesi değildir.

## Daha sonra kanun/şablon bazında doğrulanacak

- md.42: ilk 90 gün tam, 91–180 için 2/3; bir yıllık dönem, aynı yer/aynı iş/aynı kişi; istisna personel grupları.
- md.43: yol günleri 24 saat ve küsurat hesaplama.
- md.33(b): özel statüler; konaklama giderleri, belge şartları ve azami sınırlar.
- md.44–47: aile yol/gündelik, resmî mesafe, memur eş ve diğer farklılaşan durumlar.
- 2026 H Cetveli, 8. dönem toplu sözleşme/hakem kararı ve MEB görüşleri: tarife/özel statü ve hak sahipliği.
- Harcama Belgeleri Yönetmeliği: Örnek 27/28 ve destekleyici belgeler.
- Bordroya esas vergiler, varsa kesintiler, avans mahsubu ve hesap kalemi bazında kuruş yuvarlama.

## Doğrulama ilkesi

`dotnet run --project tests/GorevYolluklari.RuleChecks/GorevYolluklari.RuleChecks.csproj --configuration Release`
komutu, NuGet paketine ihtiyaç duymayan 11 kontrol çalıştırır. Bunlar **hukuki doğruluk testi değil, yazılan ön hesap algoritmasının regresyon testidir**. Gerçek kuruma ait kimlik bilgileri gizlenmiş resmî örneklerle mutabakat sonraki aşamada yapılır.
