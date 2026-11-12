using GorevYolluklari.Core.Records;

namespace GorevYolluklari.Core.Rules;

/// <summary>
/// 2026 yolluk bildirimindeki belgeli giderlerden brüt, damga vergisi ve
/// avans sonrası tahmini net tutarı hesaplar. Hak sahipliği kararı vermez.
/// Bu tutar, Örnek 27/28'in yerine geçen resmî ödeme onayı değildir.
/// </summary>
public sealed record YollukOdemeOzeti2026(decimal Brut, decimal DamgaVergisi, decimal Avans, decimal Net)
{
    // 2026 yılı: 488 sayılı Kanun eki (1) sayılı tablo IV/1-b, binde 7,59.
    public const decimal DamgaVergisiOrani = 0.00759m;

    public static YollukOdemeOzeti2026 Hesapla(IEnumerable<ExpenseEntry> giderler, decimal avans = 0m)
    {
        ArgumentNullException.ThrowIfNull(giderler);
        decimal brut = 0m;
        foreach (var gider in giderler)
        {
            if (gider is null || gider.Amount <= 0m ||
                decimal.Round(gider.Amount, 2, MidpointRounding.AwayFromZero) != gider.Amount)
                throw new ArgumentException("Giderler pozitif ve tam kuruş olmalıdır.", nameof(giderler));
            brut = checked(brut + gider.Amount);
        }

        if (brut > 100_000_000m || avans < 0m || avans > brut ||
            decimal.Round(avans, 2, MidpointRounding.AwayFromZero) != avans)
            throw new ArgumentOutOfRangeException(nameof(avans), "Gider ve avans tutarlarını kontrol edin.");

        var damga = decimal.Round(brut * DamgaVergisiOrani, 2, MidpointRounding.AwayFromZero);
        return new YollukOdemeOzeti2026(brut, damga, avans, brut - damga - avans);
    }
}
