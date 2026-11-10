using System.Text.Json;

namespace GorevYolluklari.Storage;

public static class JsonFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static T? Load<T>(string path)
    {
        if (!File.Exists(path)) return default;
        // Bozuk JSON, sanki hic kayit yokmus gibi gizlenmez; bir ust katman hata mesajini kullaniciya gosterir.
        using var input = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(input, JsonOptions)
            ?? throw new InvalidDataException("Boş veya geçersiz JSON kayıt: " + path);
    }

    public static void Save<T>(string path, T data)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("Dosya yolu geçersiz", nameof(path));
        Directory.CreateDirectory(directory);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, JsonOptions);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path))
            {
                // Aynı hacim üzerinde atomik değiştirme ve önceki sürüm yedeği.
                File.Replace(temp, path, path + ".bak");
            }
            else
            {
                File.Move(temp, path);
            }
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
