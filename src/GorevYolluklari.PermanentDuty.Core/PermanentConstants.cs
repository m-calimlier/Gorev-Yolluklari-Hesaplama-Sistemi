namespace GorevYolluklari.Core.Rules;

public sealed record PermanentConstants(decimal SelfDays = 20, decimal FamilyDays = 10,
    int MaximumFamily = 4, decimal KilometerPercent = 5)
{
    public void Validate()
    {
        if (SelfDays is < 0 or > 1000 || FamilyDays is < 0 or > 1000 ||
            MaximumFamily is < 0 or > 100 || KilometerPercent is < 0 or > 1000)
            throw new ArgumentException("Sürekli görev katsayıları geçersiz.");
    }

    public decimal Self(decimal rate) => rate * SelfDays;
    public decimal Family(decimal rate, int count) => rate * FamilyDays * Math.Min(count, MaximumFamily);
    public decimal Distance(decimal rate, decimal kilometers) => rate * (KilometerPercent / 100m) * kilometers;
}
