# Ek Ders Hakediş Sistemi → Görev Yollukları: arayüz ve kurulum aktarımı

## Kaynak
- Referans depo: https://github.com/m-calimlier/Ek-Ders-Hakedis-Sistemi (main)
- İncelenen kaynak: `src/Hakedis.App/Themes/Oehs.xaml`,
  `src/Hakedis.App/Themes/Workspace.xaml`,
  `src/Hakedis.App/Themes/PaymentCards.xaml`, `src/Hakedis.App/MainWindow.xaml`,
  `src/Hakedis.App/TeacherForm.xaml`, `scripts/Build-Installer.ps1`,
  `scripts/Build-ReleaseAssets.ps1`.
- Eski WPF yerleşimi/renkleri yerine yeni proje için bağımsız ve ön ekli
  `src/GorevYolluklari.App/Themes/HakedisInspired.xaml` oluşturuldu.
  Referans uygulamadaki sınıflar/iş mantığı taşınmadı.

## Uygulanan WPF bileşenleri
- `HakedisWorkspaceCard`: lacivert geçişli, yuvarlatılmış, ince kenarlıklı giriş kartı.
- `HakedisCardTitle`, `HakedisCardSubtitle`, `HakedisInfoCard`: bölüm, açıklama ve durum kartları.
- `HakedisFieldText`, `HakedisFieldCombo`, `HakedisFieldLabel`: TextBox, ComboBox, Label.
- `HakedisDatePicker` ve `HakedisCalendar*`: OEHS tasarımından uyarlanan,
  koyu zeminli takvim, metin ile yer tutucunun çakışmasını engelleyen tarih alanı.
- `HakedisPrimaryAction`, `HakedisSecondaryAction`: PDF/hesapla vurgusu ile
  geri/düzenle/isteğe bağlı işlem ayrımı.
- `HakedisMetricCard`: hesap sonucu bilgi kartı.
- `HakedisDataGrid`: Excel benzeri koyu tablo satırları ve seçili hücre kontrastı.
- `HakedisStatusBanner`: yatay durum şeridi.
- Geçici ve sürekli görevlerde üç kartlı `Kişi | Görev ve Güzergâh | Sonuç` düzeni korunur;
  ekran daraldığında küçük kart geçiş düğmeleri görünür.

## Mevcut işleyiş korunur
- Yalnızca hesap için gerekli girdiler, otomatik kişi kaydı ve doğrudan PDF.
- Memura ait belge inceleme/onay kutuları yeniden eklenmez.
- 2026 tarife verileri, 6245 hesap motoru, JSON şeması, kayıt dizini, PDF hesaplama
  ve mevcut test dosyalarının iş mantığı değiştirilmez.
- Programın mevcut ikon/logo dosyaları korunur.
- Geçici çok günlü görevde özel gün ve konaklama verileri kullanıcı tarafından girilir;
  mevzuatın istisna hak sahipliği kararları resmî onay olarak varsayılmaz.

## Ek Ders tarzı paketleme
- `scripts/Build-Portable.ps1`: self-contained ve tek dosya EXE içeren veri içermeyen Windows ZIP.
- `scripts/Build-Installer.ps1`: Inno Setup 6.6+ otomatik tespiti, self-contained tek EXE,
  temiz yayın içeriğinin sıkı kontrolü.
- Dağıtım izni verilen dosyalar: `GorevYolluklari.exe`, `KULLANIM.md`,
  `Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli_Dogrulanmis_OnHesap.json`.
- `installer/Gorev-Yolluklari-Hesaplama-Sistemi.iss` içindeki AppId,
  masaüstü veri konumu, normal güncellemede veri koruması ve açık izinli temiz kurulum
  işleyişi değiştirilmedi. Kurulum/uninstall kayıtları silmez.
- Mevcut `PDF` çıktı formatının resmî Örnek 27/28 birebir formu olduğu iddia edilmez.

## Yerel kabul adımları
CMD'de (GitHub Desktop'ta doğru dal Pull origin sonrasında):

```bat
dotnet build .\GorevYolluklari.slnx -c Release
dotnet run --project .\tests\GorevYolluklari.UiChecks -c Release -- "%CD%" "%TEMP%\Yolluk_UiKontrol"
dotnet run --project .\tests\GorevYolluklari.DocumentChecks -c Release
dotnet run --project .\tests\GorevYolluklari.PdfChecks -c Release
```

Kurulum betiği yalnızca Windows üzerinde .NET 10 SDK ve Inno Setup 6.6+ ile
ayrıca test edilebilir. Yukarıdaki değişiklikler GitHub'da oluşturulmuş olsa da
**sonuçları yerel Windows testleri gelmeden başarılı kabul edilmez**.
