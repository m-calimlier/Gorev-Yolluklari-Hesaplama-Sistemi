using System.Text.Json;
using GorevYolluklari.Core.Records;
namespace GorevYolluklari.Storage.Records;
public sealed class JsonRecordRepository
{
    private readonly string _backups;
    public string FilePath { get; }
    public JsonRecordRepository(string root)
    {
        root = Path.GetFullPath(root);
        FilePath = Path.Combine(root, "Veriler", "kayitlar-v1.json");
        _backups = Path.Combine(root, "Yedekler");
    }
    public WorkspaceRecords Load()
    {
        var data = File.Exists(FilePath) ? ReadFile(FilePath) : new WorkspaceRecords();
        Validate(data); return data;
    }
    private static WorkspaceRecords ReadFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var json = JsonDocument.Parse(stream);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("SchemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number ||
            !root.TryGetProperty("Revision", out var revision) || revision.ValueKind != JsonValueKind.Number ||
            !root.TryGetProperty("Personnel", out var people) || people.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("Duties", out var duties) || duties.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("JSON kayıt şeması eksik; boş kayıt olarak kabul edilmedi.");
        return root.Deserialize<WorkspaceRecords>() ?? throw new InvalidDataException("JSON kayıt boş.");
    }
    public void Save(WorkspaceRecords data, int expectedRevision)
    {
        Validate(data);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        using var guard = new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var current = Load();
        if (current.Revision != expectedRevision || data.Revision != expectedRevision)
            throw new InvalidOperationException("Kayıt başka bir işlemde değişti. Yeniden açın; mevcut kayıt ezilmedi.");
        if (File.Exists(FilePath)) BackupUnlocked();
        var next = JsonSerializer.Deserialize<WorkspaceRecords>(JsonSerializer.Serialize(data))!;
        next.Revision = checked(expectedRevision + 1); next.UpdatedAt = DateTimeOffset.UtcNow;
        JsonFileStore.Save(FilePath, next);
        data.Revision = next.Revision; data.UpdatedAt = next.UpdatedAt;
    }
    public string CreateBackup()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        using var guard = new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(FilePath)) throw new FileNotFoundException("Henüz kaydedilmiş veri yok.");
        _ = Load(); return BackupUnlocked();
    }
    private string BackupUnlocked()
    {
        Directory.CreateDirectory(_backups);
        var target = Path.Combine(_backups, "kayitlar_" + DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N")[..8] + ".json");
        File.Copy(FilePath, target, false); return target;
    }
    public void Restore(string backupPath)
    {
        var restored = ReadFile(backupPath);
        Validate(restored);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        using var guard = new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        int revision;
        try { revision = Load().Revision; }
        catch (Exception ex) when (ex is JsonException or InvalidDataException) { revision = restored.Revision; }
        if (File.Exists(FilePath)) BackupUnlocked();
        restored.Revision = checked(Math.Max(revision, restored.Revision) + 1);
        restored.UpdatedAt = DateTimeOffset.UtcNow;
        JsonFileStore.Save(FilePath, restored);
    }
    private static void Validate(WorkspaceRecords data)
    {
        if (data.SchemaVersion != 1 || data.Revision < 0 || data.Personnel is null || data.Duties is null)
            throw new InvalidDataException("Kayıt şeması desteklenmiyor veya eksik. Dosya değiştirilmedi.");
        if (data.Personnel.Any(x => x is null || x.Id == Guid.Empty || string.IsNullOrWhiteSpace(x.FullName)) ||
            data.Personnel.Select(x => x.Id).Distinct().Count() != data.Personnel.Count ||
            data.Duties.Any(x => x is null || x.Id == Guid.Empty || !data.Personnel.Any(p => p.Id == x.PersonnelId) || x.Expenses is null || x.ReviewedDays is null) ||
            data.Duties.Select(x => x.Id).Distinct().Count() != data.Duties.Count)
            throw new InvalidDataException("Eksik, mükerrer veya personeli bulunmayan kayıt. Dosya değiştirilmedi.");
    }
}
