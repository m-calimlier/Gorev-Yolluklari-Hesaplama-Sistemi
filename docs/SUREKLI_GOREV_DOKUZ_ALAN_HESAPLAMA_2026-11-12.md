# Sürekli Görev Yolluğu — Referans 9 Alanlı Hesaplayıcı

Kaynak arayüz: Kullanıcının sağladığı YOHEP v0.2a ekran görüntüsü (12.11.2026).
Uygulama dalı: `sadelestirme-bildirim-27-28-2026`.
Ana hesaplama altyapısı: 2026 H Cetveli I-B genel memur/hizmetli; 6245 sayılı
Harcırah Kanunu m.10, m.27, m.43, m.44, m.45, m.47.

## Referans 9 alan ile ekran eşleşmesi

1. **Bütçe Dönemi:** 2026 sabit bilgi. H Cetveli başka yıl için mevcut değil.
2. **Aylık Kadro Derecesi:** 1–15; `PersonnelRecord.Grade`.
3. **Ek Gösterge Grubu:** 8000+, 6400–7999, 3600–6399, 3600 altı.
   Seçim 8000/6400/3600/0 olarak saklanır. Eski sayısal kayıt gösterimi
   kategoriye çevrilir; kullanıcı değiştirmedikçe eski gerçek sayı korunur.
4. **Y.D.M. Değişken Unsuru:** Normal tayin (%100) veya aynı eski/yeni
   görev mahallerinde atanan ikinci memur eş (%50). Yalnız m.45(c)
   kilometre unsuru yarıya iner; sabit kişi/aile unsuruna dokunulmaz.
5. **Aile Ferdi Sayısı (kendiniz hariç):** m.45 sabit unsurunda 4 kişi tavan.
6. **Eski Görev Yeri:** il + ilçe.
7. **Yeni Görev Yeri:** il + ilçe.
8. **Mesafe (km):** elle giriş veya KGM sorgusu.
9. **Taşıt Ücreti (1 kişi için TL, varsa):** `DutyRecord.TravelFarePerPerson`.
   Eski JSON kayıtlarında yoksa `null`; geriye uyumlu.

## Kaldırılan gereksiz görünen girişler

- Temel Sürekli Görev kartlarında ad-soyad, unvan, kurum, personel statüsü,
  atama tarihi, bağımsız memuriyet mahalli sınıflandırması, ayrı eş-onayı,
  gider ekleme, kanıt/inceleme kutuları gösterilmiyor.
- 2026 genel memur/hizmetli tarife grubu varsayılıyor. **Özel/üst görev statüleri
  bu dokuz alanlı hesaplayıcının kapsamında değildir.**
- Kaydedilmiş eski personel adı varsa korunur; yeni hesapta ad-soyad
  yoksa `AD SOYAD BELİRTİLMEDİ` olarak açık yazılır.
- Tarih sorulmadığından hesap motoru için 01.01.2026 tarife referansı kullanılır.
  **PDF'de bu tarih gerçek atama tarihi olarak gösterilmez.**
- Aynı il/ilçe seçilirse yer değiştirme hesabı yapılamaz. Farklı il/ilçe
  seçimi dış mahal varsayımıdır; fiilî memuriyet mahalli sınırları sadece
  il/ilçe bilgisiyle kesin doğrulanamaz.

## Hesaplanan tutarlar ve açık varsayımlar

- m.45: kişi sabit unsuru = 20 x gündelik,
  aile sabit unsuru = min(aile sayısı,4) x 10 x gündelik,
  mesafe unsuru = kilometre x %5 x gündelik (ikinci memur eş için %50).
- **Hesaplama varsayımı:** Personel ve seçilen aile fertlerinin 1 gün
  seyahat ettiği kabul edilip birer gündelik eklenir (m.43–44).
- **Taşıt varsayımı:** Tek kişilik ücret bütün seçilen aile fertleri
  için de eşit kabul edilir; personel + aile sayısı ile çarpılır.
- Bu iki varsayım her gerçek görevde geçerli olmayabilir. Farklı yolculuk
  günleri, çocuk/indirimli ücret, eş hakkı veya özel vasıta koşullarında
  hesap resmî ödemeye dönüştürülmemelidir. Açıklama hem sonuç kartında hem
  PDF dipnotunda yer alır.
- Kayıt anında otomatik gündelik, aile gündeliği, kişi/aile taşıt ve
  yer değiştirme satırları oluşturulur. Eski manuel satırlar silinmez
  ve mevcut türde tekrarlı otomatik satır açılmaz.
- Damga vergisi hesaplama PDF'sinin genel ödeme özetinde korunur.
- PDF **hesaplama çıktısıdır**, resmî Örnek 28 onay formu değildir.

## Gözden geçirilen hukukî dayanaklar

- MEB Strateji Geliştirme Başkanlığı 03.10.2017 ve 18.08.2023
  eş durumu görüş yazısı:
  https://sgb.meb.gov.tr/gorusler/surekligorevesdurumu.pdf
- 6245 sayılı Harcırah Kanunu m.45.
- Sadece yer değiştirme gideri değil, seyahat ücretlerinin de
  harcırahın unsuru olduğunu belirten m.10 ve m.44.

## Yerel doğrulama bekliyor

Bu kodlar GitHub'a kaydedildi; **Windows üzerinde yeni sürüm için derleme
ve WPF UI/PDF testleri henüz çalıştırılmadı**.

```bat
dotnet build .\GorevYolluklari.slnx -c Release
dotnet run --project .\tests\GorevYolluklari.RuleChecks -c Release
dotnet run --project .\tests\GorevYolluklari.UiChecks -c Release -- "%CD%" "%TEMP%\Yolluk_UiKontrol"
```

`main` dalına merge edilmedi. Özel/üst memuriyet,
kişiye göre indirimli aile bileti veya birden fazla seyahat günü gibi
özel durumlar bu sade ön hesap modelinin dışında kalır.
