# AGENTS.md — Görev Yollukları Hesaplama Sistemi

> Codex için kalıcı proje talimatları — 10.11.2026.
> Devam bağlamı: `docs/CODEX_DEVIR_2026-11-10.md`.

## Proje hedefi
Türkiye'de MEB okul/kurum mali işlerinde yurt içi **geçici ve sürekli görev yolluğu** için mevzuata dayalı ön hesap, personel ve görev kaydı, kanıt kontrolü, hesap dökümü ve uygun aşamada resmî bildirime uyumlu çıktı üreten **taşınabilir Windows** uygulaması.

## Değişmez teknik kararlar
- Mevcut C# / **.NET 10 WPF** proje yapısını koru. Başka UI çatısına kökten geçme; gerekmedikçe WPF UI gibi büyük bir dış paketi tam entegre etme.
- **SQL Server, SQLite, başka veritabanı motoru, servis veya Office kurulumu yok.** Kalıcı kayıt `System.Text.Json` / JSON dosyalarında, yedekleme ve hata denetimiyle.
- Hedef dağıtım **win-x64 self-contained**; kullanıcı bilgisayarında .NET Runtime kurulmasını gerektirmesin. Derlemede .NET SDK ve NuGet gerekebilir; kullanıcıya dağıtılan programda gerekmesin.
- `Veriler/`, `Yedekler/`, `Ciktilar/`, `Kaynaklar/` içeriğini koru. Kayıtları sessizce sıfırlama, mevcut dosyaların üzerine sessizce yazma.
- Mevzuat kaynağı `Kaynaklar/`. Hesaplama C# ile yapılır; **hibrit Excel/DataGrid** yalnız veri girişi, aktarım ve bağımsız kontrol için. Excel.exe / Office Interop kullanma.
- Diğer projelerin yararlı örüntülerinden seçerek yararlan: `mcalimlier/Ek-Ders-Hakedis-Sistemi` (.NET 10, WPF, hibrit tablo, PDF) ve `mcalimlier/DogruFatura` (lacivert/altın kartlar, JSON/yedek, uyarlanabilir ekranlar). Kopyalamadan önce kaynak/lisansı kontrol et.
- Türkçe kullanıcı arayüzü; lacivert–altın, okunabilir, klavye ve küçük ekran dostu, sade tasarım.

## Hukuki / mali güvenlik kapısı
- Projenin şimdiye kadar ürettiği Örnek 27/28 çıktıları yalnız **KONTROL TASLAĞI — ÖDEME BELGESİ DEĞİLDİR** niteliğindedir.
- Gerçek kanıt ve onay, hak sahipliği, memuriyet mahalli, özel personel statüsü, 6245 / yürürlük tarihli tarife ve form mutabakatı tamamlanmadan resmî ödeme bordrosu / tahakkuk / imza üretilmiş gibi gösterme.
- Belirsiz olguları varsayımla doldurma; kullanıcıdan doğrulama iste veya ilgili iş akışını durdur.
- Özellikle 39, 42, 43, 45. maddelerin süre ve hak sahipliği yorumlarını kendi başına genişletme. 2026 H Cetveli dışındaki yıllarda geçerli tarife yoksa hesap bloke edilmeli.
- 6245 sayılı Kanun PDF'sinin mevcut olması veya SHA-256 imzası **hukuki güncellik kanıtı değildir**. Mevzuat güncelliğini ayrıca teyit et.
- Demo/test belgelerinde gerçek TC, IBAN, personel adı, görev veya mali kayıt kullanma. Kaynaklara kişisel veri, parola, sır ekleme; GitHub'a göndermeden önce denetle.

## Çalışma ve test kuralları
1. İLK İŞ `docs/CODEX_DEVIR_2026-11-10.md`, `docs/PROJE_DEVIR_NOTU_2026-11-09.md` (varsa), `docs/ASAMA7_BELGE_TASLAKLARI_VE_KABUL_KAPISI.md` ve projeyi oku.
2. Kaynak dosyaları, projeler, testler ve scriptleri gerçek klasörden doğrula. Geçmiş raporlar **kullanıcının bildirdiği** sonuçlardır; ilk keşifte sen de çalıştır.
3. İlk Codex oturumunda **önce salt-okunur keşif + test + plan** yap; kullanıcının açık izni olmadan büyük değişiklik veya 8. aşama kodlaması yapma.
4. Başlangıç regresyon komutu (Windows PowerShell, proje kökünden): `& .\07_Asama7_Belge_Taslaklari_Testleri.ps1`. Ayrıca WPF proje derlemesini kontrol et. Sonuç ve uyarıları raporla.
5. Önceki testleri silme, sayısını düşürme, atlatma veya `--no-verify` ile geçme. Her küçük değişiklikten sonra ilgili testleri, aşama sonunda tam regresyonu çalıştır.
6. Önceki dosya ve kişisel kayıtları değiştirmeden önce yedek al. Şüpheli eksiklikte önce kullanıcıya göster, otomatik başka ZIP'lerden eski dosyalarla üzerine yazma.
7. İşlem sonunda değişen dosyaları, çalıştırılan komutları, test sayılarını, uyarıları, yapılmayanları ve sonraki adımı kısaca bildir.
8. Kullanıcı açıkça istemeden Git branch, commit, push veya GitHub release oluşturma.

## Şu anki aşama
- Aşama 1–7 dosyaları kullanıcının Windows klasöründe mevcut.
- Kullanıcının bildirdiği regresyon durumu: **256/256 kontrol başarılı**; bunun 179'u aşama 2–6, 70'i belge-model, 7'si PDF kontrolü.
- PDF derleme hataları için iki dosyaya elle `using System.IO;` eklenmiş ve testler geçti.
- `tests/GorevYolluklari.PdfChecks/Program.cs` içindeki CS8603 düzeltmesinin uygulanmış olduğu 10.11.2026 Codex incelemesinde doğrulandı. WPF `MainWindow.xaml.cs` için eksik `System.IO` içe aktarımı da düzeltildi.
- Resmî form/bordro mutabakatı hâlâ **bekliyor**. Aşama 8 yazılım uygulaması ve açık kabul kapıları aşağıdaki güncellemede ve teslim raporunda açıklanır.

### Aşama 8 uygulama güncellemesi
- Kullanıcının açık izniyle WPF ekranları, JSON kayıt/revizyon/yedek, kontrollü TSV/XLSX değer aktarımı, seyahat aday takvimi ve taslak PDF akışı eklendi. Eski hesap motorları değiştirilmedi.
- Güncel genişletilmiş doğrulama: `& .\08_Asama8_IsAkisi_Testleri.ps1`. Bu komut bütün projeleri derler; önceki 256 kontrolü ve yeni iş akışı/PDF/WPF kontrollerini çalıştırır. Test sayıları ve açık kabul kapıları teslim raporunda kayıtlı olmalıdır.
- Plan ve ilerleme: `docs/ASAMA8_INCELEME_VE_UYGULAMA_PLANI_2026-11-10.md`, `docs/ASAMA8_ILERLEME.md`, kısa kullanım `docs/ASAMA8_KULLANIM.md`.
- Son tam test: **319/319**. Ayrıntılı teslim: `docs/ASAMA8_UYGULAMA_VE_GITHUB_RAPORU_2026-11-10.md`. Sonraki oturum bu sonucu geçmiş kayıt olarak ele almalı ve gerekli kontrolleri gerçek dosyalarda yeniden çalıştırmalıdır.
- Kullanıcı GitHub deposu oluşturma ve kaynak kodu gönderme yetkisi verdi. Depo: `mcalimlier/Gorev-Yolluklari-Hesaplama-Sistemi` (özel). Masaüstündeki yerel klasör GitHub Desktop'a kaydedildi. Sonraki oturumlarda mevcut remote/branch durumunu gerçek Git verisinden doğrula.
- Kişisel kayıtlar, yedekler, çıktılar ve ham kaynak PDF/XLSX/HTML dosyaları Git kapsamı dışındadır; bunlar yerel arşivde korunur. GitHub tam kişisel veri yedeği değildir.
- Temiz ikinci Windows makinesi, gerçek yüksek DPI/klavye kabulü, güncel mevzuat ve resmî form/kuruş mutabakatı hâlâ ayrı kabul kapılarıdır.

Bu dosyayı kaynak gerçekliğiyle çeliştiği yerde sorgula; daha ayrıntılı durum için devir notuna bak.
