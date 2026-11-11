using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Integration;
using GorevYolluklari.Core.Tariffs;
namespace GorevYolluklari.Application;
public sealed class TravelWorkflowService(string tariffPath)
{
    public TimelinePreviewResult PreviewTimeline(PersonnelRecord person, DutyRecord duty)
    {
        if (duty.Kind != DraftKind.TemporaryDuty) throw new InvalidOperationException("Seyahat takvimi geçici görev içindir.");
        var tariff = DailyRateTariff2026.FromVerifiedJson(tariffPath);
        return new TravelTimelinePreviewEngine2026(tariff).Preview(new(
            new(DateOnly.FromDateTime(duty.Departure), person.Group, person.Grade, person.Indicator),
            duty.Station, duty.AssignmentReviewed && duty.StationReviewed && !string.IsNullOrWhiteSpace(duty.Reviewer),
            duty.AssignmentEvidence, duty.StationEvidence,
            new(duty.Departure, duty.OutboundArrival ?? default, duty.OutboundEvidence),
            duty.WorkStarts ?? default, duty.WorkEnds ?? default, duty.WorkTimeEvidence, duty.WorkTimesReviewed,
            new(duty.InboundDeparture ?? default, duty.Return, duty.InboundEvidence), duty.ClockConcern));
    }
    public CalculationSnapshot Preview(PersonnelRecord person, DutyRecord duty)
    {
        var tariff = DailyRateTariff2026.FromVerifiedJson(tariffPath);
        var personnel = new PersonnelTariffInput(DateOnly.FromDateTime(duty.Departure), person.Group, person.Grade, person.Indicator);
        var reviewed = duty.AssignmentReviewed && duty.StationReviewed && !string.IsNullOrWhiteSpace(duty.Reviewer) &&
            !string.IsNullOrWhiteSpace(duty.AssignmentEvidence) && !string.IsNullOrWhiteSpace(duty.StationEvidence);
        if (duty.Return < duty.Departure || duty.Return.Year != 2026 || duty.Departure.Year != 2026)
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "Tarih aralığı geçersiz veya 2026 dışında. Geçerli tarife yok.", "2026 H Cetveli");
        if (duty.Kind == DraftKind.PermanentDuty)
        {
            var r = new PermanentDutyEngine(tariff).PreviewRelocation(new(personnel, duty.Station, reviewed, duty.FamilyReviewed,
                duty.SpouseTransfer, duty.FamilyCount, duty.Kilometers, duty.DistanceEvidence));
            return Snapshot(person, duty, r.Status.ToString(), r.UnroundedAmount, r.DailyRate, r.DailyCategory,
                "Yalnız yer değiştirme bileşeni; tam sürekli görev toplamı değildir. " + r.Explanation, r.LegalBasis);
        }
        if (duty.Kind != DraftKind.TemporaryDuty)
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "Görev türü geçersiz.", "");
        if (duty.Departure.Date == duty.Return.Date)
        {
            var r = new TemporaryDutyEngine(tariff).PreviewSameDay(new(personnel, duty.Departure, duty.Return, duty.Station, reviewed, duty.StationEvidence));
            return Snapshot(person, duty, r.Status.ToString(), r.UnroundedAmount, r.DailyRate, r.DailyCategory, r.Explanation, r.LegalBasis);
        }
        var result = new MultiDayDutyPreviewEngine2026(tariff).Preview(new(personnel, duty.Station,
            reviewed, duty.AnnualPeriodReviewed, duty.PriorDaysReviewed && duty.PriorDays.HasValue,
            duty.EligibleDaysReviewed, duty.Article42ExceptionsExcluded, duty.LodgingReviewed,
            duty.PriorDays ?? 0, duty.ReviewedDays.Select(x => new ReviewedDutyDay(DateOnly.FromDateTime(x.Date), x.LodgingOrdinal, x.LodgingAmount, x.Receipt)).ToArray(),
            duty.AssignmentEvidence, duty.StationEvidence, duty.PriorDaysEvidence));
        if (duty.ReviewedDays.Any(x => x.Date.Date < duty.Departure.Date || x.Date.Date > duty.Return.Date))
            return Snapshot(person, duty, "İnceleme gerekli", null, null, null, "İncelenmiş gün görev aralığı dışında.", result.LegalBasis);
        return Snapshot(person, duty, result.Status.ToString(), result.CombinedTotalUnrounded, result.DailyRate,
            tariff.Resolve(personnel).Category, result.Explanation, result.LegalBasis);
    }
    private static CalculationSnapshot Snapshot(PersonnelRecord p, DutyRecord d, string status, decimal? amount, decimal? rate, string? category, string reason, string legal) =>
        new(Fingerprint(p, d), status, amount, rate, category, "BUT-002:2026:I-B", reason, legal, DateTimeOffset.UtcNow,
            JsonSerializer.Deserialize<PersonnelRecord>(JsonSerializer.Serialize(p))!);
    public static string Fingerprint(PersonnelRecord p, DutyRecord d)
    {
        var clone = JsonSerializer.Deserialize<DutyRecord>(JsonSerializer.Serialize(d))!; clone.Snapshot = null;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Person = p, Duty = clone }))));
    }
    public bool IsCurrent(PersonnelRecord p, DutyRecord d) => d.Snapshot?.Fingerprint == Fingerprint(p, d);
    public DraftDecision PrepareDraft(PersonnelRecord p, DutyRecord d)
    {
        if (!IsCurrent(p, d) || d.Snapshot?.Amount is null || !d.AmountsReviewed || d.ReviewedAt is null || string.IsNullOrWhiteSpace(d.Reviewer))
            return new(DraftDecisionStatus.NeedsReview, "Güncel ön hesap ve tarihli kanıt/tutar incelemesi gerekli. Önce yeniden hesaplayın.", null);
        if (d.IndependentGross is null || d.IndependentNet is null)
            return new(DraftDecisionStatus.NeedsInput, "Bağımsız brüt ve net kontrol tutarları girilmelidir.", null);
        return DraftDocumentService.Create(new(d.Kind, d.DocumentNumber, p.FullName, p.Title, p.Institution,
            d.Origin, d.Destination, DateOnly.FromDateTime(d.Departure), DateOnly.FromDateTime(d.Return), 2026,
            d.Snapshot.TariffReference, d.Snapshot.RuleVersion, d.AssignmentEvidence, d.StationEvidence,
            d.AssignmentReviewed && d.StationReviewed, d.AmountsReviewed, d.Expenses.Select(x => x.ToLine()).ToArray(),
            d.Advance, d.AdvanceEvidence, d.IndependentGross.Value, d.IndependentNet.Value));
    }
}
