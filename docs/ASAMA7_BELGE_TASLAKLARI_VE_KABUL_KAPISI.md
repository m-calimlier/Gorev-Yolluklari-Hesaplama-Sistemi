# Aşama 7 — Örnek 27 / Örnek 28 belge taslakları, PDF ve kontrol icmali

**Tarih:** 10.11.2026. **Durum:** Kullanıcının Windows/.NET test sonucu bekleniyor. Resmî onaylı bordro veya harcırah tahakkuku DEĞİLDİR.

## Mevcut durum

- Aşama 2: 11/11 test kullanıcı tarafından başarılı bildirildi.
- Aşama 3: 23/23 test, kullanıcı bilgisayarında resmî 6245 PDF ve 2026 H Cetveli dosya imzası / SHA-256 kontrolü başarılı bildirildi.
- Aşama 4: 42/42 yeni test bildirildi; eski testlerle 76/76.
- Aşama 5: 48/48 yeni test; toplam 124/124 bildirildi.
- Aşama 6: 55/55 yeni test; toplam 179/179 bildirildi.
- **Aşama 7:** Bu paket belge taslağı modeli, PDF düzenleyicisi, örnek testleri ve kaynak-alan eşlemesi ekler. Sonuç **henüz kullanıcıda çalıştırılmadı.**

## Kaynak

- `Kaynaklar/03_Belgeler_ve_Sablonlar` altındaki Muhasebat Genel Müdürlüğü'nün Merkezî Yönetim Harcama Belgeleri Yönetmeliği PDF'si (kullanıcı ilk indirmede BEL-001 olarak başarı bildirdi).
- Açık adres: https://ms.hmb.gov.tr/uploads/2019/01/6.05-MERKEZ%C4%B0-Y%C3%96NET%C4%B0M-HARCAMA-BELGELER%C4%B0-Y%C3%96NETMEL%C4%B0%C4%9E%C4%B0.pdf
- Yönetmelik **Madde 22**: Yurt içi geçici görev, bildirim **Örnek 27** ve görevlendirme/konaklama kanıtları.
- Yönetmelik **Madde 23**: Yurt içi sürekli görev, bildirim **Örnek 28**, atama kararı/özel mesafe belgeleri.
- Yukarıdaki tarihli PDF'de **PDF sayfa indeksleri 100 ve 101 (görünür sayfa 101 ve 102)** Örnek 27 ve 28'dir.
- Örnek formdaki eski `YTL` sütunlarıyla güncel `TL` uygulaması ve 2024 sonrası yönetmelik değişiklikleri **mali hizmetler birimiyle güncellik açısından teyit edilmelidir.** Tarihsel görüntüye bakılarak formun birebir güncelliği ilan edilemez.

## Kurulan parçalar

1. `src/GorevYolluklari.Core/Documents/DraftDocuments.cs`: JSON'a da aktarılabilecek veri sözleşmesi. `decimal` + **tam kuruş** zorunluluğu, gider kalemlerinin türüne göre ayrımı, 2026/sürüm/görev oluru/mahalli/avans/bağımsız toplam kontrolleri. Aday m.43 süre birimleri bu modele **otomatik bağlanmaz**.
2. `src/GorevYolluklari.Documents/`: `PDFsharp-WPF` 6.2.4 ile yatay A4 belge, çok sayfa satır dökümü, kanıt ve hukuk referansları. Yalnız form **taslakları**; hiçbir imza alanı resmî/onaylı diye doldurulmaz.
3. `tests/GorevYolluklari.DocumentChecks/`: aday belge reddi, gider türü, yıl/kanıt/mahsubu ve icmal testleri.
4. `tests/GorevYolluklari.PdfChecks/`: gerçekten PDF oluşturup PDFsharp ile tekrar okuyarak A4 yatay, sayfa sayısı, çakışma, icmal ve mevcut dosyayı ezmeme denetimleri.
5. `Ciktilar/ASAMA7_DEMO/DEMO_*` içinde yalnız **ÖRNEK PERSONEL** verileriyle üç deneme PDF'si oluşturulur. Hiçbir gerçek personel bilgisi örnek paket içinde yoktur.
6. `07_Asama7_Belge_Taslaklari_Testleri.ps1`: önce yeni belge+PDF testlerini, ardından Aşama 6 regresyon testlerini çağırır.

## Resmî form alanları ile durum eşlemesi

| Alan | Örnek 27 | Örnek 28 | Aşama 7 |
|---|---|---|---|
| Ad/soyad, unvan, dairesi | Evet | Evet | Taslakta var |
| Aylık/kadro derecesi, ek gösterge, gündelik | Evet | Evet | Aşama 3 motorunda var; **PDF form alanlarına otomatik eşleme henüz yok** |
| Bütçe yılı | Evet | Evet | Var, 2026 ile sınırlandırıldı |
| Gidiş, dönüş, güzergâh | Evet | Evet | Tarih aralığı/güzergâh var; satır bazında saat/dakika eşlemesi **bekliyor** |
| Gündelik gün sayısı ve tutarı | Evet | Evet | Hazır onaylı satır tutarı gösterilir; onaylı gün sayacı **bekliyor** |
| Taşıt cinsi / ücreti / belge | Evet | Evet | Gider satırı ve belge var; taşıt türü ayrı kolon **bekliyor** |
| Konaklama / fatura | Uygun durumda | İlgili değil | Taslak satırı var; m.33(d) koşullarının gerçek kanıtları **bekliyor** |
| Aile fertleri / yer değiştirme | İlgili değil | Evet | Aile / taşınma toplam türleri var; kişi satır detayları **bekliyor** |
| Döviz, genel toplam, imza | Var | Var | Brüt/avans/net taslakta var; **resmî imza/kuru alanları yok** |
| Toplu kontrol icmali | Ayrı idari ek | Ayrı idari ek | Taslak icmal var; **ödeme emri değil** |

## Kritik güvenlik ve kabul sınırlamaları

- Bu modül, belge çizim katmanıdır; **hesaplama ve hak sahipliği tamamlanmış gibi işaretlenemez**.
- `AssignmentAndStationReviewed` ve `EntitlementAndAmountsReviewed` alanları, ancak bağımsız belgelendirilmiş gerçek kontrol yapılmışsa doğru işaretlenmelidir; bu boolean alanlarının bulunması yeterli hukuki kanıt değildir.
- **Kuruşlu satırlar** harici bağımsız tutarlarla eşleşmedikçe taslak oluşmaz. Ara işlemlerin hangi aşamada/şekilde yuvarlanacağı kullanıcı kurumunun resmî örnekleriyle doğrulanacaktır.
- `AdvanceTry > gross` ise bu ön sürüm hesap oluşturmaz. İade/mahsup gibi özel senaryolar sonraki mali mevzuat çalışmasına bırakılmıştır.
- Bir görev 2025/2027 ile çakışıyorsa çıktı bloklanır; diğer yıl tarifesi yoktur.
- Geçici belgeye sürekli görev taşınma kalemi; sürekli belgeye geçici görev konaklama kalemi konulamaz.
- Kayıt veya PDF dosyası sessizce **üzerine yazılmaz**. Önceki veri / kaynak / yedekler değiştirilmez.
- Maliye onaylı Örnek 27/28 formlarının birebir baskısı, imza/yetki alanları, gerçek anonim örneklerle mutabakat ve 2026 güncelliği **sonraki kabul kapısıdır**.
- Bu teslim paketine iş yeri/kullanıcı TC, IBAN, personel veya mali veri konmamıştır.
- PDFsharp-WPF NuGet paketi **derleme anında** gerekir. Kurulu olmayan Windows bilgisayarında bir defalık NuGet indirme isteyebilir; **kullanıcıya dağıtılan self-contained uygulama** ise bağımsız çalışabilir. NuGet engellenirse hata açıkça raporlanır.
- `PDFsharp-WPF 6.2.4` projedeki Ek Ders uygulamasında daha önce tercih edilmişti. Lisans ve güvenlik güncellemesi dağıtım aşamasında tekrar kontrol edilmeli.

## Çalıştırma

```powershell
$zip = "$env:USERPROFILE\Downloads\GorevYolluklari_Asama7_Belge_PDF_Taslaklari.zip"
Expand-Archive -LiteralPath $zip -DestinationPath "$env:USERPROFILE\Desktop" -Force
& "$env:USERPROFILE\Desktop\Görev Yollukları Hesaplama Sistemi\07_Asama7_Belge_Taslaklari_Testleri.ps1"
```

**Beklenen:** Yeni belge kontrol testleri + 7 gerçek PDF duman testi + önceki 179 regresyon testi sıfır hata ile geçmeli. Sonuç sayısı .NET çalıştırma çıktısında gösterilir. Başarılı olursa yalnız **belge taslağı altyapısı** tamamlandı sayılır; resmî form kabulü sayılmaz.

## Sıradaki iş

1. Kullanıcı Windows test sonucunu gönderir, PDF örneklerini inceler.
2. Güncel Örnek 27/28 asıl şablonları ve anonimleştirilmiş iki gerçek tahakkuk örneğiyle **görsel/alan/hesap tutarı mutabakatı** yapılır.
3. Derece, ek gösterge, harcırah gün sayısı, taşıt türü, seyahat hareket/varış satırları ve 45. madde aile satırları standart forma aktarılır.
4. İcmal ve imza alanları kurum uygulaması ile karşılaştırılır; onay yetkisini bilgisayar programı vermez.
5. Bir sonraki **8. aşama** olarak taşınabilir WPF form ekranı, hibrit Excel benzeri toplu veri girişi, erişilebilir lacivert-altın arayüz planlanır. Yine de bu eksikleri kapatmadan canlı resmî bordro sunulmaz.
