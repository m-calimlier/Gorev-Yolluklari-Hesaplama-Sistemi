using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
namespace GorevYolluklari.Core.Integration;
public sealed class TemporaryDutyEngine
{
 private readonly DailyRateTariff2026 _tariff;
    private readonly TemporaryConstants? _constants;

    public TemporaryDutyEngine(DailyRateTariff2026 tariff, TemporaryConstants? constants = null)
    {
        _tariff = tariff ?? throw new ArgumentNullException(nameof(tariff));
        _constants = constants;
    }

    public IntegratedPreview PreviewSameDay(SameDayDutyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Personnel);
        const string legalBasis = "6245 sayılı Kanun m.3 ve m.39 (ön hesap)";
        if (DateOnly.FromDateTime(request.Departure) != request.Personnel.DutyDate)
            return Block(IntegratedPreviewStatus.NeedsReview,
                "Görevlendirme tarihi, tarife işlem tarihiyle uyuşmuyor.", legalBasis);
        if (request.Departure.Kind == DateTimeKind.Utc ||
            request.Return.Kind == DateTimeKind.Utc ||
            request.Departure.Kind != request.Return.Kind)
            return Block(IntegratedPreviewStatus.NeedsReview,
                "UTC veya farklı zaman türlerinde girilmiş saatler; yerel saatleri doğrulayın.", legalBasis);
        if (!request.AssignmentReviewed)
            return Block(IntegratedPreviewStatus.NeedsReview,
                "Görevlendirme oluru/kararı incelenmeden ön hesap oluşturulmaz.", legalBasis);
        if (!RecognizedStation(request.Station))
            return Block(IntegratedPreviewStatus.NeedsReview,
                "Memuriyet mahalli sınıflandırması geçersiz veya belirsiz.", legalBasis);

        var decision = _tariff.Resolve(request.Personnel);
        if (decision.Status != RateDecisionStatus.ResolvedPreliminary)
            return FromTariffDecision(decision, legalBasis);

        var result = SameDayPerDiemPreview.Calculate(
            request.Departure, request.Return, decision.DailyAmount!.Value,
            request.Station, decision.SourceReference, request.StationEvidenceReference, _constants);
        return FromDraft(result, decision);
    }

    private static bool RecognizedStation(DutyStationAssessment station) =>
        station is DutyStationAssessment.Unknown or DutyStationAssessment.Inside or DutyStationAssessment.Outside;

    private static IntegratedPreview FromDraft(DraftCalculation draft, DailyRateDecision rate)
    {
        var status = draft.Status switch
        {
            DraftStatus.Preliminary => IntegratedPreviewStatus.Preliminary,
            DraftStatus.NotEligibleByLocation => IntegratedPreviewStatus.NotEligibleByLocation,
            _ => IntegratedPreviewStatus.NeedsReview
        };
        return new IntegratedPreview(status,
            draft.UnroundedAmount,
            rate.DailyAmount, rate.Category,
            draft.Reason, draft.LegalBasis,
            rate.SourceReference, draft.EvidenceReference);
    }

    private static IntegratedPreview FromTariffDecision(
        DailyRateDecision rate, string legalBasis)
    {
        var status = rate.Status switch
        {
            RateDecisionStatus.NeedsInput => IntegratedPreviewStatus.NeedsInput,
            RateDecisionStatus.NoApplicableYear => IntegratedPreviewStatus.NoApplicableYear,
            _ => IntegratedPreviewStatus.NeedsReview
        };
        return new IntegratedPreview(status, null, null, null,
            "Tarife çözümlenemedi: " + rate.Explanation,
            legalBasis, rate.SourceReference, null);
    }

    private static IntegratedPreview Block(
        IntegratedPreviewStatus status, string explanation, string legalBasis) =>
        new(status, null, null, null, explanation, legalBasis,
            DailyRateTariff2026.SourceId + ":2026:I-B", null);
}
