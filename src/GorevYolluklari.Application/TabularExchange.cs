using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;
namespace GorevYolluklari.Application;

/// <summary>Değer tabanlı, sürümlü XLSX/TSV aktarımı. Formül çalıştırmaz; Office gerekmez.</summary>
public static class TabularExchange
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] Headers = ["Satır", "Gider türü", "Açıklama", "Tutar (TL)", "Kanıt", "Mevzuat"];
    public static string KindName(DraftLineKind kind) => kind switch {
        DraftLineKind.DailyAllowance => "Gündelik", DraftLineKind.TicketOrTravel => "Yol",
        DraftLineKind.Accommodation => "Konaklama", DraftLineKind.FamilyDaily => "Aile gündeliği",
        DraftLineKind.FamilyTravel => "Aile yol", DraftLineKind.Relocation => "Yer değiştirme",
        DraftLineKind.OtherEligible => "Diğer", _ => throw new InvalidDataException("Gider türü geçersiz.") };
    public static List<ExpenseEntry> ParseExpenses(string text)
    {
        if (text.Length > 1_000_000) throw new InvalidDataException("Aktarım boyutu çok büyük.");
        var result = new List<ExpenseEntry>(); var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in text.Replace("\r", "").Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var c = raw.Split('\t');
            if (c.SequenceEqual(Headers) || c.SequenceEqual(new[] { "YOLLUK-GIDER-V1" })) continue;
            if (c.Length != 6 || c.Any(x => string.IsNullOrWhiteSpace(x) || x.TrimStart().StartsWith('=') || x.Any(char.IsControl)))
                throw new InvalidDataException($"Satır {result.Count + 1}: altı dolu değer hücresi gerekli; formül kabul edilmez.");
            var kind = Enum.GetValues<DraftLineKind>().Where(k => KindName(k) == c[1]).Cast<DraftLineKind?>().SingleOrDefault();
            if (kind is null && Enum.TryParse<DraftLineKind>(c[1], out var gridKind) && Enum.IsDefined(gridKind)) kind = gridKind;
            if (kind is null || !decimal.TryParse(c[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Tr, out var amount) ||
                amount <= 0 || amount > 100_000_000 || decimal.Round(amount, 2) != amount || !ids.Add(c[0]))
                throw new InvalidDataException($"Satır {result.Count + 1}: tür/tutar/kimlik hatalı; tutarı 850,50 biçiminde girin.");
            result.Add(new() { LineId = c[0], Kind = kind.Value, Description = c[2], Amount = amount, Evidence = c[4], LegalBasis = c[5] });
            if (result.Count > 40) throw new InvalidDataException("Bir görev en fazla 40 gider satırı içerebilir.");
        }
        if (result.Count == 0) throw new InvalidDataException("Aktarılacak gider satırı bulunamadı.");
        return result;
    }
    public static string ToTsv(IEnumerable<ExpenseEntry> rows) => string.Join("\n", rows.Select(x => string.Join('\t', Cells(x))));
    private static string[] Cells(ExpenseEntry x) => [x.LineId, KindName(x.Kind), x.Description, x.Amount.ToString("0.00", Tr), x.Evidence, x.LegalBasis];
    public static void WriteXlsx(string path, IEnumerable<ExpenseEntry> rows)
    {
        var values = rows.ToArray(); _ = ParseExpenses(ToTsv(values));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        void Entry(string name, string content) { using var writer = new StreamWriter(zip.CreateEntry(name).Open()); writer.Write(content); }
        Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
        Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        Entry("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Giderler_v1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
        Entry("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var all = new[] { new[] { "YOLLUK-GIDER-V1" }, Headers }.Concat(values.Select(Cells)).ToArray();
        var data = new XElement(ns + "sheetData", all.Select((r, i) => new XElement(ns + "row", new XAttribute("r", i + 1),
            r.Select((v, j) => new XElement(ns + "c", new XAttribute("r", ((char)('A' + j)).ToString() + (i + 1)), new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", v)))))));
        Entry("xl/worksheets/sheet1.xml", new XElement(ns + "worksheet", data).ToString());
    }
    public static List<ExpenseEntry> ReadXlsx(string path)
    {
        if (new FileInfo(path).Length > 5_000_000) throw new InvalidDataException("XLSX 5 MB sınırını aşıyor.");
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Sum(x => x.Length) > 10_000_000 || zip.Entries.Any(x => x.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Dosya boyutu veya makro içeriği desteklenmiyor.");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument Xml(string name) { using var s = (zip.GetEntry(name) ?? throw new InvalidDataException("Şablon parçası eksik: " + name)).Open(); return XDocument.Load(s); }
        var shared = zip.GetEntry("xl/sharedStrings.xml") is null ? [] : Xml("xl/sharedStrings.xml").Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToArray();
        var doc = Xml("xl/worksheets/sheet1.xml");
        if (doc.Descendants(ns + "f").Any()) throw new InvalidDataException("Formüllü hücre kabul edilmez; yalnız değer yapıştırın.");
        var lines = new List<string>();
        foreach (var r in doc.Descendants(ns + "row"))
        {
            var cells = new string[6]; Array.Fill(cells, "");
            foreach (var c in r.Elements(ns + "c"))
            {
                var reference = (string?)c.Attribute("r") ?? "";
                var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
                if (letters.Length != 1 || letters[0] < 'A' || letters[0] > 'F') throw new InvalidDataException("Şablon sütunları A–F olmalıdır.");
                var type = (string?)c.Attribute("t"); var v = c.Element(ns + "v")?.Value ?? "";
                var value = type == "s" ? shared[int.Parse(v, CultureInfo.InvariantCulture)] : type == "inlineStr" ? string.Concat(c.Descendants(ns + "t").Select(x => x.Value)) : v;
                if (type is null && letters == "D" && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)) value = number.ToString("0.############################", Tr);
                cells[letters[0] - 'A'] = value;
            }
            lines.Add(string.Join('\t', cells).TrimEnd('\t'));
        }
        if (lines.Count < 3 || lines[0] != "YOLLUK-GIDER-V1" || lines[1] != string.Join('\t', Headers)) throw new InvalidDataException("YOLLUK-GIDER-V1 şablonu ve sütun başlıkları gerekli.");
        return ParseExpenses(string.Join('\n', lines.Skip(2)));
    }
}
