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
            MessageBox.Show("Veri klasörleri oluşturulamadı: " + ex.Message + "\nProgramı yazma izni olan taşınabilir bir klasöre yerleştirin.", "Dosya erişim hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        base.OnStartup(e);
    }
}
