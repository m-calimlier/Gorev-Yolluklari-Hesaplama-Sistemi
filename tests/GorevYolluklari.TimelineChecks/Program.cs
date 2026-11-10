using GorevYolluklari.Core.Integration;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Kullanim: dotnet run --project ... -- <2026-tarife-json>");
    return 2;
}
var engine = new TravelTimelinePreviewEngine2026(DailyRateTariff2026.FromVerifiedJson(args[0]));
var tests = new List<(string Name, Action Action)>();
var baseDate = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Unspecified);
DateTime T(int day, int h, int min = 0) => baseDate.AddDays(day).AddHours(h).AddMinutes(min);
TemporaryDutyItinerary2026 Request() => new(
    new PersonnelTariffInput(new DateOnly(2026, 6, 15), PersonnelTariffGroup.GeneralCivilServant, 5, 0),
    DutyStationAssessment.Outside, true, "OLUR-26", "MAHAL-26",
    new JourneyLeg(T(0, 8), T(0, 9), "BILET-G"),
    T(0, 9), T(0, 14), "GOREV-CIZELGESI", true,
    new JourneyLeg(T(0, 15), T(0, 20), "BILET-D"));
TimelinePreviewResult Run(TemporaryDutyItinerary2026? req = null) => engine.Preview(req ?? Request());
void Test(string name, Action action) => tests.Add((name, action));
void Require(bool okay, string detail = "Beklenen durum saglanmadi") { if (!okay) throw new Exception(detail); }
void Status(TemporaryDutyItinerary2026 r, TimelineStatus expected) =>
    Require(Run(r).Status == expected, $"Beklenen {expected}, gorulen {Run(r).Status}");
void EqualsAmount(decimal? a, decimal b) => Require(a.HasValue && a.Value == b, $"Beklenen {b} bulunan {a}");
void EqualsCount(int? a, int b) => Require(a == b, $"Beklenen {b}, bulunan {a}");
void Multi(TemporaryDutyItinerary2026 r, int outward, int inward)
{
    var x = Run(r);
    Require(x.Status == TimelineStatus.NeedsReview, x.Explanation);
    EqualsCount(x.OutboundArticle43CandidateUnits, outward);
    EqualsCount(x.InboundArticle43CandidateUnits, inward);
    Require(x.SameDayPreview is null, "Cok gunlu gunluk tutari cikarilamaz");
    Require(!x.IsReviewedEligibleDayLedger && !x.IsOfficialPaymentDocument);
}
TemporaryDutyItinerary2026 TwoDay() => Request() with
{
    WorkEnds = T(1, 14),
    Inbound = new JourneyLeg(T(1, 15), T(1, 20), "BILET-D")
};

Test("Ayni gun m39 - 2/3", () => {
    var r = Run();
    Require(r.Status == TimelineStatus.PreliminarySameDay);
    EqualsAmount(r.SameDayPreview?.UnroundedAmount, 850m * 2m / 3m);
});
Test("Ayni gun m43 ayri tahakkuk edilmez", () => {
    var r = Run(); Require(r.OutboundArticle43CandidateUnits is null && r.InboundArticle43CandidateUnits is null);
});
Test("Ayni gun toplam saat dogru", () => {
    var r = Run(); EqualsAmount(r.OutboundDurationHours, 1m); EqualsAmount(r.InboundDurationHours, 5m);
});
Test("Ayni gun seyahat ve gorev tarihleri", () => {
    var r = Run(); Require(r.CandidateCalendarDates.SequenceEqual(new[] { new DateOnly(2026, 6, 15) }));
    Require(r.CandidateTravelDates.Count == 1 && r.CandidateWorkDates.Count == 1 && r.OverlapDates.Count == 1);
});
Test("Gunu birlik 1/3", () => {
    var q = Request() with { WorkEnds = T(0, 13, 15), Inbound = new JourneyLeg(T(0, 13, 20), T(0, 14), "BILET-D") };
    EqualsAmount(Run(q).SameDayPreview?.UnroundedAmount, 850m / 3m);
});
Test("Gunu birlik sifir ogun", () => {
    var q = Request() with { WorkEnds = T(0, 10), Inbound = new JourneyLeg(T(0, 10), T(0, 12), "BILET-D") };
    EqualsAmount(Run(q).SameDayPreview?.UnroundedAmount, 0m);
});
Test("Gunu birlik 13:00 esitligi inceleme", () => {
    var q = Request() with { Outbound = new JourneyLeg(T(0, 13), T(0, 13, 30), "BILET-G"), WorkStarts = T(0, 13, 30) };
    Status(q, TimelineStatus.NeedsReview);
});
Test("Gunu birlik 19:00 esitligi inceleme", () => {
    var q = Request() with { Inbound = new JourneyLeg(T(0, 18), T(0, 19), "BILET-D") };
    Status(q, TimelineStatus.NeedsReview);
});
Test("Gunu birlik tarife 8000", () => {
    var q = Request() with { Personnel = new PersonnelTariffInput(new DateOnly(2026, 6, 15), PersonnelTariffGroup.GeneralCivilServant, 5, 8000) };
    EqualsAmount(Run(q).SameDayPreview?.DailyRate, 890m);
});
Test("Gunu birlik resmi belge degil", () => Require(!Run().IsOfficialPaymentDocument && Run().SameDayPreview?.IsOfficialPaymentDocument == false));
Test("Iki gun yol 1+1 aday", () => Multi(TwoDay(), 1, 1));
Test("Iki gun calisma takvimi", () => {
    var r=Run(TwoDay());
    Require(r.CandidateWorkDates.Count == 2 && r.CandidateCalendarDates.Count == 2 && r.OverlapDates.Count == 2);
});
Test("Cok gunlu hak edis listesi uretilmez", () => Require(!Run(TwoDay()).IsReviewedEligibleDayLedger));
Test("Gidis 24 saat tam bir birim", () => {
    var q = Request() with { Outbound = new JourneyLeg(T(0, 8), T(1, 8), "BILET-G"), WorkStarts = T(1, 8), WorkEnds = T(1, 14), Inbound = new JourneyLeg(T(1, 15), T(1, 20), "BILET-D") };
    Multi(q, 1, 1);
});
Test("Gidis 24 saat bir dakika iki birim", () => {
    var q = Request() with { Outbound = new JourneyLeg(T(0, 8), T(1, 8, 1), "BILET-G"), WorkStarts = T(1, 9), WorkEnds = T(1, 14), Inbound = new JourneyLeg(T(1, 15), T(1, 20), "BILET-D") };
    Multi(q, 2, 1);
});
Test("Gidis 48 saat iki birim", () => {
    var q = Request() with { Outbound = new JourneyLeg(T(0, 8), T(2, 8), "BILET-G"), WorkStarts = T(2, 8), WorkEnds = T(2, 14), Inbound = new JourneyLeg(T(2, 15), T(2, 20), "BILET-D") };
    Multi(q, 2, 1);
});
Test("Gidis 48 saat bir dakika uc birim", () => {
    var q = Request() with { Outbound = new JourneyLeg(T(0, 8), T(2, 8, 1), "BILET-G"), WorkStarts = T(2, 9), WorkEnds = T(2, 14), Inbound = new JourneyLeg(T(2, 15), T(2, 20), "BILET-D") };
    Multi(q, 3, 1);
});
Test("Donus 25 saat iki birim", () => {
    var q=TwoDay() with { Inbound=new JourneyLeg(T(1, 15), T(2, 16), "BILET-D") };
    Multi(q, 1, 2);
});
Test("Yol ve gorev tarih carpisimi uniq", () => {
    var q = Request() with { Outbound = new JourneyLeg(T(0, 8), T(1, 8), "BILET-G"), WorkStarts = T(1, 9), WorkEnds = T(2, 13), Inbound = new JourneyLeg(T(2, 14), T(3, 15), "BILET-D") };
    var r=Run(q);
    Require(r.CandidateCalendarDates.Count==4 && r.CandidateWorkDates.Count==2 && r.CandidateTravelDates.Count==4 && r.OverlapDates.Count==2);
});
Test("Gorev gunlerinde ara olmamasi varsayilmaz", () => {
    var r=Run(TwoDay()); Require(r.Status==TimelineStatus.NeedsReview && r.SameDayPreview is null);
});
Test("Tarih yil gecisi blok", () => {
    var q=TwoDay() with { Inbound=new JourneyLeg(T(199, 15), T(200, 20), "BILET-D"), WorkEnds = T(199, 14) };
    Status(q, TimelineStatus.NoApplicableYear);
});
Test("Tarife islem tarihi baska gun blok", () => {
    var q=Request() with { Personnel = new PersonnelTariffInput(new DateOnly(2026, 6, 16), PersonnelTariffGroup.GeneralCivilServant, 5, 0) };
    Status(q, TimelineStatus.NeedsReview);
});
Test("Baslangic sonrasi varis ters blok", () => {
    var q=Request() with { WorkStarts = T(0, 8, 59) }; Status(q, TimelineStatus.NeedsReview);
});
Test("Bitis baslangictan once blok", () => {
    var q=Request() with { WorkEnds = T(0, 8, 55) }; Status(q, TimelineStatus.NeedsReview);
});
Test("Donus baslangici gorev bitmeden blok", () => {
    var q=Request() with { Inbound = new JourneyLeg(T(0, 13), T(0, 20), "BILET-D") }; Status(q, TimelineStatus.NeedsReview);
});
Test("Donus varis hareketten once blok", () => {
    var q=Request() with { Inbound = new JourneyLeg(T(0, 20), T(0, 19), "BILET-D") }; Status(q, TimelineStatus.NeedsReview);
});
Test("Gidis varis hareketten once blok", () => {
    var q=Request() with { Outbound = new JourneyLeg(T(0, 9), T(0, 8), "BILET-G") }; Status(q, TimelineStatus.NeedsReview);
});
Test("Gidis sifir sure blok", () => {
    var q=Request() with { Outbound = new JourneyLeg(T(0, 9), T(0, 9), "BILET-G") }; Status(q, TimelineStatus.NeedsReview);
});
Test("Gorev sifir sure blok", () => {
    var q=Request() with { WorkEnds = T(0, 9) }; Status(q, TimelineStatus.NeedsReview);
});
Test("Donus sifir sure blok", () => {
    var q=Request() with { Inbound = new JourneyLeg(T(0, 20), T(0, 20), "BILET-D") }; Status(q, TimelineStatus.NeedsReview);
});
Test("Zaman turu UTC blok", () => {
    var q=Request() with { WorkStarts = DateTime.SpecifyKind(T(0, 9), DateTimeKind.Utc) }; Status(q, TimelineStatus.NeedsReview);
});
Test("Zaman turu Local blok", () => {
    var q=Request() with { WorkStarts = DateTime.SpecifyKind(T(0, 9), DateTimeKind.Local) }; Status(q, TimelineStatus.NeedsReview);
});
Test("Yaz saat kaygisi blok", () => Status(Request() with { TimeZoneOrClockChangeConcern = true }, TimelineStatus.NeedsReview));
Test("Memuriyet mahalli ici blok", () => Status(Request() with { Station=DutyStationAssessment.Inside }, TimelineStatus.NotEligibleByLocation));
Test("Memuriyet mahalli bilinmiyor blok", () => Status(Request() with { Station=DutyStationAssessment.Unknown }, TimelineStatus.NeedsReview));
Test("Memuriyet mahalli gecersiz enum blok", () => Status(Request() with { Station=(DutyStationAssessment)999 }, TimelineStatus.NeedsReview));
Test("Memuriyet mahalli referanssiz blok", () => Status(Request() with { StationEvidenceReference="" }, TimelineStatus.NeedsReview));
Test("Olur incelenmedi blok", () => Status(Request() with { AssignmentReviewed=false }, TimelineStatus.NeedsReview));
Test("Olur delil yok blok", () => Status(Request() with { AssignmentEvidenceReference=" " }, TimelineStatus.NeedsReview));
Test("Gorev saat onayi yok blok", () => Status(Request() with { WorkTimesReviewed=false }, TimelineStatus.NeedsReview));
Test("Gorev saat delili yok blok", () => Status(Request() with { WorkTimeEvidenceReference="" }, TimelineStatus.NeedsReview));
Test("Gidis delili eksik blok", () => Status(Request() with { Outbound = new JourneyLeg(T(0, 8), T(0, 9), "") }, TimelineStatus.NeedsReview));
Test("Donus delili eksik blok", () => Status(Request() with { Inbound = new JourneyLeg(T(0, 15), T(0, 20), "") }, TimelineStatus.NeedsReview));
Test("Gidis null blok", () => Status(Request() with { Outbound = null! }, TimelineStatus.NeedsInput));
Test("Donus null blok", () => Status(Request() with { Inbound = null! }, TimelineStatus.NeedsInput));
Test("Tarih alani default blok", () => Status(Request() with { WorkStarts = default }, TimelineStatus.NeedsInput));
Test("2025 yilinda blok", () => {
    var q=Request() with { Personnel=new PersonnelTariffInput(new DateOnly(2025, 6, 15), PersonnelTariffGroup.GeneralCivilServant,5,0) };
    Status(q, TimelineStatus.NoApplicableYear);
});
Test("2027 yilinda blok", () => {
    var q=Request() with { Personnel=new PersonnelTariffInput(new DateOnly(2027, 6, 15), PersonnelTariffGroup.GeneralCivilServant,5,0) };
    Status(q, TimelineStatus.NoApplicableYear);
});
Test("Ozel statude m39 tarife blok", () => {
    var q=Request() with { Personnel=new PersonnelTariffInput(new DateOnly(2026,6,15), PersonnelTariffGroup.SpecialOrHighOffice,5,0) };
    Status(q, TimelineStatus.NeedsReview);
});
Test("Eksik kadro derecesi blok", () => {
    var q=Request() with { Personnel=new PersonnelTariffInput(new DateOnly(2026,6,15), PersonnelTariffGroup.GeneralCivilServant,null,0) };
    Status(q, TimelineStatus.NeedsInput);
});
Test("M43 24 saat 1 tik iki birim", () => {
    var q=Request() with { Outbound=new JourneyLeg(T(0,8), T(1,8).AddTicks(1),"BILET-G"), WorkStarts=T(1,9), WorkEnds=T(1,14), Inbound=new JourneyLeg(T(1,15),T(1,20),"BILET-D") };
    Multi(q,2,1);
});
Test("M43 24 saat 1 tik eksik bir birim", () => {
    var q=Request() with { Outbound=new JourneyLeg(T(0,8), T(1,8).AddTicks(-1),"BILET-G"), WorkStarts=T(1,9), WorkEnds=T(1,14), Inbound=new JourneyLeg(T(1,15),T(1,20),"BILET-D") };
    Multi(q,1,1);
});
Test("M43 adaylarini eski 42 sayacina eklememe", () => {
    var r=Run(TwoDay()); Require(r.SameDayPreview is null && !r.IsReviewedEligibleDayLedger && r.OutboundArticle43CandidateUnits==1);
});
Test("Null request throw", () => {
    bool threw = false;
    try { engine.Preview(null!); } catch(ArgumentNullException) { threw=true; }
    Require(threw);
});
Test("Null personel throw", () => {
    bool threw = false;
    try { engine.Preview(Request() with { Personnel=null! }); } catch(ArgumentNullException) { threw=true; }
    Require(threw);
});

int failed = 0;
foreach (var (name, action) in tests)
{
    try { action(); Console.WriteLine("[OK] " + name); }
    catch (Exception e) { failed++; Console.Error.WriteLine("[HATA] " + name + ": " + e.Message); }
}
Console.WriteLine($"TOPLAM: {tests.Count-failed}/{tests.Count} KONTROL BASARILI");
Console.WriteLine("UYARI: m43 birimleri aday surelerdir; 42 gun sayaci ve resmi bordro DEĞİLDİR.");
return failed == 0 ? 0 : 1;
