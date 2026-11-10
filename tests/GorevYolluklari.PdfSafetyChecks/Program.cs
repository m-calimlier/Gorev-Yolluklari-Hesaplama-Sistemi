using System.IO;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Documents;
var dir = Path.Combine(Path.GetTempPath(), "YollukPdfSafety_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
var row = new DraftExpenseLine("L1", DraftLineKind.DailyAllowance, "ÖRNEK GİDER", 850m, "DEMO-BELGE", "6245 m.39");
var doc = DraftDocumentService.Create(new(DraftKind.TemporaryDuty, "DEMO-001", "ÖRNEK PERSONEL", "Öğretmen", "ÖRNEK KURUM", "Ankara", "Konya", new(2026, 6, 15), new(2026, 6, 15), 2026, "BUT-002:2026:I-B", "TEST", "DEMO-OLUR", "DEMO-MAHAL", true, true, new[] { row }, 0, null, 850, 850)).Document!;
int passed = 0;
void Reject(string name, Action action) { try { action(); } catch (InvalidDataException) { passed++; Console.WriteLine("[OK] " + name); return; } throw new Exception(name); }
try {
    Reject("Icmal toplam belge toplami ile uyusmali", () => TravelDraftPdfWriter.SaveSummary(new(new[] { doc }, 999, 0, 999), Path.Combine(dir, "bad.pdf")));
    Reject("Negatif net PDF ret", () => TravelDraftPdfWriter.SaveDraft(doc with { AdvanceTry = 900, NetTry = -50 }, Path.Combine(dir, "bad2.pdf")));
    Reject("Gecersiz tur PDF ret", () => TravelDraftPdfWriter.SaveDraft(doc with { Kind = (DraftKind)999 }, Path.Combine(dir, "bad3.pdf")));
    Reject("Kesirli kurus PDF ret", () => TravelDraftPdfWriter.SaveDraft(doc with { Lines = new[] { row with { AmountTry = 850.001m } }, GrossTry = 850.001m, NetTry = 850.001m }, Path.Combine(dir, "bad4.pdf")));
    Console.WriteLine($"TOPLAM: {passed}/4 PDF GUVENLIK KONTROLU BASARILI"); return 0;
} finally { Directory.Delete(dir, true); }
