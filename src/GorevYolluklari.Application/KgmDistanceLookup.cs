using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace GorevYolluklari.Application;

public sealed record KgmPlace(string Province, string District)
{
    public override string ToString() => $"{Province} / {District}";
}

public static class KgmDistanceLookup
{
    public const string SourceUrl = "https://vatandas.kgm.gov.tr/DistrictDistance";
    private static readonly StringComparer Turkish = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);

    public static async Task<int> FetchAsync(KgmPlace from, KgmPlace to, CancellationToken cancellationToken)
    {
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(10),
            MaxResponseContentBufferSize = 512 * 1024
        };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var token = deadline.Token;
        var page = await http.GetStringAsync(SourceUrl, token).ConfigureAwait(false);
        var citySelect = Match(page, """<select\b[^>]*\bid="SourceCityId"[^>]*>(.*?)</select>""");
        var fromCity = CityId(citySelect, from.Province);
        var toCity = CityId(citySelect, to.Province);
        var fromOptions = Districts(http, fromCity, token);
        var toOptions = fromCity == toCity ? fromOptions : Districts(http, toCity, token);
        await Task.WhenAll(fromOptions, toOptions).ConfigureAwait(false);
        var fromDistrict = DistrictId(await fromOptions.ConfigureAwait(false), from.District);
        var toDistrict = DistrictId(await toOptions.ConfigureAwait(false), to.District);
        var verification = Match(page, """<input\b[^>]*\bname="__RequestVerificationToken"[^>]*\bvalue="([^"]+)"[^>]*>""");

        using var fields = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["SourceCityId"] = Number(fromCity), ["SourceDistrictId"] = Number(fromDistrict),
            ["TargetCityId"] = Number(toCity), ["TargetDistrictId"] = Number(toDistrict),
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(verification)
        });
        using var response = await http.PostAsync(SourceUrl, fields, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
        var result = WebUtility.HtmlDecode(Match(html, """<span\b[^>]*\bid="lblResult"[^>]*>(.*?)</span>""")).Trim();
        var expected = $"{from.Province}/{from.District} ile {to.Province}/{to.District} arasındaki mesafe ";
        if (!CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IsPrefix(result, expected, CompareOptions.IgnoreCase))
            throw new InvalidDataException("KGM yanıtında seçilen güzergâh doğrulanamadı.");
        var value = result[expected.Length..];
        if (!value.EndsWith(" KM", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(value.AsSpan(0, value.Length - 3), NumberStyles.None, CultureInfo.InvariantCulture, out var km) ||
            km is < 0 or > 5000 || (km == 0 && (!Turkish.Equals(from.Province, to.Province) || !Turkish.Equals(from.District, to.District))))
            throw new InvalidDataException("KGM yanıtındaki mesafe geçerli değil.");
        return km;
    }

    private static async Task<DistrictOption[]> Districts(HttpClient http, int city, CancellationToken token)
    {
        using var request = new FormUrlEncodedContent(new Dictionary<string, string> { ["cityId"] = Number(city) });
        using var reply = await http.PostAsync(SourceUrl + "/GetDistricts", request, token).ConfigureAwait(false);
        reply.EnsureSuccessStatusCode();
        return await reply.Content.ReadFromJsonAsync<DistrictOption[]>(cancellationToken: token).ConfigureAwait(false)
            ?? throw new InvalidDataException("KGM ilçe listesi boş.");
    }

    private static int CityId(string select, string name)
    {
        foreach (Match option in Regex.Matches(select, """<option\b[^>]*\bvalue="(\d+)"[^>]*>([^<]+)</option>""",
                     RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            if (Turkish.Equals(WebUtility.HtmlDecode(option.Groups[2].Value.Trim()), name) &&
                int.TryParse(option.Groups[1].Value, out var id) && id > 0)
                return id;
        throw new InvalidDataException("Seçilen il KGM listesinde bulunamadı.");
    }

    private static int DistrictId(DistrictOption[] choices, string name)
    {
        var matches = choices.Where(option => Turkish.Equals(option.Name, name) && option.Id > 0).ToArray();
        if (matches.Length != 1) throw new InvalidDataException("Seçilen ilçe KGM listesinde tekil olarak bulunamadı.");
        return matches[0].Id;
    }

    private static string Match(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
        if (!match.Success) throw new InvalidDataException("KGM sayfası beklenen sorgu biçiminde yanıt vermedi.");
        return match.Groups[1].Value;
    }

    private static string Number(int id) => id.ToString(CultureInfo.InvariantCulture);
    private sealed record DistrictOption(int Id, string Name);
}
