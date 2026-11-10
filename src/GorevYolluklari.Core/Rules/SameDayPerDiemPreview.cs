namespace GorevYolluklari.Core.Rules;

/// <summary>
/// 6245 m.39 uyarınca yurt içi, geceleme olmayan, aynı takvim gününde biten seyahatlerin
/// yalnız gündelik oranını ön hesaplar. Öğle / akşam sınır saatleri eşitliklerinde yorum
/// yapmaz; inceleme ister. Tutarlar kuruşa yuvarlanmaz.
/// </summary>
public static class SameDayPerDiemPreview
{
    private static readonly TimeSpan Noon = new(13, 0, 0);
    private static readonly TimeSpan Evening = new(19, 0, 0);

    public static DraftCalculation Calculate(
        DateTime departure,
        DateTime returnTime,
        decimal dailyRate,
        DutyStationAssessment station,
        string tariffReference,
        string? evidenceReference)
    {
        if (dailyRate <= 0m) throw new ArgumentOutOfRangeException(nameof(dailyRate));
        if (string.IsNullOrWhiteSpace(tariffReference)) throw new ArgumentException("Tarife kaynağı zorunludur.", nameof(tariffReference));
        const string basis = "6245 sayılı Kanun m.3 ve m.39 (ön hesap)";

        if (station == DutyStationAssessment.Unknown || string.IsNullOrWhiteSpace(evidenceReference))
            return new(DraftStatus.NeedsReview, "Memuriyet mahalli dışı olma durumu ve dayanak belgesi doğrulanmalı.", null, basis, tariffReference, evidenceReference);
        if (station == DutyStationAssessment.Inside)
            return new(DraftStatus.NotEligibleByLocation, "Memuriyet mahalli içi: 39. madde gereği gündelik hesaplanmaz.", 0m, basis, tariffReference, evidenceReference);
        if (returnTime <= departure)
            return new(DraftStatus.NeedsReview, "Saat aralığı geçersiz veya eksik.", null, basis, tariffReference, evidenceReference);
        if (departure.Date != returnTime.Date)
            return new(DraftStatus.NeedsReview, "Gecelemeli/çok günlü seyahat için ayrı kurallar gerekir (m.42-43).", null, basis, tariffReference, evidenceReference);
        if (TouchesBoundary(departure.TimeOfDay) || TouchesBoundary(returnTime.TimeOfDay))
            return new(DraftStatus.NeedsReview, "13:00 veya 19:00 sınırında tam eşitlik: kurum yorumu gerekir.", null, basis, tariffReference, evidenceReference);

        var mealCount = (Contains(departure.TimeOfDay, returnTime.TimeOfDay, Noon) ? 1 : 0)
                      + (Contains(departure.TimeOfDay, returnTime.TimeOfDay, Evening) ? 1 : 0);
        var raw = dailyRate * mealCount / 3m;
        return new(DraftStatus.Preliminary, $"m.39: {mealCount}/3 gündelik; küsurat henüz yuvarlanmadı.", raw, basis, tariffReference, evidenceReference);
    }

    private static bool Contains(TimeSpan from, TimeSpan to, TimeSpan time) => from < time && time < to;
    private static bool TouchesBoundary(TimeSpan time) => time == Noon || time == Evening;
}
