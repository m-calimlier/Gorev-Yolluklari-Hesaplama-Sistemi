using GorevYolluklari.Core.Integration;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Kullanim: dotnet run --project ... -- <dogrulanmis-2026-tarife-json>");
    return 2;
}
var engine = new MultiDayDutyPreviewEngine2026(DailyRateTariff2026.FromVerifiedJson(args[0]));
var tests = new List<(string Label, Action Check)>();
var d = new DateOnly(2026, 3, 1);

ReviewedDutyDay Day(int offset = 0, int? order = null, decimal? receipt = null, string? evidence = null) =>
    new(d.AddDays(offset), order, receipt, evidence);
MultiDayDutyRequest2026 Req(int prior = 0, IReadOnlyList<ReviewedDutyDay>? days = null) =>
    new(new PersonnelTariffInput(d, PersonnelTariffGroup.GeneralCivilServant, 5, 0),
        DutyStationAssessment.Outside, true, true, true, true, true, true,
        prior, days ?? new[] { Day() }, "OLUR-1", "MAHALLI-1", "ONCEKI-GUNLER-1");
void Test(string label, Action check) => tests.Add((label, check));
MultiDayPreviewResult Run(MultiDayDutyRequest2026 request) => engine.Preview(request);
void Status(MultiDayDutyRequest2026 request, MultiDayPreviewStatus expected)
{
    var got = Run(request);
    if (got.Status != expected) throw new Exception($"Beklenen {expected}; bulunan {got.Status}: {got.Explanation}");
}
void Near(decimal actual, decimal expected, decimal tolerance = 0.000001m)
{
    if (Math.Abs(actual - expected) > tolerance) throw new Exception($"Beklenen {expected}, bulunan {actual}");
}
void Amount(MultiDayDutyRequest2026 request, decimal daily, decimal lodging)
{
    var r = Run(request);
    if (r.Status != MultiDayPreviewStatus.Preliminary) throw new Exception("On hesap yok: " + r.Explanation);
    Near(r.PerDiemTotalUnrounded!.Value, daily);
    Near(r.LodgingTotalUnrounded!.Value, lodging);
    Near(r.CombinedTotalUnrounded!.Value, daily + lodging);
}

Test("1. gun tam", () => Amount(Req(), 850m, 0m));
Test("2 gun tam", () => Amount(Req(days: new[] { Day(), Day(1) }), 1700m, 0m));
Test("90. gun tam", () => Amount(Req(89), 850m, 0m));
Test("91. gun 2/3", () => Amount(Req(90), 850m * 2m / 3m, 0m));
Test("180. gun 2/3", () => Amount(Req(179), 850m * 2m / 3m, 0m));
Test("90/91 gecisi", () => Amount(Req(89, new[] { Day(), Day(1) }), 850m + 850m * 2m / 3m, 0m));
Test("Ara verme sayaci sifirlamaz", () => Amount(Req(90, new[] { Day(), Day(10) }), 850m * 4m / 3m, 0m));
Test("181. gun blok", () => Status(Req(180), MultiDayPreviewStatus.NeedsReview));
Test("179-181 sinir asimi blok", () => Status(Req(179, new[] { Day(), Day(1) }), MultiDayPreviewStatus.NeedsReview));
Test("1-10 konaklama %160", () => Amount(Req(days: new[] { Day(order: 1, receipt: 2500m, evidence: "F-1") }), 850m, 1360m));
Test("10. gun konaklama %160", () => Amount(Req(9, new[] { Day(order: 10, receipt: 2500m, evidence: "F-10") }), 850m, 1360m));
Test("11. gun konaklama %50", () => Amount(Req(10, new[] { Day(order: 11, receipt: 900m, evidence: "F-11") }), 850m, 425m));
Test("90. gun konaklama %50", () => Amount(Req(89, new[] { Day(order: 90, receipt: 900m, evidence: "F-90") }), 850m, 425m));
Test("91. gun konaklama mustehak %40", () => Amount(Req(90, new[] { Day(order: 91, receipt: 900m, evidence: "F-91") }), 850m * 2m / 3m, 850m * 2m / 3m * 40m / 100m));
Test("180. gun konaklama mustehak %40", () => Amount(Req(179, new[] { Day(order: 180, receipt: 900m, evidence: "F-180") }), 850m * 2m / 3m, 850m * 2m / 3m * 40m / 100m));
Test("Fatura ust sinirdan az", () => Amount(Req(days: new[] { Day(order: 1, receipt: 125m, evidence: "F-125") }), 850m, 125m));
Test("Fatura 0 TL", () => Amount(Req(days: new[] { Day(order: 1, receipt: 0m, evidence: "F-0") }), 850m, 0m));
Test("Konaklama yok", () => Amount(Req(), 850m, 0m));
Test("Eksik konaklama tutari blok", () => Status(Req(days: new[] { Day(order: 1, evidence: "F-1") }), MultiDayPreviewStatus.NeedsInput));
Test("Eksik fatura referansi blok", () => Status(Req(days: new[] { Day(order: 1, receipt: 100m) }), MultiDayPreviewStatus.NeedsInput));
Test("Negatif fatura blok", () => Status(Req(days: new[] { Day(order: 1, receipt: -10m, evidence: "F-NEG") }), MultiDayPreviewStatus.NeedsInput));
Test("Yanlis konaklama sirasi blok", () => Status(Req(days: new[] { Day(order: 2, receipt: 100m, evidence: "F-2") }), MultiDayPreviewStatus.NeedsReview));
Test("Konaklama sirasi 181 blok", () => Status(Req(days: new[] { Day(order: 181, receipt: 100m, evidence: "F-181") }), MultiDayPreviewStatus.NeedsReview));
Test("Yinelenen konaklama sirasi blok", () => Status(Req(days: new[] { Day(order: 1, receipt: 100m, evidence: "F-1"), Day(1, order: 1, receipt: 100m, evidence: "F-2") }), MultiDayPreviewStatus.NeedsReview));
Test("Mükerrer tarih blok", () => Status(Req(days: new[] { Day(), Day() }), MultiDayPreviewStatus.NeedsReview));
Test("Sirasi bozuk tarihler blok", () => Status(Req(days: new[] { Day(), Day(-1) }), MultiDayPreviewStatus.NeedsReview));
Test("Bos gunler blok", () => Status(Req(days: Array.Empty<ReviewedDutyDay>()), MultiDayPreviewStatus.NeedsInput));
Test("Onceki sayac negatif blok", () => Status(Req(-1), MultiDayPreviewStatus.NeedsReview));
Test("Onceki sayac 181 blok", () => Status(Req(181), MultiDayPreviewStatus.NeedsReview));
Test("Mahalli ici blok", () => Status(Req() with { Station = DutyStationAssessment.Inside }, MultiDayPreviewStatus.NotEligibleByLocation));
Test("Mahalli bilinmiyor blok", () => Status(Req() with { Station = DutyStationAssessment.Unknown }, MultiDayPreviewStatus.NeedsReview));
Test("Mahalli belge eksik blok", () => Status(Req() with { StationEvidenceReference = "" }, MultiDayPreviewStatus.NeedsReview));
Test("Olur incelenmedi blok", () => Status(Req() with { AssignmentReviewed = false }, MultiDayPreviewStatus.NeedsReview));
Test("Olur belge eksik blok", () => Status(Req() with { AssignmentEvidenceReference = "" }, MultiDayPreviewStatus.NeedsReview));
Test("Es kisi yer is yil belirsiz blok", () => Status(Req() with { SamePersonPlaceWorkAnnualPeriodReviewed = false }, MultiDayPreviewStatus.NeedsReview));
Test("Gecmis gunler incelenmedi blok", () => Status(Req() with { PriorDaysLedgerReviewed = false }, MultiDayPreviewStatus.NeedsReview));
Test("Gecmis gunler belge eksik blok", () => Status(Req() with { PreviousDaysLedgerReference = "" }, MultiDayPreviewStatus.NeedsReview));
Test("m43 gunler incelenmedi blok", () => Status(Req() with { EligibleDutyAndTravelDaysReviewed = false }, MultiDayPreviewStatus.NeedsReview));
Test("m42 istisna incelenmedi blok", () => Status(Req() with { Article42ExceptionsExcluded = false }, MultiDayPreviewStatus.NeedsReview));
Test("33d hak sahipligi incelenmedi blok", () => Status(Req(days: new[] { Day(order: 1, receipt: 100m, evidence: "F-1") }) with { LodgingArticle33dReviewed = false }, MultiDayPreviewStatus.NeedsReview));
Test("2025 yilini 2026 tarife ile kullanma blok", () => Status(Req() with { Personnel = new PersonnelTariffInput(new DateOnly(2025, 3, 1), PersonnelTariffGroup.GeneralCivilServant, 5, 0) }, MultiDayPreviewStatus.NoApplicableYear));
Test("2027 yili blok", () => Status(Req() with { ReviewedDays = new[] { new ReviewedDutyDay(new DateOnly(2027, 1, 1)) } }, MultiDayPreviewStatus.NoApplicableYear));
Test("Ilk gun ile tarife tarihi uyusmaz blok", () => Status(Req() with { Personnel = new PersonnelTariffInput(d.AddDays(1), PersonnelTariffGroup.GeneralCivilServant, 5, 0) }, MultiDayPreviewStatus.NeedsReview));
Test("Ozel statude genel tarife yok", () => Status(Req() with { Personnel = new PersonnelTariffInput(d, PersonnelTariffGroup.SpecialOrHighOffice, 5, 0) }, MultiDayPreviewStatus.NeedsReview));
Test("Eksik derecede tarife yok", () => Status(Req() with { Personnel = new PersonnelTariffInput(d, PersonnelTariffGroup.GeneralCivilServant, null, 0) }, MultiDayPreviewStatus.NeedsInput));
Test("Derece 1 => 860", () =>
{
    var r = Run(Req() with { Personnel = new PersonnelTariffInput(d, PersonnelTariffGroup.GeneralCivilServant, 1, 0) });
    if (r.Status != MultiDayPreviewStatus.Preliminary) throw new Exception(r.Explanation);
    Near(r.DailyRate!.Value, 860m);
});
Test("Sonuc resmi belge degildir", () =>
{
    if (Run(Req()).IsOfficialPaymentDocument) throw new Exception("Yanlis resmilik isareti.");
});
Test("Yuvarlama uygulanmamis", () =>
{
    var r = Run(Req(90));
    if (r.Status != MultiDayPreviewStatus.Preliminary || r.PerDiemTotalUnrounded!.Value == 566.67m)
        throw new Exception("Kurus yuvarlama erkenden yapilmis.");
});

var failed = 0;
foreach (var (label, check) in tests)
{
    try { check(); Console.WriteLine("[OK] " + label); }
    catch (Exception error) { failed++; Console.Error.WriteLine("[HATA] " + label + ": " + error.Message); }
}
Console.WriteLine($"TOPLAM: {tests.Count - failed}/{tests.Count} KONTROL BASARILI");
Console.WriteLine("UYARI: Bu bir ON HESAPTIR; resmi bildirim, bordro veya odeme emri degildir.");
return failed == 0 ? 0 : 1;
