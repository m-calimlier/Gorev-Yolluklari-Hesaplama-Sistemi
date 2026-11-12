using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Integration;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
namespace GorevYolluklari.Application;
public sealed class TravelWorkflowService(string tariffPath, CalculationConstants? constants = null)
{
    private readonly CalculationConstants _constants = constants ?? new();

    private DailyRateTariff2026 LoadTariff()
    {
        var verified = DailyRateTariff2026.FromVerifiedJson(tariffPath);
        return _constants.Common == new CommonConstants() ? verified : verified.ForSimulation(_constants.Common);
    }

    private bool IsSimulation(DraftKind kind) => _constants.Common != new CommonConstants() ||
        (kind == DraftKind.TemporaryDuty ? _constants.Temporary != new TemporaryConstants() :
            _constants.Permanent != new PermanentConstants());

    private string RuleVersion(DraftKind kind)
    {
        if (!IsSimulation(kind)) return "ASAMA8-2026-1";
        var module = kind == DraftKind.TemporaryDuty
            ? JsonSerializer.Serialize(_constants.Temporary) : JsonSerializer.Serialize(_constants.Permanent);
        var input = JsonSerializer.Serialize(_constants.Common) + module;
        return "SIM-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    public TimelinePreviewResult PreviewTimeline(PersonnelRecord person, DutyRecord duty)
    {
        if (duty.Kind != DraftKind.TemporaryDuty) throw new InvalidOperationException("Seyahat takvimi geçici görev içindir.");
        var tariff = LoadTariff();
        return new TravelTimelinePreviewEngine2026(tariff, _constants.Temporary).Preview(new(
            new(DateOnly.FromDateTime(duty.Departure), person.Group, person.Grade, person.Indicator),
            duty.Station, duty.AssignmentReviewed && duty.StationReviewed && !string.IsNullOrWhiteSpace(duty.Reviewer),
            duty.AssignmentEvidence, duty.StationEvidence,
            new(duty.Departure, duty.OutboundArrival ?? default, duty.OutboundEvidence),
            duty.WorkStarts ?? default, duty.WorkEnds ?? default, duty.WorkTimeEvidence, duty.WorkTimesReviewed,
            new(duty.InboundDeparture ?? default, duty.Return, duty.InboundEvidence), duty.ClockConcern));
    }
    public CalculationSnapshot Preview(PersonnelRecord person, DutyRecord duty) =>
        Calculate(person, duty, calculationOnly: false);

    /// <summary>
    /// Yalnız hesaplama çıktısı: memurun belge incelemesini kendisi yapmış gibi
    /// işaretlemeden, girilen tarife/yer/tarih/aile/mesafe bilgileriyle ön hesap.
    /// </summary>
    public CalculationSnapshot PreviewForCalculation(PersonnelRecord person, DutyRecord duty) =>
        Calculate(person, duty, calculationOnly: true);

    private CalculationSnapshot Calculate(PersonnelRecord person, DutyRecord duty, bool calculationOnly)
    {
        var tariff = LoadTariff();
        var personnel = new PersonnelTariffInput(DateOnly.FromDateTime(duty.Departure), person.Group, person.Grade, person.Indicator);
        var reviewed = calculationOnly || (duty.AssignmentReviewed && duty.StationReviewed && !string.IsNullOrWhiteSpace(duty.Reviewer) &&
            !string.IsNullOrWhiteSpace(duty.AssignmentEvidence) && !string.IsNullOrWhiteSpace(duty.StationEvidence));
        var permanentDuty = duty.Kind == DraftKind.PermanentDuty;
        if (duty.Departure == default || (!permanentDuty && duty.Return == default))
            return Snapshot(person, duty, "Bilgi gerekli", null, null, null, permanentDuty
                ? "Atama / işlem tarihini seçin." : "Ayrılış ve dönüş için tarih, saat ve dakika seçin.", "2026 H Cetveli");
        if (duty.Departure.Year != 2026 || (!permanentDuty && duty.Return.Year != 2026))
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "Bu sürümde yalnız 2026 yılı tarifesi var.", "2026 H Cetveli");
        if (!permanentDuty && duty.Return < duty.Departure)
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "Dönüş, ayrılıştan önce olamaz.", "2026 H Cetveli");
        if (duty.Kind == DraftKind.PermanentDuty)
        {
            var r = new PermanentDutyEngine(tariff, _constants.Permanent).PreviewRelocation(new(personnel, duty.Station, reviewed, calculationOnly || duty.FamilyReviewed,
                duty.SpouseTransfer, duty.FamilyCount, duty.Kilometers,
                calculationOnly && string.IsNullOrWhiteSpace(duty.DistanceEvidence)
                    ? "Kullanıcı tarafından girilen mesafe" : duty.DistanceEvidence));
            IReadOnlyList<CalculationComponent>? components = null;
            if (r.Status == IntegratedPreviewStatus.Preliminary && r.DailyRate is decimal rate)
                components = [
                    new("Kişi sabit unsuru", _constants.Permanent.Self(rate)),
                    new("Aile sabit unsuru", _constants.Permanent.Family(rate, duty.FamilyCount)),
                    new("Mesafeye bağlı unsur", _constants.Permanent.Distance(rate, duty.Kilometers) *
                        (duty.SpouseTransfer ? 0.5m : 1m))
                ];
            return Snapshot(person, duty, r.Status.ToString(), r.UnroundedAmount, r.DailyRate, r.DailyCategory,
                "Yalnız yer değiştirme bileşeni; tam sürekli görev toplamı değildir. " + r.Explanation, r.LegalBasis, components);
        }
        if (duty.Kind != DraftKind.TemporaryDuty)
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "Görev türü geçersiz.", "");
        if (duty.Departure.Date == duty.Return.Date)
        {
            var r = new TemporaryDutyEngine(tariff, _constants.Temporary).PreviewSameDay(new(personnel, duty.Departure, duty.Return, duty.Station, reviewed,
                calculationOnly && string.IsNullOrWhiteSpace(duty.StationEvidence)
                    ? "Kullanıcı tarafından seçilen memuriyet mahalli" : duty.StationEvidence));
            return Snapshot(person, duty, r.Status.ToString(), r.UnroundedAmount, r.DailyRate, r.DailyCategory, r.Explanation, r.LegalBasis);
        }
        // Hesaplama modu resmi belge incelemesi beyanı oluşturmaz. Motorun
        // hesap koşullarına gerekli varsayımlar yalnız bu çağrıya uygulanır.
        // Gün listesi ve fatura tutarları kullanıcı tarafından girilmelidir.
        var result = new MultiDayDutyPreviewEngine2026(tariff, _constants.Temporary).Preview(new(personnel, duty.Station,
            reviewed, calculationOnly || duty.AnnualPeriodReviewed,
            calculationOnly || (duty.PriorDaysReviewed && duty.PriorDays.HasValue),
            calculationOnly || duty.EligibleDaysReviewed,
            calculationOnly || duty.Article42ExceptionsExcluded,
            calculationOnly || duty.LodgingReviewed,
            duty.PriorDays ?? 0,
            duty.ReviewedDays.Select(x => new ReviewedDutyDay(
                DateOnly.FromDateTime(x.Date), x.LodgingOrdinal, x.LodgingAmount,
                calculationOnly && x.LodgingAmount.HasValue && string.IsNullOrWhiteSpace(x.Receipt)
                    ? "Konaklama tutarı kullanıcı girdisi" : x.Receipt)).ToArray(),
            calculationOnly ? "Kullanıcı tarafından girilen görev" : duty.AssignmentEvidence,
            calculationOnly ? "Kullanıcı tarafından seçilen görev yeri" : duty.StationEvidence,
            calculationOnly ? "Önceki gün sayısı kullanıcı girdisi" : duty.PriorDaysEvidence));
        if (duty.ReviewedDays.Any(x => x.Date.Date < duty.Departure.Date || x.Date.Date > duty.Return.Date))
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "İncelenmiş gün görev aralığı dışında.", result.LegalBasis);
        return Snapshot(person, duty, result.Status.ToString(), result.CombinedTotalUnrounded, result.DailyRate,
            tariff.Resolve(personnel).Category, result.Explanation, result.LegalBasis,
            result.PerDiemTotalUnrounded is decimal perDiem && result.LodgingTotalUnrounded is decimal lodging
                ? [new("Gündelik", perDiem), new("Konaklama", lodging)] : null);
    }
    private CalculationSnapshot Snapshot(PersonnelRecord p, DutyRecord d, string status, decimal? amount, decimal? rate, string? category, string reason, string legal, IReadOnlyList<CalculationComponent>? components = null) =>
        new(Fingerprint(p, d), status, amount, rate, category, "BUT-002:2026:I-B", reason, legal, DateTimeOffset.UtcNow,
            JsonSerializer.Deserialize<PersonnelRecord>(JsonSerializer.Serialize(p))!, RuleVersion(d.Kind), components);
    public static string Fingerprint(PersonnelRecord p, DutyRecord d)
    {
        var clone = JsonSerializer.Deserialize<DutyRecord>(JsonSerializer.Serialize(d))!; clone.Snapshot = null;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Person = p, Duty = clone }))));
    }
    public bool IsCurrent(PersonnelRecord p, DutyRecord d) => d.Snapshot?.RuleVersion == RuleVersion(d.Kind) && d.Snapshot.Fingerprint == Fingerprint(p, d);
    public DraftDecision PrepareDraft(PersonnelRecord p, DutyRecord d)
    {
        if (IsSimulation(d.Kind) || d.Snapshot?.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal) == true)
            return new(DraftDecisionStatus.NeedsReview, "Simülasyon sabitleriyle PDF oluşturulmaz. Kaynak değerlerini geri yükleyip yeniden hesaplayın.", null);
        if (!IsCurrent(p, d) || d.Snapshot?.Amount is null || !d.AmountsReviewed || d.ReviewedAt is null || string.IsNullOrWhiteSpace(d.Reviewer))
            return new(DraftDecisionStatus.NeedsReview, "Güncel ön hesap ve tarihli kanıt/tutar incelemesi gerekli. Önce yeniden hesaplayın.", null);
        if (d.IndependentGross is null || d.IndependentNet is null)
            return new(DraftDecisionStatus.NeedsInput, "Bağımsız brüt ve net kontrol tutarları girilmelidir.", null);
        return DraftDocumentService.Create(new(d.Kind, d.DocumentNumber, p.FullName, p.Title, p.Institution,
            d.Origin, d.Destination, DateOnly.FromDateTime(d.Departure),
            DateOnly.FromDateTime(d.Kind == DraftKind.PermanentDuty ? d.Departure : d.Return), 2026,
            d.Snapshot.TariffReference, d.Snapshot.RuleVersion, d.AssignmentEvidence, d.StationEvidence,
            d.AssignmentReviewed && d.StationReviewed, d.AmountsReviewed, d.Expenses.Select(x => x.ToLine()).ToArray(),
            d.Advance, d.AdvanceEvidence, d.IndependentGross.Value, d.IndependentNet.Value));
    }
    public DraftDecision PrepareCalculationPdf(PersonnelRecord p, DutyRecord d)
    {
        if (IsSimulation(d.Kind) || d.Snapshot?.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal) == true)
            return new(DraftDecisionStatus.NeedsReview,
                "Simülasyon sabitleriyle hesaplama PDF'si oluşturulmaz. Tarifeyi geri yükleyin.", null);
        if (!IsCurrent(p, d) || d.Snapshot?.Amount is null)
            return new(DraftDecisionStatus.NeedsReview,
                "Güncel hesap bulunamadı. Kaydı yeniden hesaplayıp kaydedin.", null);
        // Kullanıcının yanlış elle yazdığı gündelik/yer değiştirme tutarı PDF'ye
        // taşınmasın. Yalnız aritmetik denetlenir; belge hak sahipliği beyan edilmez.
        var itemKind = d.Kind == DraftKind.PermanentDuty
            ? DraftLineKind.Relocation : DraftLineKind.DailyAllowance;
        var calculated = d.Kind == DraftKind.PermanentDuty || d.Departure.Date == d.Return.Date
            ? d.Snapshot.Amount
            : d.Snapshot.Components?.FirstOrDefault(x => x.Label == "Gündelik")?.Amount;
        if (calculated is decimal raw && raw > 0m)
        {
            var expected = decimal.Round(raw, 2, MidpointRounding.AwayFromZero);
            var lines = d.Expenses.Where(x => x.Kind == itemKind).ToArray();
            if (lines.Length != 1 || lines[0].Amount != expected)
                return new(DraftDecisionStatus.Inconsistent,
                    "Hesaplanan gündelik/yer değiştirme tutarı gider satırıyla eşleşmiyor. Kaydı yeniden hesaplayın.", null);
        }
        if (d.Kind == DraftKind.TemporaryDuty && d.Departure.Date != d.Return.Date &&
            d.Snapshot.Components?.FirstOrDefault(x => x.Label == "Konaklama") is { Amount: var lodging })
        {
            var expectedLodging = decimal.Round(lodging, 2, MidpointRounding.AwayFromZero);
            var actualLodging = d.Expenses.Where(x => x.Kind == DraftLineKind.Accommodation).Sum(x => x.Amount);
            if (actualLodging != expectedLodging)
                return new(DraftDecisionStatus.Inconsistent,
                    "Konaklama toplamı gün hesabıyla uyuşmuyor. Kaydı yeniden hesaplayın.", null);
        }
        return DraftDocumentService.CreateCalculationCopy(new(d.Kind, d.DocumentNumber,
            p.FullName, p.Title, p.Institution, d.Origin, d.Destination,
            DateOnly.FromDateTime(d.Departure),
            DateOnly.FromDateTime(d.Kind == DraftKind.PermanentDuty ? d.Departure : d.Return),
            2026, d.Snapshot.TariffReference, d.Snapshot.RuleVersion,
            d.AssignmentEvidence, d.StationEvidence,
            false, false, d.Expenses.Select(x => x.ToLine()).ToArray(),
            d.Advance, d.AdvanceEvidence, 0m, 0m));
    }

}
