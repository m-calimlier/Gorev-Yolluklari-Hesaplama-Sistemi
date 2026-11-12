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
using System.Windows.Documents;
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
            var headerTitles = Descendants<TextBlock>(window).Where(x =>
                x.Text == "GÖREV YOLLUKLARI HESAPLAMA CETVELİ").ToArray();
            Check("Baslik tek font boyutuyla altin vurguda",
                headerTitles.Length == 1 && headerTitles[0].FontSize >= 23 &&
                headerTitles[0].FontWeight == FontWeights.Bold);
            Check("Sol altta Created by Murat ÇALIMLIER imzasi var",
                Descendants<TextBlock>(window).Any(x =>
                    x.Text == "Created by Murat ÇALIMLIER" && x.FontWeight == FontWeights.Bold));

            Check("Iki gorev turu secilebilir", Tabs().Items.Count == 3);
            window.UpdateLayout();
            Check("Gorev basliklari kesilmez",
                Tabs().Items.OfType<TabItem>().Where(x => x.Visibility == Visibility.Visible)
                    .All(x => x.ActualHeight >= 42 && x.ActualWidth >= 120));
            SelectTab(0);
            Check("Gecici gorev ekrani acildi", Tabs().SelectedIndex == 1);
            Check("Ek Ders tasarimi tum temel kaynaklari yuklendi",
                window.TryFindResource("HakedisWorkspaceCard") is Style &&
                window.TryFindResource("HakedisFieldText") is Style &&
                window.TryFindResource("HakedisFieldCombo") is Style &&
                window.TryFindResource("HakedisDatePicker") is Style &&
                window.TryFindResource("HakedisMetricCard") is Style &&
                window.TryFindResource("HakedisPrimaryAction") is Style &&
                window.TryFindResource("HakedisSecondaryAction") is Style);
            Check("Ek Ders giris karti ve buton stili etkin",
                Descendants<Border>(page).Any(x =>
                    ReferenceEquals(x.Style, window.FindResource("HakedisWorkspaceCard"))) &&
                ReferenceEquals(FindButton("Hesapla").Style, window.FindResource("HakedisPrimaryAction")));

            window.Width = 1700; window.Height = 900; window.UpdateLayout();
            var tempTitles = new[] { "1 · Bütçe Ve Tarife", "2 · Gidiş Ve Dönüş", "3 · Görev Ve Konaklama" }
                .Select(title => Descendants<TextBlock>(page).First(x => x.Text == title && x.FontSize >= 18))
                .Select(x => x.TranslatePoint(new Point(), page)).ToArray();
            Check("Referans gecici gorev uc kart ayni satirda",
                tempTitles[0].X + 150 < tempTitles[1].X &&
                tempTitles[1].X + 150 < tempTitles[2].X &&
                Math.Abs(tempTitles[0].Y - tempTitles[1].Y) < 30 &&
                Math.Abs(tempTitles[1].Y - tempTitles[2].Y) < 30);
            Capture("Gecici_Referans_2026", 1700, 900);
            window.Width = 1366; window.Height = 768; window.UpdateLayout();
            Check("Gecici kisa ekranda belge/atama/guzergah kontrolu yok",
                !Descendants<DatePicker>(page).Any() &&
                !Descendants<CheckBox>(page).Any() &&
                !Descendants<TextBox>(page).Any(x =>
                    AutomationProperties.GetName(x) is "Ad soyad" or "KGM / mesafe kanıtı" or
                        "Bağımsız Brüt TL" or "Bağımsız Net TL"));
            Check("Ek Ders textbox ve combobox stili uygulandi",
                ReferenceEquals(Box("Gidiş Ulaşım Ücretleri Toplamı (TL)").Style,
                    window.FindResource("HakedisFieldText")) &&
                ReferenceEquals(ChoiceBox("Gidiş Gündelik Oranı").Style,
                    window.FindResource("HakedisFieldCombo")));
            var citation = Descendants<Label>(page)
                .Select(x => x.Content).OfType<TextBlock>()
                .SelectMany(x => x.Inlines.OfType<Hyperlink>()).FirstOrDefault();
            Check("Alanlarda tiklanabilir kanun madde dayanaklari", citation is not null);
            if (citation is not null)
            {
                citation.RaiseEvent(new RoutedEventArgs(Hyperlink.ClickEvent));
                window.UpdateLayout();
                var legalWindow = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(x => x != window && x.Title.StartsWith("Mevzuat Dayanağı"));
                Check("Ayri hukuki panelde kanundan sari birebir kisa alinti",
                    legalWindow is not null && Descendants<TextBlock>(legalWindow).Any(t =>
                        t.Background is SolidColorBrush b && b.Color.R > 220 && b.Color.G > 170 &&
                        b.Color.B < 180 && t.Text.StartsWith("… ") && t.Text.EndsWith(" …")));
                Check("Alinti ile aciklama ayri etiketlenir",
                    legalWindow is not null && Descendants<TextBlock>(legalWindow).Any(t =>
                        t.Text == "AÇIKLAMA (ALINTI DEĞİLDİR)"));
                legalWindow?.Close();
            }
            Check("Veri girisinde kaydet ve PDF butonlari yok",
                !Descendants<Button>(page).Any(x => x.Content is string t && t.Contains("PDF")));
            Click(FindButton("Hesapla"));
            Check("Eksik tarife girdisi kaydedilmez", repository.Load().Duties.Count == 0);

            Pick("Aylık Kadro Derecesi", 5);
            ChoiceBox("Ek Gösterge Grubu").SelectedValue = 0;
            Set("Gidiş Ulaşım Ücretleri Toplamı (TL)", "120");
            Pick("Gidiş Gündelik Oranı", 2);
            Set("Gidiş Yol Gündelik Süresi (gün)", "1");
            Set("Dönüş Ulaşım Ücretleri Toplamı (TL)", "100");
            Pick("Dönüş Gündelik Oranı", 1);
            Set("Dönüş Yol Gündelik Süresi (gün)", "1");
            Set("Görev Süresi (gün)", "3");
            Set("Konaklama Süresi (gece)", "2");
            Set("Günlük Konaklama Ücreti (TL)", "900");
            Set("Diğer Masraflar Toplamı (TL)", "50");
            Click(FindButton("Hesapla"));
            Check("Hesapla tiklaninca sonuc paneline gidilir",
                navigation.SelectedIndex == 2 && repository.Load().Duties.Count == 0);
            Check("Gecici 2026 kisa hesap toplam 5470 TL", Descendants<TextBlock>(page)
                .Any(x => x.Text == "5.470,00 TL"));
            Check("Sonuc panelinde tek kaydet PDF aksiyonu var",
                Descendants<Button>(page).Count(x => x.Content is string t && t.Contains("PDF")) == 1 &&
                FindButton("Düzenle") is not null && FindButton("Sil") is not null);
            Check("Kaydedilmemis hesap duzenlemeye geri donebilir", repository.Load().Duties.Count == 0);
            Click(FindButton("Düzenle"));
            Check("Duzenle tiklaninca alanlar korunur",
                navigation.SelectedIndex == 1 && Box("Görev Süresi (gün)").Text == "3" &&
                repository.Load().Duties.Count == 0);
            Click(FindButton("Hesapla"));
            Check("Giris duzeltme sonrasi sonuc tekrar acilir",
                navigation.SelectedIndex == 2 && repository.Load().Duties.Count == 0);
            Capture("Gecici_Referans_Hesaplanmis", 1366, 768);
            Click(FindButton("Kaydet / PDF oluştur"));
            var temporary = repository.Load().Duties.Single();
            Check("Kisi tekrar girilmeden otomatik kaydedildi", repository.Load().Personnel.Count == 1);
            Check("Gecici kisa hesap bes otomatik gider satiri",
                temporary.QuickTemporaryEnabled && temporary.Expenses.Count == 5 &&
                temporary.Expenses.Single(x => x.Kind == DraftLineKind.DailyAllowance).Amount == 3400m &&
                temporary.Expenses.Single(x => x.Kind == DraftLineKind.Accommodation).Amount == 1800m &&
                temporary.Expenses.Single(x => x.Kind == DraftLineKind.OtherEligible).Amount == 50m &&
                temporary.Expenses.Where(x => x.Kind == DraftLineKind.TicketOrTravel).Sum(x => x.Amount) == 220m);
            Check("Gecici hesap PDF olusturuldu",
                Directory.GetFiles(PortablePaths.Outputs, "*.pdf").Length == 1 &&
                openedFolder == PortablePaths.Outputs && FindButton("PDF oluştur").IsEnabled);

            Go(1);
            SelectTab(1);
            Check("Sürekli görev iki giris karti, sonuc ayrica",
                Descendants<TextBlock>(page).Any(x => x.Text == "1 · Tarife Ve Aile") &&
                Descendants<TextBlock>(page).Any(x => x.Text == "2 · Eski Ve Yeni Görev Yeri") &&
                !Descendants<TextBlock>(page).Any(x => x.Text == "3 · Sonuç"));
            window.Width = 1700; window.Height = 900; window.UpdateLayout();
            var firstTitle = Descendants<TextBlock>(page).First(x => x.Text == "1 · Tarife Ve Aile" && x.FontSize >= 18);
            var secondTitle = Descendants<TextBlock>(page).First(x => x.Text == "2 · Eski Ve Yeni Görev Yeri" && x.FontSize >= 18);
            var p1 = firstTitle.TranslatePoint(new Point(), page);
            var p2 = secondTitle.TranslatePoint(new Point(), page);
            Check("Iki giris karti yanyana", p2.X > p1.X + 150 &&
                Math.Abs(p1.Y - p2.Y) < 30);
            Check("Sürekli görevde gereksiz personel/onay alanı yok",
                !Descendants<TextBox>(page).Any(x =>
                    AutomationProperties.GetName(x) == "Ad soyad" ||
                    AutomationProperties.GetName(x) == "Unvan" ||
                    AutomationProperties.GetName(x) == "KGM / mesafe kanıtı" ||
                    AutomationProperties.GetName(x) == "Atama / işlem tarihi") &&
                !Descendants<CheckBox>(page).Any());
            Check("Bütçe dönemi gösterilir", Descendants<TextBlock>(page)
                .Any(x => x.Text.Contains("2026 (01 Ocak")));
            Capture("Surekli_Dokuz_Alan", 1700, 900);
            window.Width = 1024; window.Height = 768; window.UpdateLayout();
            var boardScroll = Descendants<ScrollViewer>(page)
                .First(x => x.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible);
            Check("Dar pencerede yatay kaydirma gercekten etkin",
                boardScroll.ScrollableWidth > 0);
            boardScroll.ScrollToHorizontalOffset(210);
            window.UpdateLayout();
            Check("Yatay kaydirma cubugu hareket ediyor", boardScroll.HorizontalOffset > 0);
            window.Width = 1700; window.Height = 900; window.UpdateLayout();
            Pick("Aylık Kadro Derecesi", 5);
            ChoiceBox("Ek Gösterge Grubu").SelectedValue = 0;
            Pick("Y.D.M. Değişken Unsuru", false);
            Set("Aile Ferdi Sayısı (kendiniz hariç)", "1");
            SelectPlace("Çıkış", "ADANA", "ALADAĞ");
            SelectPlace("Varış", "ADANA", "CEYHAN");
            Set("Mesafe (km)", "84");
            Set("Taşıt Ücreti (1 kişi için TL, varsa)", "250");
            Click(FindButton("Hesapla"));
            Check("Surekli hesaplamadan dogrudan sonuc paneline", navigation.SelectedIndex == 2);
            Check("Sürekli hesap toplamı 31.270 TL", Descendants<TextBlock>(page)
                .Any(x => x.Text == "31.270,00 TL"));
            Check("Sürekli yer değiştirme bileşeni doğru", Descendants<TextBlock>(page)
                .Any(x => x.Text.Contains("29.070,00 TL")));
            Check("Fazladan gider ve belge kartı yok", !Descendants<Button>(page)
                .Any(x => Equals(x.Content, "Yer değiştirmeyi giderlere aktar") ||
                          Equals(x.Content, "Kişiyi kaydet")));
            Click(FindButton("Kaydet / PDF oluştur"));
            var permanent = repository.Load().Duties.Single(x => x.Kind == DraftKind.PermanentDuty);
            var lines = permanent.Expenses;
            Check("9 alan otomatik gider satirlarina aktarildi",
                lines.Count == 5 &&
                lines.Single(x => x.Kind == DraftLineKind.Relocation).Amount == 29070m &&
                lines.Single(x => x.Kind == DraftLineKind.DailyAllowance).Amount == 850m &&
                lines.Single(x => x.Kind == DraftLineKind.FamilyDaily).Amount == 850m &&
                lines.Single(x => x.Kind == DraftLineKind.TicketOrTravel).Amount == 250m &&
                lines.Single(x => x.Kind == DraftLineKind.FamilyTravel).Amount == 250m);
            Check("Bilet alani ve gizli atama kodu dogru",
                permanent.TravelFarePerPerson == 250m &&
                permanent.Departure.Year == 2026 && permanent.Station == DutyStationAssessment.Outside);
            Check("Surekli PDF olusturuldu", Directory.GetFiles(PortablePaths.Outputs, "*.pdf").Length == 2 &&
                FindButton("PDF oluştur").IsEnabled);
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
