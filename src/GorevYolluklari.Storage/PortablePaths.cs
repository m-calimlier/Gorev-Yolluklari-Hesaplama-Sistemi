namespace GorevYolluklari.Storage;

public static class PortablePaths
{
    // Açık test/taşıma seçeneği; varsayılan kalıcı veri kökü yalnız masaüstüdür.
    public static string Root => Path.GetFullPath((File.Exists(Path.Combine(AppContext.BaseDirectory, "desktop-data.profile")) ? null : Environment.GetEnvironmentVariable("GOREV_YOLLUKLARI_ROOT")) ?? ResolveDesktopRoot(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
    public static string ResolveDesktopRoot(string desktop)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(desktop);
        return Path.Combine(Path.GetFullPath(desktop), "Görev Yollukları Hesaplama Sistemi");
    }
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
        var packaged = Path.Combine(AppContext.BaseDirectory, "Kaynaklar");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOREV_YOLLUKLARI_ROOT")) && Directory.Exists(packaged) && !Path.GetFullPath(packaged).Equals(Sources, StringComparison.OrdinalIgnoreCase))
            foreach (var file in Directory.EnumerateFiles(packaged, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                var target = Path.Combine(Sources, Path.GetRelativePath(packaged, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!File.Exists(target)) File.Copy(file, target, false);
            }
    }
}
