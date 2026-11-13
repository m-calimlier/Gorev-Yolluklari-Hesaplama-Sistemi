using GorevYolluklari.Core.Integration;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

if (args.Length != 1 || !File.Exists(args[0]))
    throw new ArgumentException("H Cetveli ön hesap JSON tam yolunu verin.");

var engine = new HarcirahPreviewEngine(DailyRateTariff2026.FromVerifiedJson(args[0]));
int passed = 0;
void Check(string name, bool condition)
{
    if (!condition) throw new Exception("TEST BAŞARISIZ: " + name);
    passed++;
    Console.WriteLine("[OK] " + name);
}
void Throws<T>(string name, Action a) where T : Exception
{
    bool caught = false;
    try { a(); } catch (T) { caught = true; }
    Check(name, caught);
}
PersonnelTariffInput P(int year = 2026, int? grade = 5, int? indicator = 0,
    PersonnelTariffGroup group = PersonnelTariffGroup.GeneralCivilServant) =>
    new(new DateOnly(year, 6, 15), group, grade, indicator);
SameDayDutyRequest Day(
    PersonnelTariffInput? p = null, int fromHour = 9, int toHour = 20,
    DutyStationAssessment station = DutyStationAssessment.Outside,
    bool reviewed = true, string? evidence = "GOREV-OLURU-ORNEK-001") =>
    new(p ?? P(), new DateTime(2026, 6, 15, fromHour, 0, 0),
        new DateTime(2026, 6, 15, toHour, 0, 0), station, reviewed, evidence);
RelocationDutyRequest Move(
    PersonnelTariffInput? p = null,
    DutyStationAssessment station = DutyStationAssessment.Outside,
    bool eligible = true, bool family = true, bool spouse = false,
    int familyCount = 2, decimal km = 10m, string? evidence = "KGM-ORNEK-001") =>
    new(p ?? P(grade: 1), station, eligible, family, spouse, familyCount, km, evidence);

var fullDay = engine.PreviewSameDay(Day());
Check("Günübirlik 2/3: 850 TL tarife ile bağlı",
    fullDay.Status == IntegratedPreviewStatus.Preliminary &&
    fullDay.DailyRate == 850m && fullDay.DailyCategory == "B_e" &&
    fullDay.UnroundedAmount == 850m * 2m / 3m);
Check("Ön hesap kaynağında BUT-002 yer alır",
    fullDay.TariffReference.Contains("BUT-002") && fullDay.LegalBasis.Contains("m.39"));
Check("Ön hesap resmî ödeme belgesi değildir", !fullDay.IsOfficialPaymentDocument);
Check("Yuvarlama erken yapılmaz", fullDay.UnroundedAmount != 566.67m);
var oneMeal = engine.PreviewSameDay(Day(toHour: 14));
Check("Günübirlik 1/3", oneMeal.UnroundedAmount == 850m / 3m);
var noMeal = engine.PreviewSameDay(Day(fromHour: 8, toHour: 12));
Check("Öğün geçmiyorsa 0/3 ön hesap", noMeal.UnroundedAmount == 0m && noMeal.Status == IntegratedPreviewStatus.Preliminary);
Check("Mahalliçi gündelik yok", engine.PreviewSameDay(Day(station: DutyStationAssessment.Inside)).Status == IntegratedPreviewStatus.NotEligibleByLocation);
Check("Mahalli belirsizse blok", engine.PreviewSameDay(Day(station: DutyStationAssessment.Unknown)).Status == IntegratedPreviewStatus.NeedsReview);
Check("Bozuk mahalli enum blok", engine.PreviewSameDay(Day(station: (DutyStationAssessment)999)).Status == IntegratedPreviewStatus.NeedsReview);
Check("Görev oluru incelenmemişse blok", engine.PreviewSameDay(Day(reviewed: false)).Status == IntegratedPreviewStatus.NeedsReview);
Check("Mahalli delili yoksa blok", engine.PreviewSameDay(Day(evidence: null)).Status == IntegratedPreviewStatus.NeedsReview);
Check("13:00 eşitliği inceleme", engine.PreviewSameDay(Day(fromHour: 13)).Status == IntegratedPreviewStatus.NeedsReview);
Check("19:00 eşitliği inceleme", engine.PreviewSameDay(Day(toHour: 19)).Status == IntegratedPreviewStatus.NeedsReview);
var overnight = Day() with { Return = new DateTime(2026, 6, 16, 9, 0, 0) };
Check("Çok günlü seyahat manuel inceleme", engine.PreviewSameDay(overnight).Status == IntegratedPreviewStatus.NeedsReview);
Check("Gidiş dönüş sırası geçersiz", engine.PreviewSameDay(Day(fromHour: 20, toHour: 9)).Status == IntegratedPreviewStatus.NeedsReview);
Check("Tarife tarihi / ayrılış tarihi uyumsuz", engine.PreviewSameDay(Day(p: P(year: 2025))).Status == IntegratedPreviewStatus.NeedsReview);
Check("Tarih türü uyumsuz", engine.PreviewSameDay(Day() with {
    Return = DateTime.SpecifyKind(new DateTime(2026, 6, 15, 20, 0, 0), DateTimeKind.Utc)
}).Status == IntegratedPreviewStatus.NeedsReview);
Check("Her iki saat UTC olursa yerel saat belirsiz", engine.PreviewSameDay(Day() with {
    Departure = DateTime.SpecifyKind(new DateTime(2026, 6, 15, 9, 0, 0), DateTimeKind.Utc),
    Return = DateTime.SpecifyKind(new DateTime(2026, 6, 15, 20, 0, 0), DateTimeKind.Utc)
}).Status == IntegratedPreviewStatus.NeedsReview);
Check("Eksik derece ile hesap yok", engine.PreviewSameDay(Day(p: P(grade: null))).Status == IntegratedPreviewStatus.NeedsInput);
Check("Özel statüde hesap yok", engine.PreviewSameDay(Day(p: P(group: PersonnelTariffGroup.SpecialOrHighOffice))).Status == IntegratedPreviewStatus.NeedsReview);
Check("2027 tarifesi otomatik uygulanmaz", engine.PreviewSameDay(Day(p: P(year: 2027)) with {
    Departure = new DateTime(2027, 6, 15, 9, 0, 0), Return = new DateTime(2027, 6, 15, 20, 0, 0)
}).Status == IntegratedPreviewStatus.NoApplicableYear);
Check("8000 eşiği 890 TL", engine.PreviewSameDay(Day(p: P(2026, 5, 8000))).DailyRate == 890m);
Check("6400 eşiği 880 TL", engine.PreviewSameDay(Day(p: P(2026, 5, 6400))).DailyRate == 880m);
Check("3600 eşiği 870 TL", engine.PreviewSameDay(Day(p: P(2026, 5, 3600))).DailyRate == 870m);
Check("1-4 derece 860 TL", engine.PreviewSameDay(Day(p: P(2026, 1, 0))).DailyRate == 860m);

var m = engine.PreviewRelocation(Move());
Check("Sürekli görev: 860 x (20+20+0.5) ön hesap", m.Status == IntegratedPreviewStatus.Preliminary &&
    m.DailyRate == 860m && m.UnroundedAmount == 34830m);
Check("Sürekli görev kanun dayanağı ve KGM referansı", m.LegalBasis.Contains("m.45") && m.EvidenceReference == "KGM-ORNEK-001");
Check("Sürekli görev aile 40 gündelik üst sınır", engine.PreviewRelocation(Move(familyCount: 7)).UnroundedAmount == 860m * (20m + 40m + 0.5m));
Check("Sürekli görev aile üyesi 0", engine.PreviewRelocation(Move(familyCount: 0)).UnroundedAmount == 860m * (20m + 0.5m));
Check("Sürekli görev km=0 hesap matematiği", engine.PreviewRelocation(Move(km: 0m)).UnroundedAmount == 860m * 40m);
var secondSpouse = engine.PreviewRelocation(Move(spouse: true));
Check("İkinci memur eş: yalnız değişken unsur yarım",
    secondSpouse.Status == IntegratedPreviewStatus.Preliminary &&
    secondSpouse.UnroundedAmount == 860m * (20m + 20m + 0.25m));
Check("Hak sahipliği teyitsizse blok", engine.PreviewRelocation(Move(eligible: false)).Status == IntegratedPreviewStatus.NeedsReview);
Check("Aile incelemesi yoksa blok", engine.PreviewRelocation(Move(family: false)).Status == IntegratedPreviewStatus.NeedsReview);
Check("KGM mesafe dayanağı yoksa blok", engine.PreviewRelocation(Move(evidence: " ")).Status == IntegratedPreviewStatus.NeedsReview);
Check("Mahalliçi taşınmada blok", engine.PreviewRelocation(Move(station: DutyStationAssessment.Inside)).Status == IntegratedPreviewStatus.NotEligibleByLocation);
Check("Bilinmeyen mahalli sınıfında blok", engine.PreviewRelocation(Move(station: (DutyStationAssessment)999)).Status == IntegratedPreviewStatus.NeedsReview);
Check("2025 tarifesi ile taşıma ön hesabı yok", engine.PreviewRelocation(Move(p: P(2025))).Status == IntegratedPreviewStatus.NoApplicableYear);
Check("Eksik ek göstergeyle taşıma hesaplanmaz", engine.PreviewRelocation(Move(p: P(indicator: null))).Status == IntegratedPreviewStatus.NeedsInput);
Check("Özel statüde taşınma hesaplanmaz", engine.PreviewRelocation(Move(p: P(group: PersonnelTariffGroup.SpecialOrHighOffice))).Status == IntegratedPreviewStatus.NeedsReview);
Throws<ArgumentOutOfRangeException>("Negatif kilometre engellenir", () => engine.PreviewRelocation(Move(km: -1m)));
Throws<ArgumentOutOfRangeException>("Negatif aile sayısı engellenir", () => engine.PreviewRelocation(Move(familyCount: -1)));
Check("Taşınma sonucu da resmî belge değil", !m.IsOfficialPaymentDocument);

Console.WriteLine($"TOPLAM: {passed}/{passed} KONTROL BAŞARILI");
Console.WriteLine("UYARI: Tarife + m.39/m.45 sonuçları yalnız sentetik verilere dayalı ön hesaptır.");
Console.WriteLine("UYARI: Hak sahipliği, yol/konaklama, resmi örnek 27/28 ve bordro onayı henüz yok.");
