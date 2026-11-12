# Aşama 8 kısa kullanım

## Veri girişi

Uygulama Veri Girişi panelinde açılır. Geçici görev veya Sürekli görev sekmesini seçin. İlk kartta mevcut personeli açılır listeden seçin veya Yeni kişi seçeneğiyle bilgilerini doldurun. Kişiyi kaydet düğmesi yeni kişiyi saklar ve sağdaki görev kartını açar. Daha önce kaydedilmiş kişiyi seçtiğinizde görev kartı doğrudan açılır. Belge numarası, uygulamanın ürettiği benzersiz görev kodudur. Görev oluru veya atama kararının numarasını kanıt bölümüne girin. Geçici görevde tarihleri takvimden, saat ve dakikayı açılır listelerden seçin. Sürekli görev sırası Kişi → Atama Ve Güzergâh → Sonuç şeklindedir. Atama adımında yalnız atama / işlem tarihi seçilir. Sonuçta Yer Değiştirmeyi Giderlere Aktar düğmesi tek gider satırı oluşturur. Taşıt, avans ve PDF işlemleri sonuçtaki açılır bölümde yapılır.

Mahalliyet, görev oluru ve inceleyen beyanını girin. İşaretler yalnız kullanıcı beyanını kaydeder; uygulama belge içeriğini okuyup hukuki yetki doğrulamaz. Gerekçesiz otomatik hak sahipliği kararı üretilmez.

Çıkış ve varış için il ve ilçe seçin. Liste internet olmadan da kullanılabilir. Sürekli görevde aile sayısına personelin kendisini eklemeyin. Hak sahipliği incelenmiş aile fertlerini sayın. İki yer seçildiğinde uygulama KGM mesafesini sorgular. Bağlantı veya sonuç sorunu varsa kilometreyi ve dayanağını elle girin. Sonucu incelerken mesafenin görevin gerçek güzergâhına uyduğunu kontrol edin. Sonuç yalnız yer değiştirme bileşenidir. Çok günlü geçici görevde açılır bölümde geçmiş gün defteri ve ayrı incelenmiş hak günleri gerekir. Takvimden otomatik hak günü türetilmez.

Formun altındaki Kaydet düğmesi alanları kontrol eder ve ön hesabı çalıştırır. Başarılı kayıttan sonra bildirim görünür ve Hesapla paneli açılır. Eksik veya geçersiz veri olduğunda form açık kalır.

Seyahat takvimi bölümünde gidiş varışı, görev başlangıç/bitişi, dönüş hareketi ve kanıtları girerek m.43 aday sürelerini görebilirsiniz. Bu listeler onaylı hak günleri değildir; gün tablosuna veya tutara otomatik aktarılmaz. Eksik saat, kronoloji veya saat dilimi şüphesi inceleme sonucuna gider.

## Gider ve belge taslağı

Gider ve Belge kartında ilgili gideri ekleyin. Açılan bölümde tutarı ve dayanağı yazın. Açıklama veya mevzuat referansını görmek için Açıklama Ve Mevzuat bölümünü açın. Bu Gideri Kaldır düğmesi yalnız açık gideri siler. Düzenleme ve kaldırma, görev formunu kaydedince diske yazılır. Gider Özeti, brütü ve avans sonrası neti otomatik hesaplar. Avans varsa Avans Varsa Girin bölümünü açın.

Toplu Aktarım bölümündeki TSV yapıştırma, Excel'den kopyalanan altı sütunu alır: Satır, Gider türü, Açıklama, Tutar (TL), Kanıt, Mevzuat. Önce bütün satırlar doğrulanır; hatada hiçbir satır eklenmez. Uygulamanın XLSX ver çıktısı `YOLLUK-GIDER-V1` şablonudur. XLSX al yalnız bu değer şablonunu destekler. Formül/makro çalıştırılmaz.

PDF taslağı gerekiyorsa Bağımsız Tutarlar bölümüne kurumun ayrı kontrol kaydındaki brüt ve neti girin. Kaydet düğmesi tutarları otomatik karşılaştırır. Eşleştiğinde gider dayanakları için onay istenir. Tutarlar eksikse, uyuşmazsa veya onay verilmezse görev kaydedilir ve PDF taslağı kapalı kalır.

Hesapla veya Ana Sayfa üzerinden kayıt seçin. Ayrıntı kartları personel, görev ve hesap bilgilerini gösterir. Düzenle aynı kaydı kendi sekmesinde açar. Sil onay ister. Belge kontrolleri tamamlandığında PDF düğmesi, Ciktilar klasöründe benzersiz adlı bir kontrol taslağı üretir. Bildirim kapatılınca klasör açılır.

## Sabitler ve kaynaklar

Geçici, Sürekli ve Ortak kartlarının her birinde Düzenle düğmesi vardır. Kaydet girilen değerleri doğrular ve saklar. Vazgeç kaydedilmemiş değişiklikleri geri alır. Kaynak değerlerini yükle, ilgili kartın varsayılan değerlerini geri getirir. Değiştirilmiş sabitlerle hesaplanan kayıtlar simülasyon olarak etiketlenir. PDF için kaynak değerleriyle yeniden hesaplama gerekir.

Sol menünün en altındaki Kaynaklar bölümünde ilgili belgenin PDF düğmesi tam dosyayı varsayılan PDF görüntüleyicide açar. Yerel dosya yoksa mevcut resmî bağlantı kullanılır veya eksik dosya bilgisi gösterilir.

## Kayıt, yedek ve taşıma

Ctrl+S veya Kaydet ile JSON kayıtlarını saklayın. Her güncellemede önceki sürüm tarihli yedeğe alınır. Aynı dosyaya başka bir süreç yazmışsa eski ekran yeni kaydı ezemez.

Yedekleme / Ayarlar bölümünde Yedek oluştur veya Yedekten geri yükle kullanın. Geri yükleme disk üzerindeki mevcut dosyayı da korur; kaydedilmemiş ekran değişiklikleri için açık uyarı verir. Bozuk veya şeması eksik JSON boş kayıt gibi açılmaz. JSON yedekleri şifreli değildir.

Programı başka bilgisayara taşırken EXE ile birlikte Veriler, Yedekler, Ciktilar ve Kaynaklar klasörlerini koruyun. GitHub yalnız kaynak kodu ve arındırılmış belgeleri taşır; kişisel veri yedeği değildir. Güncellemede veri klasörlerinizi yeni boş klasörlerle değiştirmeyin.

1024×768, 1366×768 ve 1920×1080 profilleri Ayarlar'dan uygulanabilir. 1920×1080 seçimi pencereyi ekranın kullanılabilir alanına büyütür. Pencere mevcut ekran alanına sığdırılır; yüksek DPI ve farklı bilgisayarlarda son kullanıcı kabulü ayrıca yapılmalıdır.

## Sınır

Bu sürüm resmî ödeme bordrosu, imza veya muhasebe onayı değildir. Form eşlemesi, gerçek anonim örneklerle kuruş/yuvarlama mutabakatı ve hukuki güncellik tamamlanmadan ödeme sürecinde resmî belge olarak kullanılmamalıdır.
