# Aşama 8 ilerleme kaydı

Plan: ASAMA8_INCELEME_VE_UYGULAMA_PLANI_2026-11-10.md

- Kullanıcı planın uygulanmasını, GitHub deposu, commit/push ve GitHub Desktop bağlantısını açıkça istedi.
- Çalışma masaüstündeki mevcut klasörde, tam kaynak yedeği Windows geçici klasöründe: GorevYolluklari_Asama8_2152c9e034d1457e83ecfecfab22dc87.
- Başlangıçta Git deposu yok; mevcut klasörü Git deposuna dönüştürmek kullanıcının istediği yerel bağlantıyı korur. Ayrı worktree oluşturulmadı.
- 8.1 kayıt ve uygulama katmanı: 27 iş akışı testi başarısız başlangıçtan sonra başarılı.
- Arayüz: yerel WPF stil sözlüğü; formlar, tablolar ve uygulama servisleri bağlanıyor.
- Kaynak kopyalama kararı: örnek projelerden kod/asset kopyalanmadı; lisans belirsizliği nedeniyle yalnız genel desenler uygulandı.
- XLSX kararı: yeni paket yerine sınırlı, sürümlü değer şablonu Open XML/ZIP ile üretildi; genel amaçlı Excel okuyucusu değildir.
- Hukuki yorum, m.43 aday hak günü dönüşümü ve resmî çıktı kapsamı genişletilmedi.

## Son inceleme ve kararlar

- WPF yedi bölüm, üç pencere boyutu: ilk görsel çalışma 25/25. Açılır liste/düğme metin kontrastı görsel incelemeyle düzeltildi.
- PDF kurum satırı uyarı bandıyla çakışıyordu: konum testi eski PDF üzerinde başarısız oldu; yeni düzende geçti ve PNG görsel olarak incelendi.
- PDF güvenliği: dört ek test önce tutarsız icmalde başarısız oldu; toplamlar, negatif net, tür ve kuruş denetimleri eklendikten sonra 4/4 geçti. Eski 7 PDF testi korundu.
- Şemasız `{}` JSON için ek test önce başarısız oldu; okuma/geri yüklemede açık şema zorunluluğu getirildi. Son tam regresyonda yeniden doğrulanacak.
- GitHub deposu özel olarak oluşturuldu: mcalimlier/Gorev-Yolluklari-Hesaplama-Sistemi. Yerel klasör Desktop CLI `github open` ile GitHub Desktop'a kaydedildi; uygulama ağacında proje adı ve main dalı görüldü.
- Tarayıcı eklentisi mevcut olmadığından web sayfasını bağlı Chrome aracında açma denemesi sonuçlanmadı; depo sunucuda GitHub API ile oluşturuldu, son doğrulama Git/HTTPS üzerinden yapılacak.
- Son inceleme: bağımsız alt ajan aracı yok; plan/kaynak/test karşılaştırması yazar tarafından yapıldı. Bu, bağımsız kod incelemesi yerine geçmez.
- Ertelenenler: temiz ikinci Windows makinesi, gerçek %125/%150 DPI ve kullanıcı klavye kabulü; parola tabanlı şifreli yedek; hukuki ve resmî form mutabakatı.
- Son inceleme düzeltmeleri: tablo kopyasında gider türü geri aktarımı, eksik JSON şeması reddi, yalnız ilgili görevin snapshot'ını geçersiz kılma, güzergâh değişiminde ilgili incelemeleri yenileme. İlgili testler önce başarısız çalıştırıldı; düzeltmelerden sonra tam kapıda yeniden çalıştırılıyor.
- Seyahat aday takvimi: eksik saatler için blokaj, mevcut motorla 1+1 aday süre ve hak günlerine otomatik aktarmama kontrolleri eklendi; formda ayrı açılır bölüm kullanılıyor.
- İsteğe bağlı seyahat tarih alanlarının boş değer dönüşümü WPF testinde hata verdi; boşu null koruyan doğrulamalı dönüşüm eklendi. Kullanıcının girmediği tarih varsayımla tamamlanmaz.
- Eski belge test yardımcısındaki CS8602 için `DoesNotReturnIf(false)` açıklaması eklendi; 70 testin koşulları ve sayısı değiştirilmedi.
- Son tam kapı: 319/319, çıkış 0. Uyarı açıklaması sonrası belge projesi 0 hata/0 uyarı ve 70/70 tekrar doğrulandı. Son self-contained paket ayrı veri köküyle açıldı ve yanıt verdi. Ayrıntılar teslim raporundadır.
