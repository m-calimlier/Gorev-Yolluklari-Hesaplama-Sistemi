using System.Globalization;
using System.Text.Json;

namespace GorevYolluklari.Core.Tariffs;

/// <summary>Bu aşamada yalnız 2026 H Cetveli I-B genel memur/hizmetli kategorileri.</summary>
public enum PersonnelTariffGroup { Unknown, GeneralCivilServant, SpecialOrHighOffice }
public enum RateDecisionStatus { ResolvedPreliminary, NeedsInput, NeedsReview, NoApplicableYear }

public sealed record PersonnelTariffInput(
    DateOnly DutyDate,
    PersonnelTariffGroup Group,
    int? MonthlyOrCadreGrade,
    int? AdditionalIndicator);

public sealed record DailyRateDecision(
    RateDecisionStatus Status, decimal? DailyAmount,
    string? Category, string Explanation, string SourceReference,
    DateOnly? ValidFrom, DateOnly? ValidTo);

/// <summary>
/// H Cetvelini dosyadan alır. 2026 rakamlarını resmî cetvelin doğrulanmış I-B satırlarına
/// karşı birebir kontrol eder; başka yıl veya özel statü için tahminde bulunmaz.
/// </summary>
public sealed class DailyRateTariff2026
{
    private static readonly IReadOnlyDictionary<string, decimal> OfficialRates =
        new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["B_a"] = 890m, ["B_b"] = 880m, ["B_c"] = 870m,
            ["B_d"] = 860m, ["B_e"] = 850m
        };

    public const string SourceId = "BUT-002";
    public const string SourceUrl = "https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf";
    private readonly IReadOnlyDictionary<string, decimal> _rates;
    private DailyRateTariff2026(IReadOnlyDictionary<string, decimal> rates) => _rates = rates;

    public static DailyRateTariff2026 FromVerifiedJson(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) throw new ArgumentException("Tarife dosya yolu zorunlu.", nameof(fullPath));
        using var json = JsonDocument.Parse(File.ReadAllText(fullPath));
        var root = json.RootElement;
        if (root.GetProperty("yil").GetInt32() != 2026 ||
            root.GetProperty("kaynakID").GetString() != SourceId ||
            root.GetProperty("kaynakURL").GetString() != SourceUrl ||
            root.GetProperty("gecerlilikBaslangici").GetString() != "2026-01-01" ||
            root.GetProperty("gecerlilikSonu").GetString() != "2026-12-31" ||
            root.GetProperty("birim").GetString() != "TRY" ||
            root.GetProperty("uygulamaDurumu").GetString() != "RESMI_CETVELLE_KARSILASTIRILMIS_ON_HESAP")
            throw new InvalidDataException("Tarifenin sürümü, kaynağı veya geçerliliği doğrulanamadı.");
        var categories = root.GetProperty("kategoriler");
        if (categories.ValueKind != JsonValueKind.Array || categories.GetArrayLength() != 5)
            throw new InvalidDataException("2026 I-B kategorilerinin tamamı gerekli.");
        var actual = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var entry in categories.EnumerateArray())
        {
            var id = entry.GetProperty("anahtar").GetString() ?? "";
            var amount = entry.GetProperty("tutar").GetDecimal();
            if (!OfficialRates.TryGetValue(id, out var expected) || expected != amount || !actual.TryAdd(id, amount))
                throw new InvalidDataException($"2026 H Cetveli hatalı, tekrarlı veya eşleşmeyen kategori: {id}");
        }
        return new DailyRateTariff2026(actual);
    }

    public DailyRateDecision Resolve(PersonnelTariffInput input)
    {
        if (input.DutyDate.Year != 2026)
            return Reject(RateDecisionStatus.NoApplicableYear, "Bu işlem yılı için doğrulanmış tarife bulunmuyor.");
        if (input.Group != PersonnelTariffGroup.GeneralCivilServant)
            return Reject(RateDecisionStatus.NeedsReview,
                "Özel/üst görev veya belirsiz personel statüsü: genel memur tarifesi otomatik uygulanmaz.");
        if (input.MonthlyOrCadreGrade is null || input.AdditionalIndicator is null)
            return Reject(RateDecisionStatus.NeedsInput, "Derece ve ek gösterge alanları açıkça doldurulmalıdır; 0 geçerli ek gösterge girdisidir.");
        if (input.MonthlyOrCadreGrade is < 1 or > 15 || input.AdditionalIndicator < 0)
            return Reject(RateDecisionStatus.NeedsInput, "Derece 1-15, ek gösterge 0 veya daha büyük olmalıdır.");
        var indicator = input.AdditionalIndicator.Value;
        var grade = input.MonthlyOrCadreGrade.Value;
        string id = indicator >= 8000 ? "B_a"
                  : indicator >= 6400 ? "B_b"
                  : indicator >= 3600 ? "B_c"
                  : grade <= 4 ? "B_d" : "B_e";
        return new DailyRateDecision(RateDecisionStatus.ResolvedPreliminary, _rates[id], id,
            "2026 H Cetveli I-B seçimi; hak sahipliği, görev yeri ve özel istisnalar ayrıca incelenmelidir.",
            SourceId + ":2026:I-" + id,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    }

    private static DailyRateDecision Reject(RateDecisionStatus status, string explanation) =>
        new(status, null, null, explanation, SourceId + ":2026:I-B", null, null);
}
