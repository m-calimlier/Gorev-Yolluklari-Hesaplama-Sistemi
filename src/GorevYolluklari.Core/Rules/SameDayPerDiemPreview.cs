namespace GorevYolluklari.Core.Rules;

/// <summary>
/// 6245 m.39 uyarınca yurt içi, geceleme olmayan, aynı takvim gününde biten seyahatlerin
/// yalnız gündelik oranını ön hesaplar. Öğle / akşam sınır saatleri eşitliklerinde yorum
/// yapmaz; inceleme ister. Tutarlar kuruşa yuvarlanmaz.
/// </summary>
public static class SameDayPerDiemPreview
{
    public static DraftCalculation Calculate(
        DateTime departure,
        DateTime returnTime,
        decimal dailyRate,
        DutyStationAssessment station,
        string tariffReference,
        string? evidenceReference, TemporaryConstants? constants = null)
    {
        var rules = constants ?? new();
        rules.Validate();
        var noon = TimeSpan.FromHours(rules.LunchHour);
        var evening = TimeSpan.FromHours(rules.DinnerHour);
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
        if (departure.TimeOfDay == noon || departure.TimeOfDay == evening || returnTime.TimeOfDay == noon || returnTime.TimeOfDay == evening)
            return new(DraftStatus.NeedsReview, $"{rules.LunchHour:00}:00 veya {rules.DinnerHour:00}:00 sınırında tam eşitlik: kurum yorumu gerekir.", null, basis, tariffReference, evidenceReference);

        var mealCount = (Contains(departure.TimeOfDay, returnTime.TimeOfDay, noon) ? 1 : 0)
                      + (Contains(departure.TimeOfDay, returnTime.TimeOfDay, evening) ? 1 : 0);
        var raw = dailyRate * mealCount / rules.MealDivisor;
        return new(DraftStatus.Preliminary, $"m.39: {mealCount}/{rules.MealDivisor} gündelik; küsurat henüz yuvarlanmadı.", raw, basis, tariffReference, evidenceReference);
    }

    private static bool Contains(TimeSpan from, TimeSpan to, TimeSpan time) => from < time && time < to;
}
