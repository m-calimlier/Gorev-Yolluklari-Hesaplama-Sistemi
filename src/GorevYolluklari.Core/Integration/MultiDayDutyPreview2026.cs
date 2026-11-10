using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

namespace GorevYolluklari.Core.Integration;

/// <summary>
/// Aşama 5: yalnız 2026 yurtiçi genel memur/hizmetli için, daha önce mevzuat açısından
/// doğrulanmış hak kazanılan günlerin 6245 m.42 kota / 33(d) konaklama üst sınırı ÖN HESABI.
/// m.43 seyahat günlerini, memuriyet mahallini ve hak sahipliğini otomatik karara bağlamaz.
/// Çıktı ödeme emri, resmî yolluk bildirimi veya kesin hakediş değildir.
/// </summary>
public enum MultiDayPreviewStatus
{
    Preliminary,
    NeedsInput,
    NeedsReview,
    NoApplicableYear,
    NotEligibleByLocation
}

/// <param name="Date">Mevzuat/seyahat günü ayrı incelenerek onaylanmış gündelik günü.</param>
/// <param name="LodgingDayOrdinal">İlgili görevlendirmenin onaylı konaklama gün sırası; gece yoksa null.</param>
/// <param name="DocumentedLodgingTL">Geceye düşen belgeli tutar; gece yoksa null.</param>
/// <param name="LodgingReceiptReference">Fatura/konaklama belgesinin yerel referansı; gece yoksa null.</param>
public sealed record ReviewedDutyDay(
    DateOnly Date,
    int? LodgingDayOrdinal = null,
    decimal? DocumentedLodgingTL = null,
    string? LodgingReceiptReference = null);

public sealed record MultiDayDutyRequest2026(
    PersonnelTariffInput Personnel,
    DutyStationAssessment Station,
    bool AssignmentReviewed,
    bool SamePersonPlaceWorkAnnualPeriodReviewed,
    bool PriorDaysLedgerReviewed,
    bool EligibleDutyAndTravelDaysReviewed,
    bool Article42ExceptionsExcluded,
    bool LodgingArticle33dReviewed,
    int ApprovedPreviouslyCountedDays,
    IReadOnlyList<ReviewedDutyDay> ReviewedDays,
    string? AssignmentEvidenceReference,
    string? StationEvidenceReference,
    string? PreviousDaysLedgerReference);

public sealed record MultiDayPreviewLine(
    DateOnly Date,
    int Article42Ordinal,
    int LodgingOrdinal,
    decimal DailyRate,
    decimal DailyAllowanceUnrounded,
    decimal? DocumentedLodgingTL,
    decimal? LodgingLimitUnrounded,
    decimal LodgingAllowanceUnrounded,
    string? LodgingReceiptReference);

public sealed record MultiDayPreviewResult(
    MultiDayPreviewStatus Status,
    string Explanation,
    string LegalBasis,
    string TariffReference,
    decimal? DailyRate,
    decimal? PerDiemTotalUnrounded,
    decimal? LodgingTotalUnrounded,
    decimal? CombinedTotalUnrounded,
    IReadOnlyList<MultiDayPreviewLine> Lines)
{
    public bool IsOfficialPaymentDocument => false;
}

public sealed class MultiDayDutyPreviewEngine2026
{
    private readonly DailyRateTariff2026 _tariff;
    private const string Legal = "6245 m.3, m.33(d), m.39, m.42, m.43; 2026 H Cetveli I-B dipnot (1) (ön hesap)";
    private const string Tariff = "BUT-002:2026:I-B";

    public MultiDayDutyPreviewEngine2026(DailyRateTariff2026 tariff) =>
        _tariff = tariff ?? throw new ArgumentNullException(nameof(tariff));

    public MultiDayPreviewResult Preview(MultiDayDutyRequest2026 request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Personnel);
        if (request.ReviewedDays is null || request.ReviewedDays.Count == 0)
            return Block(MultiDayPreviewStatus.NeedsInput, "Önceden incelenmiş gündelik günleri gerekli.");
        if (request.ReviewedDays.Any(day => day is null))
            return Block(MultiDayPreviewStatus.NeedsInput, "Boş gün kaydı kabul edilmez.");
        if (request.ReviewedDays.Count > 180 || request.ApprovedPreviouslyCountedDays is < 0 or > 180)
            return Block(MultiDayPreviewStatus.NeedsReview, "Geçmiş gün sayısı/geçerli dönem sınırı inceleme gerektiriyor.");
        if (request.ReviewedDays.Any(day => day.Date.Year != 2026) || request.Personnel.DutyDate.Year != 2026)
            return Block(MultiDayPreviewStatus.NoApplicableYear, "2026 harici günler için bu tarife uygulanamaz.");
        if (request.Personnel.DutyDate != request.ReviewedDays[0].Date)
            return Block(MultiDayPreviewStatus.NeedsReview, "Tarife işlem tarihi ilk incelenmiş günle uyuşmuyor.");
        for (var i = 1; i < request.ReviewedDays.Count; i++)
            if (request.ReviewedDays[i].Date <= request.ReviewedDays[i - 1].Date)
                return Block(MultiDayPreviewStatus.NeedsReview, "Günler tekrarsız ve kronolojik olmalıdır.");
        if (request.Station == DutyStationAssessment.Inside)
            return Block(MultiDayPreviewStatus.NotEligibleByLocation, "Memuriyet mahalli içinde gündelik ön hesabı engellendi.");
        if (request.Station != DutyStationAssessment.Outside || string.IsNullOrWhiteSpace(request.StationEvidenceReference))
            return Block(MultiDayPreviewStatus.NeedsReview, "Memuriyet mahalli dışı olma durumu belgeli doğrulanmalı.");
        if (!request.AssignmentReviewed || string.IsNullOrWhiteSpace(request.AssignmentEvidenceReference))
            return Block(MultiDayPreviewStatus.NeedsReview, "Görevlendirme oluru/kanıtı incelenmedi.");
        if (!request.SamePersonPlaceWorkAnnualPeriodReviewed || !request.PriorDaysLedgerReviewed ||
            string.IsNullOrWhiteSpace(request.PreviousDaysLedgerReference))
            return Block(MultiDayPreviewStatus.NeedsReview,
                "m.42 için aynı kişi, aynı yer, aynı iş, bir yıllık dönem ve eski görev günleri belgeli doğrulanmalı.");
        if (!request.EligibleDutyAndTravelDaysReviewed)
            return Block(MultiDayPreviewStatus.NeedsReview,
                "m.43 yolculuk günleri, geceleme ve hak kazanılan günler yetkili tarafından incelenmeli.");
        if (!request.Article42ExceptionsExcluded)
            return Block(MultiDayPreviewStatus.NeedsReview, "m.42 istisnalarına girilip girilmediği incelenmeli.");
        if (request.ApprovedPreviouslyCountedDays + request.ReviewedDays.Count > 180)
            return Block(MultiDayPreviewStatus.NeedsReview,
                "180 gün sınırı aşan dönem: 181. güne kendiliğinden yeni gündelik atanamaz.");

        var rate = _tariff.Resolve(request.Personnel);
        if (rate.Status != RateDecisionStatus.ResolvedPreliminary)
        {
            var kind = rate.Status switch
            {
                RateDecisionStatus.NeedsInput => MultiDayPreviewStatus.NeedsInput,
                RateDecisionStatus.NoApplicableYear => MultiDayPreviewStatus.NoApplicableYear,
                _ => MultiDayPreviewStatus.NeedsReview
            };
            return Block(kind, "Gündelik tarifesi çözümlenemedi: " + rate.Explanation, rate.SourceReference);
        }

        // Konaklama her incelenmiş gündelik gününe en fazla bir gecelik kayıt olarak girilir.
        // Konaklama gün sıra numarası otomatik sıfırlanmaz; m.42 sıra numarasıyla
        // farklılık varsa (yeni görevlendirme/ara verme vb.) hukuki inceleme gerektirir.
        var lodgingOrdinals = new HashSet<int>();
        var lines = new List<MultiDayPreviewLine>(request.ReviewedDays.Count);
        var dailyTotal = 0m;
        var lodgingTotal = 0m;
        for (var i = 0; i < request.ReviewedDays.Count; i++)
        {
            var item = request.ReviewedDays[i];
            var count = checked(request.ApprovedPreviouslyCountedDays + i + 1);
            var baseRate = rate.DailyAmount!.Value;
            var actualPerDiem = count <= 90 ? baseRate : baseRate * 2m / 3m;
            var hasAnyLodgingField = item.DocumentedLodgingTL.HasValue || item.LodgingDayOrdinal.HasValue ||
                                     !string.IsNullOrWhiteSpace(item.LodgingReceiptReference);
            decimal? cap = null;
            decimal? receipt = null;
            var accommodation = 0m;
            var lodgingOrdinal = 0;
            if (hasAnyLodgingField)
            {
                if (!request.LodgingArticle33dReviewed)
                    return Block(MultiDayPreviewStatus.NeedsReview, "33(d) konaklama hak sahipliği ayrı incelenmeli.");
                if (!item.DocumentedLodgingTL.HasValue || item.DocumentedLodgingTL.Value < 0 ||
                    !item.LodgingDayOrdinal.HasValue || string.IsNullOrWhiteSpace(item.LodgingReceiptReference))
                    return Block(MultiDayPreviewStatus.NeedsInput, "Konaklama tutarı, görev günü sırası ve belge referansı eksik/geçersiz.");
                lodgingOrdinal = item.LodgingDayOrdinal.Value;
                if (lodgingOrdinal is < 1 or > 180 || !lodgingOrdinals.Add(lodgingOrdinal))
                    return Block(MultiDayPreviewStatus.NeedsReview, "Konaklama gün sırası geçersiz veya yinelenmiş.");
                if (lodgingOrdinal != count)
                    return Block(MultiDayPreviewStatus.NeedsReview,
                        "m.42 gün sırası ve konaklama dönem sıra numarası farklı: yeni görev/ara verme yorumu için inceleme gerekir.");
                receipt = item.DocumentedLodgingTL.Value;
                cap = lodgingOrdinal <= 10 ? baseRate * 160m / 100m
                    : lodgingOrdinal <= 90 ? baseRate * 50m / 100m
                    : actualPerDiem * 40m / 100m;
                accommodation = Math.Min(receipt.Value, cap.Value);
            }
            dailyTotal += actualPerDiem;
            lodgingTotal += accommodation;
            lines.Add(new MultiDayPreviewLine(item.Date, count, lodgingOrdinal, baseRate,
                actualPerDiem, receipt, cap, accommodation, item.LodgingReceiptReference));
        }
        return new MultiDayPreviewResult(MultiDayPreviewStatus.Preliminary,
            "m.42 gün sayacı ve 2026 H Cetveli 33(d) konaklama üst sınırı; kuruş yuvarlaması yapılmadı.",
            Legal, rate.SourceReference, rate.DailyAmount,
            dailyTotal, lodgingTotal, dailyTotal + lodgingTotal, lines);
    }

    private static MultiDayPreviewResult Block(MultiDayPreviewStatus status, string reason, string? tariff = null) =>
        new(status, reason, Legal, tariff ?? Tariff, null, null, null, null,
            Array.Empty<MultiDayPreviewLine>());
}
