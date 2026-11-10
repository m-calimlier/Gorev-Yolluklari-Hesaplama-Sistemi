using System.IO;
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
            var window = new MainWindow(); window.Show();
            var navigation = (ListBox)window.FindName("Navigation");
            Check("Yedi gezinme bolumu", navigation.Items.Count == 7);
            var demo = Descendants<Button>(window).Single(x => Equals(x.Content, "Sentetik demo kayıtlarını ekle"));
            demo.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("Demo gecici gorev ekranini acar", navigation.SelectedIndex == 1);
            foreach (var size in new[] { (1024d, 768d), (1366d, 768d), (1920d, 1080d) })
            {
                window.Width = size.Item1; window.Height = size.Item2;
                for (var i = 0; i < 7; i++)
                {
                    navigation.SelectedIndex = i;
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                    Check($"{size.Item1}x{size.Item2} bolum {i} acilir", ((StackPanel)window.FindName("Page")).Children.Count > 0);
                    var bitmap = new RenderTargetBitmap((int)size.Item1, (int)size.Item2, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var file = File.Create(Path.Combine(output, $"UI_{size.Item1}_{i}.png")); encoder.Save(file);
                }
            }
            navigation.SelectedIndex = 1;
            Descendants<Button>(window).Single(x => Equals(x.Content, "Ön hesapla")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("Onaysiz demo tutar uretmez", ((TextBlock)window.FindName("Status")).Text.Contains("HESAP BLOKE"));
            var vm = (GorevYolluklari.App.ViewModels.MainViewModel)window.DataContext;
            var other = GorevYolluklari.Core.Records.DutyRecord.CreateDemo(vm.Personnel[0].Id);
            var preserved = vm.SelectedDuty!.Snapshot; other.Snapshot = preserved; vm.Duties.Add(other);
            vm.SelectedDuty.AssignmentReviewed = true; vm.SelectedDuty.StationReviewed = true;
            var origin = Descendants<TextBox>(window).Single(x => System.Windows.Automation.AutomationProperties.GetName(x) == "Çıkış yeri");
            origin.Text = "ÖRNEK DEĞİŞEN YER"; origin.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Check("Bir gorev degisince diger snapshot korunur", preserved is not null && other.Snapshot == preserved);
            Check("Guzergah degisince ilgili incelemeler yenilenir", !vm.SelectedDuty.AssignmentReviewed && !vm.SelectedDuty.StationReviewed);
            Descendants<Button>(window).Single(x => Equals(x.Content, "Kaydet")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check("UI kaydi JSON olusur", File.Exists(Path.Combine(PortablePaths.Data, "kayitlar-v1.json")));
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
