using System.Windows;
using GorevYolluklari.Storage;

namespace GorevYolluklari.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try { PortablePaths.EnsureFolders(); }
        catch (Exception ex)
        {
            MessageBox.Show("Veri klasörleri oluşturulamadı: " + ex.Message + "\nMasaüstündeki Görev Yollukları Hesaplama Sistemi klasörünün yazma izinlerini kontrol edin.", "Dosya erişim hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        base.OnStartup(e);
    }
}
