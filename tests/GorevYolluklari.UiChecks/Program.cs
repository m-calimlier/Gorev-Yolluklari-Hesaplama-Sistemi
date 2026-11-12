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

            Check("Ana ekran veri girisiyle acilir", navigation.SelectedIndex == 1);
            Check("Iki gorev turu secilebilir", Tabs().Items.Count == 3);
            window.UpdateLayout();
            Check("Gorev basliklari kesilmez",
                Tabs().Items.OfType<TabItem>().Where(x => x.Visibility == Visibility.Visible)
                    .All(x => x.ActualHeight >= 42 && x.ActualWidth >= 120));
            SelectTab(0);
            Check("Gecici gorev ekrani acildi", Tabs().SelectedIndex == 1);
            Check("Yeniden brüt net girisi yok", !Descendants<TextBox>(page).Any(x =>
                AutomationProperties.GetName(x).StartsWith("Bağımsız")));
            Check("Belge kontrolu kutulari zorunlu ekranin disinda", !Descendants<CheckBox>(page).Any(x =>
                AutomationProperties.GetName(x) == "Görev oluru incelendi" ||
                AutomationProperties.GetName(x) == "Mahalliyet incelendi"));
            Check("Belge numarasi otomatik", !Descendants<TextBox>(page).Any(x =>
                AutomationProperties.GetName(x) == "Belge numarası"));

            Click(FindButton("Kaydet ve PDF oluştur"));
            Check("Eksik personel kaydedilmez", repository.Load().Duties.Count == 0);
            Set("Ad soyad", "SENTETİK PERSONEL");
            Pick("Statü", PersonnelTariffGroup.GeneralCivilServant);
            Pick("Derece", 5);
            Set("Ek gösterge", "0");
            Click(FindButton("Kişiyi kaydet"));
            Check("Personel tek sefer kaydedildi", repository.Load().Personnel.Count == 1);

            SelectPlace("Çıkış", "ADANA", "ALADAĞ");
            SelectPlace("Varış", "ADANA", "CEYHAN");
            void PickDate(string name, int hour)
            {
                Descendants<DatePicker>(page).Single(x => AutomationProperties.GetName(x) == name)
                    .SelectedDate = new DateTime(2026, 6, 15);
                ChoiceBox(name + " saati").SelectedIndex = hour;
                ChoiceBox(name + " dakikası").SelectedIndex = 0;
            }
            PickDate("Ayrılış tarihi", 9);
            PickDate("Dönüş tarihi", 20);
            Pick("Memuriyet mahalli değerlendirmesi", DutyStationAssessment.Outside);
            Check("Gecici gorevde ek inceleme zorunlulugu yok", !Descendants<TextBox>(page)
                .Any(x => AutomationProperties.GetName(x) == "İnceleyen beyanı / kayıt referansı"));
            window.Width = 1366; window.Height = 768; window.UpdateLayout();
            Capture("Gecici_Yolluk_Sade", 1366, 768);
            Click(FindButton("Kaydet ve PDF oluştur"));
            var temporary = repository.Load().Duties.Single();
            Check("Gecici gorev otomatik gundelik satiri", temporary.Kind == DraftKind.TemporaryDuty &&
                temporary.Expenses.Count == 1 && temporary.Expenses[0].Kind == DraftLineKind.DailyAllowance &&
                temporary.Expenses[0].Amount == 566.67m);
            Check("Gecici PDF olusturuldu", Directory.GetFiles(PortablePaths.Outputs, "*.pdf").Length == 1 &&
                openedFolder == PortablePaths.Outputs && FindButton("PDF").IsEnabled);

            Go(1);
            SelectTab(1);
            Check("Surekli gorev uc karti var",
                Descendants<TextBlock>(page).Any(x => x.Text == "1 · Kişi") &&
                Descendants<TextBlock>(page).Any(x => x.Text == "2 · Atama Ve Güzergâh") &&
                Descendants<TextBlock>(page).Any(x => x.Text == "3 · Sonuç"));
            window.Width = 1700; window.Height = 900; window.UpdateLayout();
            var firstTitle = Descendants<TextBlock>(page).Single(x => x.Text == "1 · Kişi");
            var secondTitle = Descendants<TextBlock>(page).Single(x => x.Text == "2 · Atama Ve Güzergâh");
            var thirdTitle = Descendants<TextBlock>(page).Single(x => x.Text == "3 · Sonuç");
            var p1 = firstTitle.TranslatePoint(new Point(), page);
            var p2 = secondTitle.TranslatePoint(new Point(), page);
            var p3 = thirdTitle.TranslatePoint(new Point(), page);
            Check("Uc kart gercekten yanyana", p2.X > p1.X + 150 && p3.X > p2.X + 150 &&
                Math.Abs(p1.Y - p2.Y) < 30 && Math.Abs(p2.Y - p3.Y) < 30);
            Check("Surekli gorevde belge kaniti zorunlu degil",
                !Descendants<TextBox>(page).Any(x =>
                    AutomationProperties.GetName(x) == "İnceleyen beyanı / kayıt referansı" ||
                    AutomationProperties.GetName(x) == "KGM / mesafe kanıtı"));
            Capture("Surekli_Uc_Kart_Sade", 1700, 900);
            window.Width = 1024; window.Height = 768; window.UpdateLayout();
            var boardScroll = Descendants<ScrollViewer>(page)
                .First(x => x.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible);
            Check("Dar pencerede yatay kaydirma gercekten etkin",
                boardScroll.ScrollableWidth > 0);
            boardScroll.ScrollToHorizontalOffset(210);
            window.UpdateLayout();
            Check("Yatay kaydirma cubugu hareket ediyor", boardScroll.HorizontalOffset > 0);
            window.Width = 1700; window.Height = 900; window.UpdateLayout();
            var people = ChoiceBox("Personel seçimi");
            people.SelectedIndex = 1;
            window.UpdateLayout();
            Click(FindButton("Kişiyi kaydet"));
            SelectPlace("Çıkış", "ADANA", "ALADAĞ");
            SelectPlace("Varış", "ADANA", "CEYHAN");
            Descendants<DatePicker>(page).Single(x => AutomationProperties.GetName(x) == "Atama / işlem tarihi")
                .SelectedDate = new DateTime(2026, 6, 15);
            Pick("Memuriyet mahalli değerlendirmesi", DutyStationAssessment.Outside);
            Set("Hak sahibi aile ferdi sayısı (personel hariç)", "1");
            Set("Kaynaklı kilometre", "84");
            Click(FindButton("Sonucu hesapla"));
            Check("Surekli hesap sonucu ucuncu kartta", Descendants<TextBlock>(page)
                .Any(x => x.Text == "29.070,00 TL"));
            Check("Elle gider aktarmak gerekmiyor", !Descendants<Button>(page)
                .Any(x => Equals(x.Content, "Yer değiştirmeyi giderlere aktar")));
            Click(FindButton("Kaydet ve PDF oluştur"));
            var permanent = repository.Load().Duties.Single(x => x.Kind == DraftKind.PermanentDuty);
            Check("Surekli yer degistirme gideri otomatik", permanent.Expenses.Count == 1 &&
                permanent.Expenses[0].Kind == DraftLineKind.Relocation &&
                permanent.Expenses[0].Amount == 29070m);
            Check("Surekli PDF olusturuldu", Directory.GetFiles(PortablePaths.Outputs, "*.pdf").Length == 2 &&
                FindButton("PDF").IsEnabled);
            Check("Onay beyanı uydurulmadi", !permanent.AmountsReviewed &&
                !permanent.AssignmentReviewed && !permanent.StationReviewed);
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
