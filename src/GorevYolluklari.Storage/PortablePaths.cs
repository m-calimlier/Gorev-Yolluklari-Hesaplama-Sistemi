namespace GorevYolluklari.Storage;

public static class PortablePaths
{
    // Açık test/taşıma seçeneği; belirtilmezse EXE yanındaki taşınabilir kök kullanılır.
    public static string Root => Path.GetFullPath(Environment.GetEnvironmentVariable("GOREV_YOLLUKLARI_ROOT") ?? AppContext.BaseDirectory);
    public static string Data => Path.Combine(Root, "Veriler");
    public static string Backups => Path.Combine(Root, "Yedekler");
    public static string Outputs => Path.Combine(Root, "Ciktilar");
    public static string Sources => Path.Combine(Root, "Kaynaklar");

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(Backups);
        Directory.CreateDirectory(Outputs);
        Directory.CreateDirectory(Sources);
    }
}
