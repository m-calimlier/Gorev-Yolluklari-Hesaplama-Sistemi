using System.IO;
using System.Windows.Automation;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Application;
using System.Net.Http;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
using GorevYolluklari.Storage.Records;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GorevYolluklari.Storage;
using GorevYolluklari.App;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var root = Path.Combine(Path.GetTempPath(), "YollukUI_" + Guid.NewGuid().ToString("N"));
        var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable("GOREV_YOLLUKLARI_ROOT", root);
        PortablePaths.EnsureFolders();
        Directory.CreateDirectory(Path.Combine(PortablePaths.Sources, "02_Butce_ve_Tarifeler"));
        File.Copy(Path.Combine(args[0], "Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli_Dogrulanmis_OnHesap.json"), Path.Combine(PortablePaths.Sources, "02_Butce_ve_Tarifeler/2026_H_Cetveli_Dogrulanmis_OnHesap.json"));
        int passed = 0;
        void Check(string name, bool ok) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("[OK] " + name); }
        try
        {
            var app = new App(); app.InitializeComponent();
            var dialogs = new List<string>();
            string? openedFolder = null;
            var failKgm = false;
            var window = new MainWindow((message, title, buttons) =>
            {
                dialogs.Add(title + ": " + message);
                return buttons == MessageBoxButton.YesNo ? MessageBoxResult.Yes : MessageBoxResult.OK;
            }, folder => openedFolder = folder, (from, to, _) => failKgm
                ? Task.FromException<int>(new HttpRequestException("SENTETİK BAĞLANTI HATASI"))
                : Task.FromResult(from == new KgmPlace("ADANA", "ALADAĞ") && to == new KgmPlace("ADANA", "CEYHAN") ? 84 : 0));
            // Ekran görüntüsü alınırken masaüstü tıklamaları ve pencere etkinliği testi etkilemesin.
            window.ShowActivated = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = SystemParameters.VirtualScreenLeft - 3000;
            window.Top = SystemParameters.VirtualScreenTop;
            window.IsHitTestVisible = false;
            window.Show();
            var navigation = (ListBox)window.FindName("Navigation");
            var page = (StackPanel)window.FindName("Page");
            var vm = (GorevYolluklari.App.ViewModels.MainViewModel)window.DataContext;
            var repository = new JsonRecordRepository(root);
            Button FindButton(string label) => Descendants<Button>(page).Single(x => Equals(x.Content, label));
            TextBox Box(string name) => Descendants<TextBox>(page).Single(x => AutomationProperties.GetName(x) == name);
            void Click(Button b)
            {
                b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                window.UpdateLayout();
            }
            void Set(string name, string value)
            {
                var box = Box(name);
                box.Text = value;
                box.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            }
            void Go(int index)
            {
                navigation.SelectedIndex = index;
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                if (navigation.SelectedIndex != index)
                    throw new Exception("Gezinme tamamlanamadı: " + ((TextBlock)window.FindName("Status")).Text);
            }
            TabControl Tabs() => Descendants<TabControl>(page).Single();
            void SelectTab(int index)
            {
                Tabs().SelectedIndex = index + 1;
                window.UpdateLayout();
            }
            void Pick(string name, object value)
            {
                var combo = Descendants<ComboBox>(page).Single(x => AutomationProperties.GetName(x) == name);
                combo.SelectedValue = value;
                combo.GetBindingExpression(ComboBox.SelectedValueProperty)!.UpdateSource();
            }
            ComboBox ChoiceBox(string name) => Descendants<ComboBox>(page).Single(x => AutomationProperties.GetName(x) == name);
            void SelectPlace(string label, string city, string district)
            {
                ChoiceBox(label + " ili").SelectedValue = city;
                ChoiceBox(label + " ilçesi").SelectedItem = district;
            }
            void Review(string name)
            {
                var box = Descendants<CheckBox>(page).Single(x => AutomationProperties.GetName(x) == name);
                box.IsChecked = true;
                box.GetBindingExpression(CheckBox.IsCheckedProperty)!.UpdateSource();
            }

            void Capture(string name, int width, int height)
            {
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(output, name + ".png"));
                encoder.Save(file);
            }

            Check("Alti gezinme bolumu ve tek veri girisi", navigation.Items.Count == 6 && !navigation.Items.Contains("Personel ve Kayıtlar"));
            Check("Veri girisi ilk ekran", navigation.SelectedIndex == 1);
            Check("Baslangicta modul secili degil", Tabs().SelectedIndex == 0 && ((TabItem)Tabs().SelectedItem).Visibility == Visibility.Collapsed);
            Check("Secim olmadan gorev formu acilmaz", !Descendants<TextBox>(page).Any());
            SelectTab(0);
            if (dialogs.Count != 0) throw new Exception(dialogs.Last());
            Check("Gecici sekme formu acar", Tabs().SelectedIndex == 1 && ChoiceBox("Çıkış ili").Items.Count == 81);
            foreach (var size in new[] { (1024d, 768d), (1366d, 768d), (1920d, 1080d) })
            {
                window.Width = size.Item1; window.Height = size.Item2;
                for (var i = 0; i < 6; i++)
                {
                    Go(i);
                    Check($"{size.Item1}x{size.Item2} bolum {i} acilir", ((StackPanel)window.FindName("Page")).Children.Count > 0);
                    Capture($"UI_{size.Item1}_{i}", (int)size.Item1, (int)size.Item2);
                }
            }
            Go(1);
            Check("Kisi kaydedilene dek sag kart kapali", !Box("Belge numarası").IsEnabled && !FindButton("Kaydet").IsEnabled);
            Click(FindButton("Kişiyi kaydet"));
            Check("Eksik ad soyad kirmizi", Equals(Box("Ad soyad").BorderBrush, Brushes.LightCoral));
            Check("Eksik form kaydedilmez ve acik kalir", navigation.SelectedIndex == 1 && repository.Load().Personnel.Count == 0);
            Check("Gereksiz yeni personel dugmesi yok", !Descendants<Button>(page).Any(b => Equals(b.Content, "Yeni personel bilgileri")));
            Set("Ad soyad", "SENTETİK PERSONEL");
            Set("Unvan", "SENTETİK UNVAN");
            Set("Kurum", "SENTETİK KURUM");
            Pick("Statü", PersonnelTariffGroup.GeneralCivilServant);
            Pick("Derece", 5);
            Set("Ek gösterge", "0");
            Click(FindButton("Kişiyi kaydet"));
            Check("Kisi ayri kaydedilir ve gorev karti acilir", repository.Load().Personnel.Count == 1 &&
                repository.Load().Duties.Count == 0 && Box("Belge numarası").IsEnabled);
            window.Width = 1024; window.Height = 768; window.UpdateLayout();
            var personPosition = Box("Ad soyad").TranslatePoint(new Point(0, 0), page);
            var dutyPosition = Box("Belge numarası").TranslatePoint(new Point(0, 0), page);
            Check("Ilk iki kart yanyana", dutyPosition.X > personPosition.X + 160);
            Capture("Ikili_Veri_Girisi", 1024, 768);
            var degree = Descendants<ComboBox>(page).Single(c => AutomationProperties.GetName(c) == "Derece");
            degree.IsDropDownOpen = true;
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Check("Derece acilir listesi gercek popup acar", degree.Items.Count == 15 &&
                degree.Template.FindName("PART_Popup", degree) is System.Windows.Controls.Primitives.Popup { IsOpen: true });
            degree.IsDropDownOpen = false;
            Check("Il secmeden ilce kapali", !ChoiceBox("Çıkış ilçesi").IsEnabled);
            SelectPlace("Çıkış", "ADANA", "ALADAĞ");
            SelectPlace("Varış", "ADANA", "CEYHAN");
            Check("Ilce secenekleri ile gore suzulur", ChoiceBox("Çıkış ilçesi").Items.Contains("ALADAĞ") && !ChoiceBox("Çıkış ilçesi").Items.Contains("ÇANKAYA"));
            void PickDate(string label, int hour)
            {
                Descendants<DatePicker>(page).Single(c => AutomationProperties.GetName(c) == label).SelectedDate = new DateTime(2026, 6, 15);
                ChoiceBox(label + " saati").SelectedIndex = hour;
                ChoiceBox(label + " dakikası").SelectedIndex = 0;
            }
            PickDate("Ayrılış tarihi", 9);
            PickDate("Dönüş tarihi", 20);
            var multiDayReview = Descendants<Expander>(page).Single(x => x.Header.ToString()!.StartsWith("Çok Günlü Görev"));
            Check("Tek gunde gecmis gun defteri gizli", multiDayReview.Visibility == Visibility.Collapsed);
            var returnDate = Descendants<DatePicker>(page).Single(x => AutomationProperties.GetName(x) == "Dönüş tarihi");
            returnDate.SelectedDate = new DateTime(2026, 6, 16);
            Check("Cok gunlu gorevde gecmis gun defteri erisilebilir", multiDayReview.Visibility == Visibility.Visible);
            returnDate.SelectedDate = new DateTime(2026, 6, 15);
            var timeline = Descendants<Expander>(page).Single(x => x.Header.ToString()!.StartsWith("Seyahat Takvimi"));
            timeline.IsExpanded = true;
            window.UpdateLayout();
            PickDate("Gidiş varışı", 10);
            Click(Descendants<Button>(page).Single(b => AutomationProperties.GetName(b) == "Gidiş varışı tarihini temizle"));
            Check("Istege bagli tarih ve saat temizlenebilir", Descendants<DatePicker>(page).Single(p =>
                AutomationProperties.GetName(p) == "Gidiş varışı").SelectedDate is null && ChoiceBox("Gidiş varışı saati").SelectedIndex == -1);
            timeline.IsExpanded = false;
            Pick("Memuriyet mahalli değerlendirmesi", DutyStationAssessment.Outside);
            Set("Görev oluru / atama kararı referansı", "SENTETİK OLUR");
            Set("Memuriyet mahalli kanıtı", "SENTETİK MAHAL");
            Set("İnceleyen beyanı / kayıt referansı", "SENTETİK İNCELEME");
            Click(FindButton("Kaydet"));
            Check("Onaysiz gorev tutar uretmez", repository.Load().Duties.Count == 0 && dialogs.Last().Contains("tamamlanamadı"));
            Review("Görev oluru incelendi");
            Review("Mahalliyet incelendi");
            Check("Gecici panelde aile mesafe alani yok", !Descendants<TextBox>(page).Any(x => AutomationProperties.GetName(x) == "Kaynaklı kilometre"));
            Click(FindButton("Kaydet"));
            var saved = repository.Load().Duties.Single();
            Check("UI kaydi JSON olusur", saved.Snapshot?.Amount == 850m * 2 / 3);
            Check("Basari bildirimi ve otomatik sonuc", navigation.SelectedIndex == 2 && dialogs.Last().StartsWith("Kayıt başarılı"));
            Check("Eksik belge incelemesinde PDF kapali", !FindButton("PDF").IsEnabled);
            var amountLabel = Descendants<TextBlock>(page).Single(t => t.Text == "566,67 TL");
            Check("Sonuc iki ondalik basamakla gosterilir", amountLabel is not null);
            var resultPicker = ChoiceBox("Hesaplanmış görev");
            Check("Sonuc kartlari iki sutunda", amountLabel!.TranslatePoint(new Point(0, 0), page).X >
                resultPicker.TranslatePoint(new Point(0, 0), page).X + 100);
            Click(FindButton("PDF eksiklerini aç"));
            Check("PDF eksigi ilgili alana goturur", navigation.SelectedIndex == 1 &&
                Equals(Box("Bağımsız brüt TL").BorderBrush, Brushes.LightCoral));
            window.Width = 1024; window.Height = 768; window.UpdateLayout();
            Capture("Sonuc_Kartlari", 1024, 768);

            Go(0);
            Check("Ana sayfa hesap arsivi", Descendants<ComboBox>(page).Any(x => AutomationProperties.GetName(x) == "Hesaplanmış görev"));
            Click(FindButton("Düzenle"));
            Check("Duzenle dogru sekme ve dolu alanlar", navigation.SelectedIndex == 1 && Tabs().SelectedIndex == 1 && Equals(ChoiceBox("Çıkış ilçesi").SelectedItem, "ALADAĞ"));
            SelectPlace("Çıkış", "ADANA", "KOZAN");
            Click(FindButton("Kaydet"));
            Check("Guzergah degisince ilgili incelemeler yenilenir", navigation.SelectedIndex == 1 && repository.Load().Duties.Single().Origin == saved.Origin);
            Review("Görev oluru incelendi");
            Review("Mahalliyet incelendi");
            var details = Descendants<Expander>(page).Single(x => Equals(x.Header, "PDF İçin Son Tutar Kontrolü"));
            details.IsExpanded = true;
            window.UpdateLayout();
            Click(FindButton("Gündeliği giderlere aktar"));
            Click(FindButton("Gündeliği giderlere aktar"));
            var daily = Descendants<Expander>(page).Single(x => AutomationProperties.GetName(x).StartsWith("Gider OTO-GD-"));
            daily.IsExpanded = true;
            window.UpdateLayout();
            Check("Gundelik tek satira aktarilir, tutar elle girilmez", Box("Gider tutarı (TL)").IsReadOnly &&
                Box("Gider tutarı (TL)").Text.Contains("566,67") && Box("Gider dayanağı").Text == "SENTETİK OLUR");
            details = Descendants<Expander>(page).Single(x => Equals(x.Header, "PDF İçin Son Tutar Kontrolü"));
            details.IsExpanded = true;
            window.UpdateLayout();
            Check("Gider ozeti otomatik hesaplanir", Descendants<TextBlock>(page).Any(t =>
                t.Text == "Brüt 566,67 TL  ·  Damga vergisi 4,30 TL  ·  Avans 0,00 TL  ·  Tahmini net 562,37 TL"));
            Set("Bağımsız brüt TL", "566,67");
            Set("Bağımsız net TL", "565,00");
            Check("Bagimsiz net uyusmazligi otomatik gosterilir", Descendants<TextBlock>(details)
                .Any(text => text.Text.Contains("Bağımsız net tutarı")));
            Set("Bağımsız net TL", "566,67");
            var beforeSaveDialogs = dialogs.Count;
            Click(FindButton("Kaydet"));
            Check("Kaydederken eslesen toplamlar otomatik onaya sunulur", dialogs.Skip(beforeSaveDialogs)
                .Any(message => message.StartsWith("Belge kontrolü: Toplamlar eşleşti")));
            Check("Duzenleme kimligi korur", repository.Load().Duties.Single().Id == saved.Id);
            Check("Incelenen kayitta PDF etkin", navigation.SelectedIndex == 2 && FindButton("PDF").IsEnabled);
            Click(FindButton("PDF"));
            Check("PDF bildirim ve cikti klasoru", dialogs.Last().StartsWith("PDF oluşturuldu") &&
                openedFolder == PortablePaths.Outputs && Directory.GetFiles(openedFolder, "*.pdf").Length == 1);

            Go(1);
            SelectTab(1);
            Check("Surekli kayit ayri turde", Tabs().SelectedIndex == 2);
            Check("Surekli gorevde uc kart ayni anda gorunur",
                Descendants<TextBlock>(page).Any(t => t.Text == "1 · Kişi") &&
                Descendants<TextBlock>(page).Any(t => t.Text == "2 · Atama Ve Güzergâh") &&
                Descendants<TextBlock>(page).Any(t => t.Text == "3 · Sonuç") &&
                Descendants<DatePicker>(page).Any());
            window.Width = 1700; window.Height = 900; window.UpdateLayout();
            var firstCardTitle = Descendants<TextBlock>(page).Single(t => t.Text == "1 · Kişi");
            var middleCardTitle = Descendants<TextBlock>(page).Single(t => t.Text == "2 · Atama Ve Güzergâh");
            var lastCardTitle = Descendants<TextBlock>(page).Single(t => t.Text == "3 · Sonuç");
            var firstPoint = firstCardTitle.TranslatePoint(new Point(), page);
            var middlePoint = middleCardTitle.TranslatePoint(new Point(), page);
            var lastPoint = lastCardTitle.TranslatePoint(new Point(), page);
            Check("Uc kart ayni satirda soldan saga dizilir",
                firstPoint.X + 150 < middlePoint.X && middlePoint.X + 150 < lastPoint.X &&
                Math.Abs(firstPoint.Y - middlePoint.Y) < 30 && Math.Abs(middlePoint.Y - lastPoint.Y) < 30);
            Capture("Surekli_Uc_Kart", 1700, 900);
            window.Width = 1024; window.Height = 768; window.UpdateLayout();
            var people = Descendants<ComboBox>(page).Single(c => AutomationProperties.GetName(c) == "Personel seçimi");
            people.SelectedIndex = 1;
            window.UpdateLayout();
            Check("Kayitli kisi diger sekmede tekrar kullanilir", Box("Ad soyad").Text == "SENTETİK PERSONEL");
            Click(FindButton("Kişiyi kaydet"));
            Check("Surekli panelde mesafe var", Box("Kaynaklı kilometre").IsEnabled);
            Check("Hesap oncesi sonuc yer tutucu gorunur",
                Descendants<TextBlock>(page).Any(t => t.Text.Contains("Sonucu hesapla düğmesine basın")) &&
                !Descendants<Button>(page).Any(b => Equals(b.Content, "Personel taşıt gideri ekle")));
            SelectPlace("Çıkış", "ADANA", "ALADAĞ");
            SelectPlace("Varış", "ADANA", "CEYHAN");
            Check("KGM otomatik mesafeyi ve dayanagi doldurur", Box("Kaynaklı kilometre").Text == "84" &&
                Box("KGM / mesafe kanıtı").Text.Contains("KGM İlçeler Arası Mesafe"));
            failKgm = true;
            SelectPlace("Varış", "ADANA", "KOZAN");
            Check("KGM baglanti hatasinda elle giris acik", dialogs.Last().Contains("elle girin") &&
                Box("Kaynaklı kilometre").IsEnabled && Box("KGM / mesafe kanıtı").Text == "");
            Check("Es nakli iki secenekli acilir liste", Descendants<ComboBox>(page).Single(c =>
                AutomationProperties.GetName(c) == "Eşin aynı güzergâhta memur olarak nakli").Items.Count == 2);
            Check("Surekli panelde tek tarih secicisi", Descendants<DatePicker>(page).Single().SelectedDate is null &&
                AutomationProperties.GetName(Descendants<DatePicker>(page).Single()) == "Atama / işlem tarihi");
            Check("Surekli panelde gecici saat alani yok", !Descendants<ComboBox>(page).Any(x => AutomationProperties.GetName(x).StartsWith("Ayrılış tarihi")));
            Check("Surekli panelde seyahat adayi yok", !Descendants<Button>(page).Any(x => Equals(x.Content, "Seyahat adaylarını göster")));
            failKgm = false;
            SelectPlace("Varış", "ADANA", "CEYHAN");
            Descendants<DatePicker>(page).Single().SelectedDate = new DateTime(2026, 6, 15);
            Pick("Memuriyet mahalli değerlendirmesi", DutyStationAssessment.Outside);
            Set("Kaynaklı kilometre", "84");
            Set("KGM / mesafe kanıtı", "SENTETİK MESAFE DAYANAĞI");
            Set("Hak sahibi aile ferdi sayısı (personel hariç)", "1");
            Set("Görev oluru / atama kararı referansı", "SENTETİK ATAMA");
            Set("Memuriyet mahalli kanıtı", "SENTETİK MAHAL");
            Set("İnceleyen beyanı / kayıt referansı", "SENTETİK İNCELEME");
            Review("Görev oluru incelendi");
            Review("Mahalliyet incelendi");
            Review("Aile hak sahipliği incelendi");
            var priorDialogCount = dialogs.Count;
            Click(FindButton("Sonucu hesapla"));
            if (dialogs.Count != priorDialogCount)
                throw new InvalidOperationException("Sürekli görev ön hesabı açılamadı: " + dialogs.Last());
            var optional = Descendants<Expander>(page).Single(e => Equals(e.Header, "Taşıt, Avans Ve PDF"));
            Check("Sonuc ucuncu kartta ve gider alani varsayilan kapali", !optional.IsExpanded &&
                Descendants<TextBlock>(page).Any(t => t.Text == "29.070,00 TL") &&
                repository.Load().Duties.Count == 1);
            optional.IsExpanded = true;
            window.UpdateLayout();
            Check("Surekli gorevde personel ve aile tasit girisi var", FindButton("Personel taşıt gideri ekle") is not null &&
                FindButton("Aile taşıt gideri ekle") is not null);
            var currentForm = FindButton("Yer değiştirmeyi giderlere aktar");
            Click(FindButton("Personel taşıt gideri ekle"));
            Click(FindButton("Aile taşıt gideri ekle"));
            var expenseEditors = Descendants<Expander>(page).Where(e => AutomationProperties.GetName(e).StartsWith("Gider L")).ToArray();
            Check("Tasiti eklemek formu yeniden acmaz", ReferenceEquals(currentForm, FindButton("Yer değiştirmeyi giderlere aktar")) && expenseEditors.Length == 2);
            Check("Personel ve aile tasit giderleri ayridir", expenseEditors[0].Header.ToString()!.StartsWith("Personel Taşıt") &&
                expenseEditors[1].Header.ToString()!.StartsWith("Aile Taşıt"));
            var invalidAmount = Descendants<TextBox>(expenseEditors[0]).Single(t => AutomationProperties.GetName(t) == "Gider tutarı (TL)");
            invalidAmount.Text = "hatalı";
            invalidAmount.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Click(Descendants<Button>(expenseEditors[0]).Single(b => Equals(b.Content, "Bu gideri kaldır")));
            Check("Gider kendi dugmesiyle kaldirilir", !Descendants<Expander>(page).Contains(expenseEditors[0]) &&
                Descendants<Expander>(page).Contains(expenseEditors[1]));
            Click(Descendants<Button>(expenseEditors[1]).Single(b => Equals(b.Content, "Bu gideri kaldır")));
            Check("Son gider kaldirilinca bos durum gosterilir", Descendants<TextBlock>(page).Any(t => t.Text == "Henüz gider eklenmedi." && t.IsVisible));
            Click(FindButton("Yer değiştirmeyi giderlere aktar"));
            Click(FindButton("Yer değiştirmeyi giderlere aktar"));
            var relocationRows = Descendants<Expander>(page).Where(x => AutomationProperties.GetName(x).StartsWith("Gider OTO-YD-")).ToArray();
            Check("Yer degistirme tek satira aktarilir", relocationRows.Length == 1 &&
                relocationRows[0].Header.ToString()!.Contains("29.070,00 TL"));
            Click(FindButton("Kaydet"));
            Check("Aktarilan satir kaydedilir", repository.Load().Duties.Single(d => d.Kind == DraftKind.PermanentDuty)
                .Expenses.Single().Amount == 29070m);
            Click(FindButton("Sil"));
            Go(1);
            SelectTab(0);
            Check("Module donuste gecici kayit korunur", repository.Load().Duties.Single().Kind == DraftKind.TemporaryDuty);

            Go(3);
            var editors = Descendants<Button>(page).Where(x => Equals(x.Content, "Düzenle")).ToArray();
            Check("Uc sabit karti ve kilitli alanlar", editors.Length == 3 && Descendants<TextBox>(page).All(x => x.IsReadOnly));
            var lunch = Box("Öğle saati");
            Click(editors[0]);
            Check("Sadece secili sabit karti acilir", !lunch.IsReadOnly && Box("Kişi gündelik katsayısı").IsReadOnly);
            lunch.Text = "14";
            var cancel = Descendants<Button>(page).Single(x => Equals(x.Content, "Vazgeç") && x.Visibility == Visibility.Visible);
            Click(cancel);
            Check("Vazgec sabit degisikligini geri alir", lunch.Text == "13" && lunch.IsReadOnly);
            Click(editors[0]);
            lunch.Text = "25";
            Click(editors[0]);
            Check("Gecersiz sabit diske yazilmaz", repository.Load().Constants.Temporary.LunchHour == 13 && !lunch.IsReadOnly);
            lunch.Text = "14";
            Click(editors[0]);
            Check("Gecerli sabit diske yazilir", repository.Load().Constants.Temporary.LunchHour == 14 && lunch.IsReadOnly);
            Go(2);
            Check("Sabit degisince eski sonuc PDF vermez", !FindButton("PDF").IsEnabled);
            Check("Sabit degisince kayit sonucu korunur", repository.Load().Duties.Single().Snapshot?.Amount == saved.Snapshot?.Amount);
            Click(FindButton("Sil"));
            Check("Onayli silme diske yazilir", repository.Load().Duties.Count == 0);
            Go(5);
            Check("Kaynaklar son menude ve ayri PDF dugmeleri var", Equals(navigation.Items[5], "Kaynaklar") &&
                Descendants<Button>(page).Count(x => Equals(x.Content, "PDF")) >= 3);
            Go(4);
            Descendants<ComboBox>(page).Single().SelectedIndex = 2;
            Click(FindButton("Pencere profilini uygula"));
            Check("En buyuk profil pencereyi buyutur", window.WindowState == WindowState.Maximized);
            window.Hide();
            Console.WriteLine($"TOPLAM: {passed}/{passed} WPF KONTROLU BASARILI; goruntuler: {output}"); return 0;
        }
        finally { Directory.Delete(root, true); }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
