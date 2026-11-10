# Aşama 8 kısa kullanım

## İlk açılış

Program kişisel kayıtlarla dolu gelmez. Sol menüde Personel ve Kayıtlar bölümünden kişi ekleyin; derece, ek gösterge ve statüyü açıkça belirtin. Belirsiz veya özel statüde genel tarife uygulanmaz. Gerçek işlem öncesinde kaynak/mevzuat güncelliğini kurumunuzla doğrulayın.

Ana Sayfa üzerindeki Demo düğmesi yalnız sentetik ÖRNEK PERSONEL kaydı ve DEMO görevini ekler. Onayları otomatik işaretlemez. Denemeyi gerçek kayıtlarınızdan ayrı taşınabilir klasörde yapmanız önerilir.

## Görev girişi

Geçici Görev / Sürekli Görev bölümünden Yeni görev seçin. Aynı kişi için birden fazla görev kaydı açılabilir. Belge numarası benzersiz ve anlaşılır olmalıdır. Tarih ve saat biçimi `15.06.2026 09:00` şeklindedir.

Mahalliyet, görev oluru ve inceleyen beyanını girin. İşaretler yalnız kullanıcı beyanını kaydeder; uygulama belge içeriğini okuyup hukuki yetki doğrulamaz. Gerekçesiz otomatik hak sahipliği kararı üretilmez.

Sürekli görevde aile ve kaynaklı kilometre bilgilerini ayrıca girin. Sonuç yalnız yer değiştirme bileşenidir. Çok günlü geçici görevde açılır bölümde geçmiş gün defteri ve ayrı incelenmiş hak günleri gerekir. Takvimden otomatik hak günü türetilmez.

Ön hesapla sonucunda ham tutar, kategori, kaynak ve gerekçeyi görün. Bloke edilmiş durumda açıklanan veriyi tamamlayın; bilinmeyeni varsayımla doldurmayın.

Seyahat takvimi bölümünde gidiş varışı, görev başlangıç/bitişi, dönüş hareketi ve kanıtları girerek m.43 aday sürelerini görebilirsiniz. Bu listeler onaylı hak günleri değildir; gün tablosuna veya tutara otomatik aktarılmaz. Eksik saat, kronoloji veya saat dilimi şüphesi inceleme sonucuna gider.

## Hibrit tablo ve belge taslağı

Belgeler / İcmaller ekranında bağımsız belgelerden tam kuruş gider satırları girin. Satır türünü açılır listeden seçin; satır kimliklerini tekrarlamayın. Satırı düzenlemek için hücreye çift tıklayın veya klavyeyle gezinip düzenleyin. Seçili satırı kaldırma düğmesi vardır; bu değişiklik diske ancak Kaydet ile yazılır.

TSV yapıştırma, Excel'den kopyalanan altı sütunu alır: Satır, Gider türü, Açıklama, Tutar (TL), Kanıt, Mevzuat. Önce bütün satırlar doğrulanır; hatada hiçbir satır eklenmez. Uygulamanın XLSX ver çıktısı `YOLLUK-GIDER-V1` şablonudur. XLSX al yalnız bu değer şablonunu destekler. Formül/makro çalıştırılmaz.

Avans ve varsa avans kanıtını girin. Bağımsız brüt/net kontrol alanlarını ayrı kontrolden doldurun; programın kendi toplamını bağımsız kontrol gibi kopyalamayın. İnceleme işaretinden sonra İncelemeyi kaydet / hesapla ile tarihli inceleme ve güncel ön hesap oluşturun.

Taslak PDF oluştur / aç yalnız KONTROL TASLAĞI üretir. Çıktı benzersiz adla Ciktilar klasöründedir; önceki PDF ezilmez. Kontrol icmali, bağımsız incelemesi işaretli kayıtların tümünü yeniden doğrular; geçersiz kayıt varsa icmal durur.

## Kayıt, yedek ve taşıma

Ctrl+S veya Kaydet ile JSON kayıtlarını saklayın. Her güncellemede önceki sürüm tarihli yedeğe alınır. Aynı dosyaya başka bir süreç yazmışsa eski ekran yeni kaydı ezemez.

Yedekleme / Ayarlar bölümünde Yedek oluştur veya Yedekten geri yükle kullanın. Geri yükleme disk üzerindeki mevcut dosyayı da korur; kaydedilmemiş ekran değişiklikleri için açık uyarı verir. Bozuk veya şeması eksik JSON boş kayıt gibi açılmaz. JSON yedekleri şifreli değildir.

Programı başka bilgisayara taşırken EXE ile birlikte Veriler, Yedekler, Ciktilar ve Kaynaklar klasörlerini koruyun. GitHub yalnız kaynak kodu ve arındırılmış belgeleri taşır; kişisel veri yedeği değildir. Güncellemede veri klasörlerinizi yeni boş klasörlerle değiştirmeyin.

1024×768, 1366×768 ve 1920×1080 profilleri Ayarlar'dan uygulanabilir. Pencere mevcut ekran alanına sığdırılır; yüksek DPI ve farklı bilgisayarlarda son kullanıcı kabulü ayrıca yapılmalıdır.

## Sınır

Bu sürüm resmî ödeme bordrosu, imza veya muhasebe onayı değildir. Form eşlemesi, gerçek anonim örneklerle kuruş/yuvarlama mutabakatı ve hukuki güncellik tamamlanmadan ödeme sürecinde resmî belge olarak kullanılmamalıdır.
