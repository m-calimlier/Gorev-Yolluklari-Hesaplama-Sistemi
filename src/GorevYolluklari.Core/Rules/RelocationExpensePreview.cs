namespace GorevYolluklari.Core.Rules;

/// <summary>
/// 6245 m.45 a/b/c formülünün ön hesap modülü. Aile ve kilometre hak sahipliği
/// önceden incelenmiş olmalıdır. Yol ücreti ve seyahat gündelikleri bu tutara dahil değildir.
/// Aynı eski/yeni mahallerde atanan ikinci memur eşin m.45(c) değişken unsuru yarı hesaplanır.
/// Bu seçim belge/hak sahipliği onayı değildir.
/// </summary>
public static class RelocationExpensePreview
{
    public static DraftCalculation Calculate(
        decimal dailyRate,
        int approvedFamilyMemberCount,
        decimal approvedKilometers,
        DutyStationAssessment station,
        bool eligibilityReviewed,
        bool familyReviewed,
        bool jointCivilServantSpouseTransfer,
        string tariffReference,
        string? distanceEvidenceReference, PermanentConstants? constants = null)
    {
        var rules = constants ?? new();
        rules.Validate();
        if (dailyRate <= 0m) throw new ArgumentOutOfRangeException(nameof(dailyRate));
        if (approvedFamilyMemberCount < 0) throw new ArgumentOutOfRangeException(nameof(approvedFamilyMemberCount));
        if (approvedKilometers < 0m) throw new ArgumentOutOfRangeException(nameof(approvedKilometers));
        if (string.IsNullOrWhiteSpace(tariffReference)) throw new ArgumentException("Tarife kaynağı zorunludur.", nameof(tariffReference));
        const string basis = "6245 sayılı Kanun m.10, m.45 ve m.47 (ön hesap)";
        if (station == DutyStationAssessment.Unknown || !eligibilityReviewed || !familyReviewed || string.IsNullOrWhiteSpace(distanceEvidenceReference))
            return new(DraftStatus.NeedsReview, "Hak sahipliği, aile durumu ve kaynaklı mesafe kontrol edilmelidir.", null, basis, tariffReference, distanceEvidenceReference);
        if (station == DutyStationAssessment.Inside)
            return new(DraftStatus.NotEligibleByLocation, "Yer değişikliği için farklı memuriyet mahalli doğrulanmadı.", 0m, basis, tariffReference, distanceEvidenceReference);
        decimal selfFixed = rules.Self(dailyRate);
        decimal familyFixed = rules.Family(dailyRate, approvedFamilyMemberCount);
        decimal distanceCost = rules.Distance(dailyRate, approvedKilometers);
        if (jointCivilServantSpouseTransfer) distanceCost /= 2m;
        decimal raw = selfFixed + familyFixed + distanceCost;
        return new(DraftStatus.Preliminary,
            $"m.45: {rules.SelfDays}x + {Math.Min(approvedFamilyMemberCount, rules.MaximumFamily) * rules.FamilyDays}x aile ({rules.MaximumFamily * rules.FamilyDays}x tavan) + %{rules.KilometerPercent} x {approvedKilometers} km" +
            (jointCivilServantSpouseTransfer ? " (ikinci memur eş için değişken unsur %50)" : "") +
            ". Seyahat ücretleri dahil değil.",
            raw, basis, tariffReference, distanceEvidenceReference);
    }
}
