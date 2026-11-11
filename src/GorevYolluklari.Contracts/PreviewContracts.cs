using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

namespace GorevYolluklari.Core.Integration;

/// <summary>
/// Aşama 4 yalnızca ön hesaplamadır; ödeme emri, yolluk bildirimi,
/// nihai hak sahipliği kararı veya resmî bordro oluşturmaz.
/// </summary>
public enum IntegratedPreviewStatus
{
    Preliminary,
    NeedsInput,
    NeedsReview,
    NoApplicableYear,
    NotEligibleByLocation
}

public sealed record IntegratedPreview(
    IntegratedPreviewStatus Status,
    decimal? UnroundedAmount,
    decimal? DailyRate,
    string? DailyCategory,
    string Explanation,
    string LegalBasis,
    string TariffReference,
    string? EvidenceReference)
{
    public bool IsOfficialPaymentDocument => false;
}

public sealed record SameDayDutyRequest(
    PersonnelTariffInput Personnel,
    DateTime Departure,
    DateTime Return,
    DutyStationAssessment Station,
    bool AssignmentReviewed,
    string? StationEvidenceReference);

public sealed record RelocationDutyRequest(
    PersonnelTariffInput Personnel,
    DutyStationAssessment Station,
    bool EligibilityReviewed,
    bool FamilyReviewed,
    bool JointCivilServantSpouseTransfer,
    int ApprovedFamilyMemberCount,
    decimal ApprovedKilometers,
    string? DistanceEvidenceReference);
