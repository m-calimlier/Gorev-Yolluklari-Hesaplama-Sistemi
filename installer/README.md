# Kurulum
Inno Setup 6.6 veya üstü kullanılır. Ana betik: Gorev-Yolluklari-Hesaplama-Sistemi.iss.
Koyu arayüz, program ikonu, masaüstü kısayolu ve başlangıçta boş temiz kurulum seçeneği.
Sorumluluk metni + kabul kutusu + Tamam zorunludur. Sessiz kurulum desteklenmez.
Kalıcı kullanıcı verileri yalnız Masaüstü/Görev Yollukları Hesaplama Sistemi altında tutulur.
Normal kurulum eski verileri hash doğrulamasıyla taşır; çakışmaları Yedekler/EskiKonumlar altında korur. TEHYS kendi veri hazırlayıcısını kullanır.
Temiz kurulum yalnız bilinen veri alt klasörlerini, ayrı onaydan sonra siler. Program kaldırıldığında kullanıcı verileri korunur.
Yalnız temiz dotnet publish klasörünü paketleyin; kullanıcı veri klasörünü paketlemeyin.
Eski DoğruFatura/DoğruTemin NSIS betikleri kapatılmıştır. SETUP_HAZIRLA.ps1 ve DoğruFatura Publish-Installers.ps1 artık Inno Setup kullanır.