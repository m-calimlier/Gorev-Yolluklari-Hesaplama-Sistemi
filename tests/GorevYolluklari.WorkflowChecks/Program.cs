using System.Text.Json;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Storage.Records;
using GorevYolluklari.Application;

var root = Path.Combine(Path.GetTempPath(), "YollukWorkflow_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int passed = 0;
void Check(string name, bool ok) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("[OK] " + name); }
void Throws<T>(string name, Action a) where T : Exception { try { a(); } catch (T) { Check(name, true); return; } throw new Exception("Ret bekleniyordu: " + name); }
try
{
    var repo = new JsonRecordRepository(root);
    var state = repo.Load();
    Check("Olmayan kayit ayri durum", state.Revision == 0 && state.Personnel.Count == 0);
    var person = new PersonnelRecord { FullName = "ÖRNEK PERSONEL", Title = "Öğretmen", Institution = "ÖRNEK KURUM", Grade = 5, Indicator = 0, Group = GorevYolluklari.Core.Tariffs.PersonnelTariffGroup.GeneralCivilServant };
    state.Personnel.Add(person);
    repo.Save(state, 0);
    Check("Kaydet yeniden ac", repo.Load().Personnel.Single().FullName == person.FullName && state.Revision == 1);
    Throws<InvalidOperationException>("Eski revizyon reddi", () => repo.Save(state, 0));
    var before = File.ReadAllText(repo.FilePath);
    using (var locked = new FileStream(repo.FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        Throws<IOException>("Ikinci yazici kilidi", () => repo.Save(state, 1));
    Check("Basarisiz yazma eski kaydi korur", File.ReadAllText(repo.FilePath) == before);
    state.Personnel[0].Title = "ÖRNEK UNVAN";
    repo.Save(state, 1);
    Check("Tarihli yedek", Directory.GetFiles(Path.Combine(root, "Yedekler"), "*.json").Length == 1);
    var backup = repo.CreateBackup();
    state.Personnel[0].Title = "DEĞİŞİKLİK"; repo.Save(state, 2);
    repo.Restore(backup);
    Check("Geri yukleme ve artan revizyon", repo.Load().Personnel[0].Title == "ÖRNEK UNVAN" && repo.Load().Revision == 4);
    File.WriteAllText(Path.Combine(root, "bad.json"), "{");
    before = File.ReadAllText(repo.FilePath);
    Throws<JsonException>("Bozuk yedek ret", () => repo.Restore(Path.Combine(root, "bad.json")));
    Check("Bozuk yedek mevcut kaydi korur", before == File.ReadAllText(repo.FilePath));
    var invalid = repo.Load(); invalid.SchemaVersion = 99;
    Throws<InvalidDataException>("Desteklenmeyen sema ret", () => repo.Save(invalid, invalid.Revision));
    var duplicate = repo.Load(); duplicate.Personnel.Add(duplicate.Personnel[0]);
    Throws<InvalidDataException>("Mukerrer kimlik ret", () => repo.Save(duplicate, duplicate.Revision));
    File.WriteAllText(repo.FilePath, "null");
    Throws<InvalidDataException>("Null JSON bos liste degil", () => repo.Load());
    File.WriteAllText(repo.FilePath, "{");
    Throws<JsonException>("Bozuk JSON bos liste degil", () => repo.Load());
    File.WriteAllText(repo.FilePath, "{}");
    Throws<InvalidDataException>("Eksik sema bos liste degil", () => repo.Load());
    File.WriteAllText(repo.FilePath, before);

    var row = new ExpenseEntry { LineId = "L1", Kind = DraftLineKind.DailyAllowance, Description = "ÖRNEK GİDER", Amount = 850m, Evidence = "BELGE-001", LegalBasis = "6245 m.39" };
    var pasted = TabularExchange.ParseExpenses("GUN-01\tGündelik\tÖRNEK GİDER\t850,50\tBELGE-001\t6245 m.39");
    Check("Turkce ondalik TSV", pasted.Single().Amount == 850.50m && pasted[0].LineId == "GUN-01");
    Check("DataGrid enum degeri TSV geri alinir", TabularExchange.ParseExpenses("GUN-02\tDailyAllowance\tÖRNEK\t850,00\tB\tM").Single().Kind == DraftLineKind.DailyAllowance);
    Throws<InvalidDataException>("Hatali toplu giris atomik ret", () => TabularExchange.ParseExpenses("A\tGündelik\tÖRNEK\t1,00\tB\tM\nHATALI"));
    Throws<InvalidDataException>("Formul hucre ret", () => TabularExchange.ParseExpenses("A\tGündelik\t=1+1\t1,00\tB\tM"));
    Throws<InvalidDataException>("Mukerrer satir ret", () => TabularExchange.ParseExpenses("A\tGündelik\tÖRNEK\t1,00\tB\tM\nA\tGündelik\tÖRNEK\t1,00\tB\tM"));
    var xlsx = Path.Combine(root, "ornek.xlsx");
    TabularExchange.WriteXlsx(xlsx, new[] { row });
    Check("Office olmadan XLSX gidis donus", TabularExchange.ReadXlsx(xlsx).Single().Amount == row.Amount);
    Throws<IOException>("XLSX uzerine yazma yok", () => TabularExchange.WriteXlsx(xlsx, new[] { row }));

    var tariffPath = Path.Combine(args[0], "Kaynaklar", "02_Butce_ve_Tarifeler", "2026_H_Cetveli_Dogrulanmis_OnHesap.json");
    var service = new TravelWorkflowService(tariffPath);
    var duty = DutyRecord.CreateDemo(person.Id);
    Check("Eksik seyahat saatleri aday uretmez", service.PreviewTimeline(person, duty).Status == GorevYolluklari.Core.Integration.TimelineStatus.NeedsInput);
    duty.AssignmentReviewed = true; duty.StationReviewed = true; duty.Reviewer = "ÖRNEK KONTROL";
    duty.OutboundArrival = new(2026,6,15,10,0,0); duty.WorkStarts = new(2026,6,15,11,0,0);
    duty.WorkEnds = new(2026,6,16,14,0,0); duty.InboundDeparture = new(2026,6,16,15,0,0); duty.Return = new(2026,6,16,20,0,0);
    duty.OutboundEvidence = "DEMO-GIDIS"; duty.InboundEvidence = "DEMO-DONUS"; duty.WorkTimeEvidence = "DEMO-GOREV"; duty.WorkTimesReviewed = true;
    var timeline = service.PreviewTimeline(person, duty);
    Check("Seyahat takvimi mevcut motorla 1+1 aday", timeline.OutboundArticle43CandidateUnits == 1 && timeline.InboundArticle43CandidateUnits == 1);
    Check("Seyahat adaylari hak gunlerine aktarilmaz", duty.ReviewedDays.Count == 0 && !timeline.IsReviewedEligibleDayLedger);
    duty = DutyRecord.CreateDemo(person.Id);
    duty.AssignmentReviewed = true; duty.StationReviewed = true; duty.Reviewer = "ÖRNEK KONTROL";
    var preview = service.Preview(person, duty);
    Check("Mevcut motor ayni ham sonuc", preview.Amount == 850m * 2m / 3m && preview.Category == "B_e");
    duty.Snapshot = preview;
    Check("Snapshot girdiyle eslesir", service.IsCurrent(person, duty));
    duty.Origin = "DEĞİŞTİ";
    Check("Girdi degisince eski snapshot gecersiz", !service.IsCurrent(person, duty));
    duty = DutyRecord.CreateDemo(person.Id); duty.Departure = duty.Departure.AddYears(1); duty.Return = duty.Return.AddYears(1);
    Check("2027 tarife blok", service.Preview(person, duty).Amount is null);
    duty = DutyRecord.CreateDemo(person.Id); duty.AssignmentReviewed = true; duty.StationReviewed = true; duty.Reviewer = "ÖRNEK KONTROL";
    duty.Expenses.Add(row); duty.IndependentGross = 850m; duty.IndependentNet = 850m;
    Check("Inceleme olmadan taslak yok", !service.PrepareDraft(person, duty).MayPrintDraft);
    duty.AmountsReviewed = true; duty.ReviewedAt = DateTimeOffset.UtcNow;
    duty.Snapshot = service.Preview(person, duty);
    Check("Genel tarife referansi ile taslak", service.PrepareDraft(person, duty).MayPrintDraft);
    duty.IndependentNet = 849m;
    Check("Bagimsiz toplam uyusmazligi blok", !service.PrepareDraft(person, duty).MayPrintDraft);
    duty.IndependentNet = 850m; person.Grade = 1;
    Check("Personel degisince taslak blok", !service.PrepareDraft(person, duty).MayPrintDraft);
    Console.WriteLine($"TOPLAM: {passed}/{passed} IS AKISI KONTROLU BASARILI");
    return 0;
}
finally { Directory.Delete(root, true); }
