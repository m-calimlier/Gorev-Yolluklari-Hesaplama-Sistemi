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
    // Bir önceki test dereceyi 1'e değiştirdi. 850 TL gündelik bekleyen yeni
    // senaryo, 2026 H-Cetveli B-e (5. derece) ile başlamalı.
    person.Grade = 5;
    // Yeni hesaplama PDF yolunda inceleme yapıldı beyanı, bağımsız brüt ve
    // bağımsız net toplamları gerekmez; yanlış hesap satırları yine bloke olur.
    duty = DutyRecord.CreateDemo(person.Id);
    duty.AssignmentReviewed = false; duty.StationReviewed = false;
    duty.AssignmentEvidence = ""; duty.StationEvidence = ""; duty.Reviewer = "";
    var basic = service.PreviewForCalculation(person, duty);
    Check("Belge kontrolu olmadan ön hesap yapılır", basic.Amount == 850m * 2m / 3m);
    duty.Expenses.Add(new ExpenseEntry
    {
        LineId = "OTO-GD-TEST", Kind = DraftLineKind.DailyAllowance,
        Description = "Gündelik", Amount = 566.67m, Evidence = "", LegalBasis = "6245 m.39"
    });
    duty.Snapshot = service.PreviewForCalculation(person, duty);
    var direct = service.PrepareCalculationPdf(person, duty);
    Check("Yolluk PDF'si belge onayi ve tekrar toplam olmadan hazir",
        direct.MayPrintDraft && direct.Document is { IsCalculationOutput: true } &&
        !direct.Document.IsOfficialPaymentDocument);
    duty.Expenses[0].Amount = 600m;
    duty.Snapshot = service.PreviewForCalculation(person, duty);
    Check("Yanlis gundelik yazilirsa PDF reddedilir", !service.PrepareCalculationPdf(person, duty).MayPrintDraft);
    var quickPerson = new PersonnelRecord
    {
        FullName = "AD SOYAD BELİRTİLMEDİ", Grade = 5, Indicator = 0,
        Group = GorevYolluklari.Core.Tariffs.PersonnelTariffGroup.GeneralCivilServant
    };
    var quickDuty = new DutyRecord
    {
        Kind = DraftKind.TemporaryDuty,
        QuickTemporaryEnabled = true, PersonnelId = quickPerson.Id,
        DocumentNumber = "GEC-KISA-2026",
        Origin = "GÜZERGÂH BELİRTİLMEDİ", Destination = "GÜZERGÂH BELİRTİLMEDİ",
        Departure = new DateTime(2026, 1, 1), Return = new DateTime(2026, 1, 1),
        QuickOutboundFare = 120m, QuickOutboundThirds = 2, QuickOutboundTravelDays = 1,
        QuickReturnFare = 100m, QuickReturnThirds = 1, QuickReturnTravelDays = 1,
        QuickWorkDays = 3, QuickLodgingNights = 2, QuickLodgingDailyFare = 900m,
        QuickOtherExpenses = 50m
    };
    var quickPreview = service.PreviewForCalculation(quickPerson, quickDuty);
    Check("Yeni kisa gecici 2026 servis sonucu 5470", quickPreview.Amount == 5470m &&
        quickPreview.Components?.Count == 7);
    var quickRows = new[]
    {
        ("GQ", DraftLineKind.TicketOrTravel, 120m, "6245 m.14, m.27"),
        ("RQ", DraftLineKind.TicketOrTravel, 100m, "6245 m.14, m.27"),
        ("TQ", DraftLineKind.DailyAllowance, 3400m, "6245 m.39, m.42, m.43"),
        ("LQ", DraftLineKind.Accommodation, 1800m, "6245 m.33(d)"),
        ("OQ", DraftLineKind.OtherEligible, 50m, "6245 m.14")
    };
    foreach (var (key, kind, amount, basis) in quickRows)
        quickDuty.Expenses.Add(new ExpenseEntry
        {
            LineId = "OTO-" + key + "-" + quickDuty.Id.ToString("N"),
            Kind = kind, Amount = amount, Description = key + " 2026 hesap", LegalBasis = basis
        });
    quickDuty.Snapshot = service.PreviewForCalculation(quickPerson, quickDuty);
    var quickPdf = service.PrepareCalculationPdf(quickPerson, quickDuty);
    Check("Kisa gecici PDF dogrudan olusur ve tarih uydurulmaz",
        quickPdf.MayPrintDraft && quickPdf.Document is { IsQuickTemporaryCalculation: true } &&
        quickPdf.Document.GrossTry == 5470m && !quickPdf.Document.IsOfficialPaymentDocument);
    quickDuty.Expenses.Single(x => x.Kind == DraftLineKind.OtherEligible).Amount = 80m;
    quickDuty.Snapshot = service.PreviewForCalculation(quickPerson, quickDuty);
    Check("Kisa gecici kaynak disi tutar farkini reddeder",
        !service.PrepareCalculationPdf(quickPerson, quickDuty).MayPrintDraft);

    var constants = new CalculationConstants
    {
        Temporary = new(MealDivisor: 4),
        Permanent = new(SelfDays: 21),
        Common = new(RateE: 900)
    };
    var simulation = new TravelWorkflowService(tariffPath, constants);
    person.Grade = 5;
    duty.Snapshot = simulation.PreviewForCalculation(person, duty);
    Check("Sabitler gecici motor sonucuna uygulanir", duty.Snapshot.Amount == 450m &&
        duty.Snapshot.DailyRate == 900m);
    Check("Simulasyon kendi surumuyle guncel", simulation.IsCurrent(person, duty) && !service.IsCurrent(person, duty));
    Check("Simulasyon kaydinda PDF kapali", !simulation.PrepareDraft(person, duty).MayPrintDraft);
    var permanentDuty = DutyRecord.CreateDemo(person.Id);
    permanentDuty.Kind = DraftKind.PermanentDuty;
    permanentDuty.AssignmentReviewed = permanentDuty.StationReviewed = permanentDuty.FamilyReviewed = true;
    permanentDuty.Reviewer = "SENTETİK";
    permanentDuty.DistanceEvidence = "SENTETİK";
    permanentDuty.Kilometers = 100;
    permanentDuty.FamilyCount = 1;
    permanentDuty.Return = default;
    var permanent = simulation.Preview(person, permanentDuty);
    Check("Surekli gorev bitis tarihi gerektirmez", permanent.Amount is not null);
    Check("Sabitler surekli motor sonucuna uygulanir", permanent.Amount == 32400m && permanent.Components?.Count == 3);
    permanentDuty.Snapshot = permanent;
    var otherTemporary = new TravelWorkflowService(tariffPath, constants with { Temporary = new() });
    Check("Gecici sabit surekli sonucun surumunu etkilemez", otherTemporary.IsCurrent(person, permanentDuty));
    Console.WriteLine($"TOPLAM: {passed}/{passed} IS AKISI KONTROLU BASARILI");
    return 0;
}
finally { Directory.Delete(root, true); }
