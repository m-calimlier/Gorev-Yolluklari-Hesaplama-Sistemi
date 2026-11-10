# Teknik mimari kararı — v0.1

## Bağımlılıksız Windows programı

- C#/.NET 10/WPF, native Windows masaüstü. `win-x64`, self-contained, tek EXE yayın hedefi.
- Yerel kayıt: `System.Text.Json` ile UTF-8 dosyalar; `Veriler/`, `Yedekler/`, `Ciktilar/` uygulama klasörü yanında.
- Kayıt yazma: geçici dosyaya yaz, mevcut dosyanın `.bak` yedeği, atomik taşıma; bozuk kayıtları sessizce boş kabul ETME.
- Taşınabilirlik: Windows kullanıcı hesabına bağlı DPAPI kullanılarak kaydedilmiş veriler tek başına taşınabilir sayılmaz. Hassas veri güvenliği için **kullanıcının kendi parolasına dayalı şifreli yedek** ayrıca tasarlanacak.
- Kaynaklar: `Kaynaklar/` içindeki resmî PDF ve KGM Excel. Uygulama herhangi bir canlı web hizmetine bağlanmadan çalışmalı.
- Excel hibrit: içe/dışa aktarma ve kural doğrulamasına yardımcı karşılaştırma. **Asıl hesap sonucu Excel hücre formüllerine bağlı olmayacak**. Excel.exe/Office Interop kullanılmayacak.
- Kaynak projesi: mcalimlier/Ek-Ders-Hakedis-Sistemi (.NET 10 WPF, PDFsharp-WPF, NPOI, veri yedeği) ve mcalimlier/DogruFatura (lacivert/altın kartlar, esnek ekran profilleri, JSON/yedek/çıktı ayrımı). Lisans ve yeniden kullanım izinleri bileşen düzeyinde kontrol edilecek.

## Geliştirme sırası

1. Kaynak kataloğu ve mevzuatın güncel derlemesi. (İlerleme: bağlantılar hazır; belgeleri yerel PC'ye indirmek gerekiyor.)
2. Kural matrisi ve hak sahipliği karar ağacı, kritik görüşlerin sınıflandırılması.
3. Geçici yolluk motoru, sınır koşulları, testler.
4. Sürekli yolluk motoru, aile/mesafe/ikametgâh testleri.
5. Resmî Örnek 27/28 alanları ve uygun PDF çıktı/ayrı icmal.
6. WPF kullanıcı arayüzü, personel kayıtları, veri güvenliği/yedekleme.
7. Self-contained tek EXE derleme, farklı Windows bilgisayarlarda test ve yasal kontrol.

## Kurallar için güvenlik kapısı

Görev mahalli, personel statüsü veya aile hak sahipliği belirsiz ise **kullanıcıdan mevzuata dayalı değerlendirme ve belge iste**, otomatik uygun kabul etme. Hesaplanmış tutarlar, PDF'nin oluşturulması ve yetkili HYS'de ödeme onayı birbirinden farklı süreçlerdir.

## İlk sürüm hedefi

Türkiye içi geçici ve sürekli görev. İlk kullanıcı: okul/kurum mali işleri. Yurt dışı geçici ve sürekli görev hariç.

## Açık başlıklar

- Harcama Belgeleri Yönetmeliğinin tüm güncel değişikliklerini doğrulama.
- Hak sahipliği, aile, mesafe, göreve başlama/atama, belge ve sınır testleri.
- Örnek 27/28 orijinal şablonlarının piksel/punto uyumlu örnekleri.
- Damga vergisi, mahsup ve kuruş yuvarlaması için yürürlük tarihli mevzuat.
- Kişisel veri için parola temelli taşınabilir şifreleme ve yedekleme.
