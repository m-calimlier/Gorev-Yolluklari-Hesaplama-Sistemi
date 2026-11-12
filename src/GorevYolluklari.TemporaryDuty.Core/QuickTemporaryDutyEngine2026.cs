using GorevYolluklari.Core.Tariffs;

namespace GorevYolluklari.Core.Integration;

/// <summary>
/// Sadece yurt içi 2026 genel memur/hizmetli için, kullanıcının verdiği
/// sayısal bilgiler üzerinden geçici görev ÖN HESABI.
/// Bu tür iş kuralları Sürekli Görev çekirdeğine taşınmaz.
/// Memuriyet mahalli ve hak sahipliği bakımından resmî onay oluşturmaz.
/// </summary>
public sealed record QuickTemporaryDutyRequest2026(
    PersonnelTariffInput Personnel,
    decimal OutboundFare,
    int OutboundDailyThirds,
    int OutboundTravelDays,
    decimal ReturnFare,
    int ReturnDailyThirds,
    int ReturnTravelDays,
    int WorkDays,
    int LodgingNights,
    decimal NightlyLodgingPrice,
    decimal OtherEligibleCosts);

public sealed record QuickTemporaryDutyResult2026(
    decimal DailyRate,
    decimal OutboundFare,
    decimal OutboundDaily,
    decimal WorkDaily,
    decimal ReturnFare,
    decimal ReturnDaily,
    decimal Lodging,
    decimal OtherEligibleCosts,
    string TariffReference,
    string RuleReference)
{
    public decimal DailyTotal => OutboundDaily + WorkDaily + ReturnDaily;
    public decimal Total => OutboundFare + DailyTotal + ReturnFare + Lodging + OtherEligibleCosts;
    public bool IsOfficialPaymentDocument => false;
}

public sealed class QuickTemporaryDutyEngine2026(DailyRateTariff2026 tariff)
{
    private static decimal Money(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public QuickTemporaryDutyResult2026 Calculate(QuickTemporaryDutyRequest2026 r)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(r.Personnel);
        if (r.Personnel.DutyDate.Year != 2026)
            throw new ArgumentException("Bu sürüm yalnız 2026 bütçe dönemi içindir.");
        if (r.OutboundDailyThirds is < 0 or > 3 || r.ReturnDailyThirds is < 0 or > 3)
            throw new ArgumentException("Yol gündeliği oranı 0, 1/3, 2/3 veya tam olmalıdır.");
        if (r.OutboundTravelDays is < 0 or > 180 || r.ReturnTravelDays is < 0 or > 180 ||
            r.WorkDays is < 0 or > 180 || r.LodgingNights is < 0 or > 180)
            throw new ArgumentException("Gün ve gece sayıları 0–180 aralığında olmalıdır.");
        if (r.LodgingNights > r.WorkDays)
            throw new ArgumentException("Konaklama gecesi görev günlerinden fazla olamaz.");
        var monetary = new[] { r.OutboundFare, r.ReturnFare, r.NightlyLodgingPrice, r.OtherEligibleCosts };
        if (monetary.Any(v => v < 0m || v > 10_000_000m || v != Money(v)))
            throw new ArgumentException("Tutarlar pozitif veya sıfır, tam kuruş ve 10 milyon TL'yi aşmamalıdır.");
        if (r.NightlyLodgingPrice > 0m && r.LodgingNights == 0)
            throw new ArgumentException("Günlük konaklama ücreti varsa gece sayısını da girin.");
        if (r.WorkDays + r.OutboundTravelDays + r.ReturnTravelDays == 0 &&
            r.OutboundFare + r.ReturnFare + r.OtherEligibleCosts == 0m)
            throw new ArgumentException("Hesaplanacak görev günü veya uygun gider girilmedi.");

        var rateDecision = tariff.Resolve(r.Personnel);
        if (rateDecision.Status != RateDecisionStatus.ResolvedPreliminary ||
            rateDecision.DailyAmount is not decimal rate)
            throw new ArgumentException("Gündelik tarifesi belirlenemedi: " + rateDecision.Explanation);

        // 6245 m.39 oranı kullanıcıca seçilir; yol günü 6245 m.43 uyarınca
        // kullanıcı tarafından girilmiş süredir. Saatlerden veya görev
        // güzergâhından hak sahipliği sonucu çıkarılmaz.
        var outboundDaily = Money(rate * r.OutboundDailyThirds * r.OutboundTravelDays / 3m);
        var returnDaily = Money(rate * r.ReturnDailyThirds * r.ReturnTravelDays / 3m);
        // 6245 m.42: aynı kişi / aynı iş / aynı yerde 180 gün, ilk 90 tam,
        // sonraki 90 gün 2/3. Önceki dönem günleri girişte sorulmadığı için
        // sıfır varsayılır; resmî hak sahipliği incelemesi kullanıcı dışındadır.
        var workFull = Math.Min(r.WorkDays, 90);
        var workReduced = Math.Max(0, r.WorkDays - 90);
        var workDaily = Money(rate * workFull + rate * workReduced * 2m / 3m);

        // 2026 H Cetveli I. bölüm dipnot (1), 6245 m.33(d).
        // İlk 10 gecede 1,60x; takip eden 80'de 0,50x;
        // son 90'da (2/3 hak edilen gündelik) x 0,40.
        decimal lodgingRaw = 0m;
        for (var night = 1; night <= r.LodgingNights; night++)
        {
            var limit = night <= 10 ? rate * 1.60m
                : night <= 90 ? rate * 0.50m
                : rate * (2m / 3m) * 0.40m;
            lodgingRaw += Math.Min(r.NightlyLodgingPrice, limit);
        }
        return new(rate, Money(r.OutboundFare), outboundDaily, workDaily,
            Money(r.ReturnFare), returnDaily, Money(lodgingRaw), Money(r.OtherEligibleCosts),
            rateDecision.SourceReference,
            "6245 m.14, 27, 33(d), 39, 42, 43; 2026 H Cetveli I-B dipnot (1)");
    }
}
