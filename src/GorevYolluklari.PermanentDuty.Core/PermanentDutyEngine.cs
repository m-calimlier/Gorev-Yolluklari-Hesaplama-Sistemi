using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
namespace GorevYolluklari.Core.Integration;
public sealed class PermanentDutyEngine
{
 private readonly DailyRateTariff2026 _tariff;
    private readonly PermanentConstants? _constants;

    public PermanentDutyEngine(DailyRateTariff2026 tariff, PermanentConstants? constants = null)
    {
        _tariff = tariff ?? throw new ArgumentNullException(nameof(tariff));
        _constants = constants;
    }

    public IntegratedPreview PreviewRelocation(RelocationDutyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Personnel);
        const string legalBasis = "6245 sayılı Kanun m.10, m.45 ve m.47 (ön hesap)";
        // Girdileri tarife sonucundan bağımsız denetle: geçersiz sayıyı maskeleme.
        if (request.ApprovedFamilyMemberCount < 0)
            throw new ArgumentOutOfRangeException(nameof(request.ApprovedFamilyMemberCount));
        if (request.ApprovedKilometers < 0m)
            throw new ArgumentOutOfRangeException(nameof(request.ApprovedKilometers));
        if (!RecognizedStation(request.Station))
            return Block(IntegratedPreviewStatus.NeedsReview,
                "Memuriyet mahalli sınıflandırması geçersiz veya belirsiz.", legalBasis);

        var decision = _tariff.Resolve(request.Personnel);
        if (decision.Status != RateDecisionStatus.ResolvedPreliminary)
            return FromTariffDecision(decision, legalBasis);

        var result = RelocationExpensePreview.Calculate(
            decision.DailyAmount!.Value,
            request.ApprovedFamilyMemberCount,
            request.ApprovedKilometers,
            request.Station,
            request.EligibilityReviewed,
            request.FamilyReviewed,
            request.JointCivilServantSpouseTransfer,
            decision.SourceReference,
            request.DistanceEvidenceReference, _constants);
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
