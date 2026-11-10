namespace GorevYolluklari.Core.Rules;

/// <summary>Bu sınıflar yalnızca ön hesap yapar. Harcırah hak sahipliği ve resmî tutar onayı değildir.</summary>
public enum DutyStationAssessment { Unknown, Inside, Outside }
public enum DraftStatus { NeedsReview, NotEligibleByLocation, Preliminary }

public sealed record DraftCalculation(
    DraftStatus Status,
    string Reason,
    decimal? UnroundedAmount,
    string LegalBasis,
    string TariffReference,
    string? EvidenceReference = null);
