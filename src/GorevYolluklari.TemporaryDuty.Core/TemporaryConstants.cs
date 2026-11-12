namespace GorevYolluklari.Core.Rules;

public sealed record TemporaryConstants(
    int LunchHour = 13, int DinnerHour = 19, int MealDivisor = 3,
    int FullDays = 90, int MaxDays = 180, int ReducedNumerator = 2, int ReducedDivisor = 3,
    int FirstLodgingDays = 10, decimal FirstLodgingPercent = 160,
    decimal MiddleLodgingPercent = 50, decimal LastLodgingPercent = 40)
{
    public void Validate()
    {
        if (LunchHour < 1 || DinnerHour > 23 || LunchHour >= DinnerHour || MealDivisor is < 2 or > 100 ||
            FirstLodgingDays < 1 || FullDays < FirstLodgingDays || MaxDays < FullDays || MaxDays > 366 ||
            ReducedNumerator < 1 || ReducedDivisor < ReducedNumerator || ReducedDivisor > 100 ||
            FirstLodgingPercent is < 0 or > 1000 || MiddleLodgingPercent is < 0 or > 1000 ||
            LastLodgingPercent is < 0 or > 1000)
            throw new ArgumentException("Geçici görev sabitleri geçersiz. Saat, gün sırası, oran ve paydaları kontrol edin.");
    }
}
