using System.Globalization;
using System.Text.RegularExpressions;

namespace GorevYolluklari.Core.Documents;

/// <summary>
/// Aşama 7: Örnek 27/28 ile karşılaştırmalı belge TASLAĞI. Resmî bordro değildir.
/// Her satır doğrulanmış iki ondalıklı TL tutarı ve kanıt referansı ile girilir.
/// Aşama 2-6'daki aday gün/tutarlar bu sınıfa doğrudan otomatik aktarılmaz.
/// </summary>
public enum DraftKind { TemporaryDuty = 27, PermanentDuty = 28 }
public enum DraftLineKind { DailyAllowance, TicketOrTravel, Accommodation, FamilyDaily, FamilyTravel, Relocation }
public enum DraftDecisionStatus { DraftReady, NeedsInput, NeedsReview, Inconsistent }

public sealed record DraftExpenseLine(
    string LineId, DraftLineKind Kind, string Description,
    decimal AmountTry, string EvidenceReference, string LegalReference);

public sealed record DraftDocumentRequest(
    DraftKind Kind, string DocumentNumber,
    string PersonnelFullName, string PersonnelTitle, string Institution,
    string Origin, string Destination, DateOnly Begins, DateOnly Ends,
    int BudgetYear, string TariffReference, string RuleVersion,
    string AssignmentEvidence, string StationEvidence,
    bool AssignmentAndStationReviewed, bool EntitlementAndAmountsReviewed,
    IReadOnlyList<DraftExpenseLine> Lines,
    decimal AdvanceTry, string? AdvanceEvidence,
    decimal IndependentGrossTry, decimal IndependentNetTry);

public sealed record DraftDocument(
    DraftKind Kind, string DocumentNumber, string PersonnelFullName,
    string PersonnelTitle, string Institution, string Origin, string Destination,
    DateOnly Begins, DateOnly Ends, int BudgetYear,
    string TariffReference, string RuleVersion,
    string AssignmentEvidence, string StationEvidence,
    IReadOnlyList<DraftExpenseLine> Lines,
    decimal GrossTry, decimal AdvanceTry, decimal NetTry)
{
    public bool IsOfficialPaymentDocument => false;
    public bool IsCalculationOutput { get; init; }
    public string VisibilityNotice => IsCalculationOutput
        ? "YOLLUK HESAPLAMA ÇIKTISI - RESMİ ONAY BELGESİ DEĞİLDİR"
        : "KONTROL TASLAĞI - ÖDEME BELGESİ DEĞİLDİR";
    public string RequiredFormCode => Kind == DraftKind.TemporaryDuty ? "Örnek 27" : "Örnek 28";
}

public sealed record DraftDecision(DraftDecisionStatus Status, string Reason, DraftDocument? Document)
{
    public bool MayPrintDraft => Status == DraftDecisionStatus.DraftReady && Document is not null;
    public bool IsOfficialPaymentDocument => false;
}

public static class DraftDocumentService
{
    private static readonly Regex DocumentIdPattern = new(@"^[A-Za-z0-9][A-Za-z0-9_-]{3,39}$", RegexOptions.CultureInvariant);
    private const decimal MaximumAllowed = 100_000_000m;
    private static readonly HashSet<DraftLineKind> TemporaryKinds =
        new() { DraftLineKind.DailyAllowance, DraftLineKind.TicketOrTravel, DraftLineKind.Accommodation };
    private static readonly HashSet<DraftLineKind> PermanentKinds =
        new() { DraftLineKind.DailyAllowance, DraftLineKind.TicketOrTravel,
            DraftLineKind.FamilyDaily, DraftLineKind.FamilyTravel, DraftLineKind.Relocation };

    public static DraftDecision Create(DraftDocumentRequest request) => CreateCore(request, requireReview: true);

    /// <summary>
    /// Kullanıcıdan aynı tutarları ikinci kez istemeyen hesaplama PDF'si üretir.
    /// Belge/hak sahipliği incelemesi yapıldığını iddia etmez; sayısal ve
    /// yapısal denetimleri atlamaz. Onaylanmış Örnek 27/28 belgesi değildir.
    /// </summary>
    public static DraftDecision CreateCalculationCopy(DraftDocumentRequest request) =>
        CreateCore(request, requireReview: false);

    private static DraftDecision CreateCore(DraftDocumentRequest request, bool requireReview)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Kind is not (DraftKind.TemporaryDuty or DraftKind.PermanentDuty))
            return Refuse(DraftDecisionStatus.NeedsInput, "Belge türü Örnek 27 ya da Örnek 28 olmalıdır.");
        if (string.IsNullOrWhiteSpace(request.DocumentNumber) ||
            !DocumentIdPattern.IsMatch(request.DocumentNumber))
            return Refuse(DraftDecisionStatus.NeedsInput, "Belge numarası 4-40 karakter olmalı; yalnız A-Z, 0-9, _ ve - kullanılmalıdır.");
        if (!ValidText(request.PersonnelFullName, 100) ||
            (requireReview && (!ValidText(request.PersonnelTitle, 100) ||
                               !ValidText(request.Institution, 150))) ||
            (!requireReview && (!OptionalText(request.PersonnelTitle, 100) ||
                                !OptionalText(request.Institution, 150))) ||
            !ValidText(request.Origin, 150) || !ValidText(request.Destination, 150))
            return Refuse(DraftDecisionStatus.NeedsInput, "Personel, unvan, kurum, çıkış ve varış bilgileri zorunludur.");
        if (request.Begins == default || request.Ends == default || request.Ends < request.Begins ||
            request.BudgetYear != 2026 || request.Begins.Year != 2026 || request.Ends.Year != 2026)
            return Refuse(DraftDecisionStatus.NeedsReview, "Bu aşama yalnız 2026 içinde başlayıp biten yurt içi görevleri destekler.");
        if (request.TariffReference != "BUT-002:2026:I-B" || !ValidText(request.RuleVersion, 80))
            return Refuse(DraftDecisionStatus.NeedsReview, "2026 I-B tarife kaynağı/kural sürümü doğrulanmadı.");
        if (requireReview && (!request.AssignmentAndStationReviewed || !ValidText(request.AssignmentEvidence, 150) ||
            !ValidText(request.StationEvidence, 150)))
            return Refuse(DraftDecisionStatus.NeedsReview, "Görev oluru ve memuriyet mahalli kanıtı incelemesi eksik.");
        if (requireReview && !request.EntitlementAndAmountsReviewed)
            return Refuse(DraftDecisionStatus.NeedsReview, "Hak sahipliği ve satır tutarları bağımsız olarak incelenmemiş.");
        if (request.Lines is null || request.Lines.Count is < 1 or > 40)
            return Refuse(DraftDecisionStatus.NeedsInput, "Taslak 1-40 arasında gider satırı içermelidir.");
        var lineIds = new HashSet<string>(StringComparer.Ordinal);
        var allowed = request.Kind == DraftKind.TemporaryDuty ? TemporaryKinds : PermanentKinds;
        decimal gross = 0m;
        foreach (var line in request.Lines)
        {
            if (line is null || !ValidText(line.LineId, 40) || !lineIds.Add(line.LineId) ||
                !allowed.Contains(line.Kind) || !ValidText(line.Description, 150) ||
                (requireReview && !ValidText(line.EvidenceReference, 150)) ||
                !OptionalText(line.EvidenceReference, 150) || !ValidText(line.LegalReference, 100))
                return Refuse(DraftDecisionStatus.NeedsInput, "Gider satırında eksik/tekrarlı kimlik, uygunsuz tür veya belge/mevzuat referansı var.");
            if (!IsMoney(line.AmountTry) || line.AmountTry <= 0m)
                return Refuse(DraftDecisionStatus.NeedsInput, "Gider satırları pozitif, tam kuruş ve TL cinsinden olmalıdır.");
            try { gross = checked(gross + line.AmountTry); }
            catch (OverflowException) { return Refuse(DraftDecisionStatus.Inconsistent, "Satır toplamı taşmaya neden oldu."); }
            if (gross > MaximumAllowed)
                return Refuse(DraftDecisionStatus.NeedsReview, "Tutar üst denetim sınırını aştı; manuel inceleme gerekiyor.");
        }
        if (!IsMoney(request.AdvanceTry) || request.AdvanceTry < 0m || request.AdvanceTry > gross ||
            (requireReview && request.AdvanceTry > 0m && !ValidText(request.AdvanceEvidence, 150)))
            return Refuse(DraftDecisionStatus.NeedsReview, "Avans tutarı veya avans kanıtı geçersiz.");
        if (requireReview && (!IsMoney(request.IndependentGrossTry) || !IsMoney(request.IndependentNetTry) ||
            request.IndependentGrossTry != gross || request.IndependentNetTry != gross - request.AdvanceTry))
            return Refuse(DraftDecisionStatus.Inconsistent, "Bağımsız toplam/mahsup kontrolüyle tutarlar eşleşmedi.");

        // Giriş listesini kopyalayarak taslağı daha sonraki değişikliklerden koruruz.
        var rows = Array.AsReadOnly(request.Lines.ToArray());
        var document = new DraftDocument(request.Kind, request.DocumentNumber,
            request.PersonnelFullName.Trim(), request.PersonnelTitle.Trim(), request.Institution.Trim(),
            request.Origin.Trim(), request.Destination.Trim(), request.Begins, request.Ends,
            request.BudgetYear, request.TariffReference, request.RuleVersion.Trim(),
            request.AssignmentEvidence.Trim(), request.StationEvidence.Trim(),
            rows, gross, request.AdvanceTry, gross - request.AdvanceTry)
            { IsCalculationOutput = !requireReview };
        return new(DraftDecisionStatus.DraftReady,
            requireReview ? "Yalnız kontrol TASLAĞI oluşturulabilir; resmî ödeme onayı yoktur."
                : "Hesaplama çıktısı hazır; bu belge resmî onay veya hak sahipliği doğrulaması değildir.",
            document);
    }

    public static DraftBatchDecision CreateSummary(IReadOnlyList<DraftDocument> drafts)
    {
        if (drafts is null || drafts.Count is < 1 or > 500)
            return new(false, "İcmal için 1-500 taslak gereklidir.", null);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        decimal gross = 0m, advance = 0m, net = 0m;
        foreach (var doc in drafts)
        {
            if (doc is null || doc.IsOfficialPaymentDocument || doc.BudgetYear != 2026 ||
                !seen.Add(doc.DocumentNumber) || doc.GrossTry < 0 || doc.AdvanceTry < 0 ||
                doc.NetTry != doc.GrossTry - doc.AdvanceTry ||
                doc.GrossTry != doc.Lines.Sum(x => x.AmountTry))
                return new(false, "İcmalde eksik/tekrarlı ya da tutarsız belge bulundu.", null);
            gross += doc.GrossTry;
            advance += doc.AdvanceTry;
            net += doc.NetTry;
        }
        if (gross != advance + net)
            return new(false, "İcmal mahsup eşitliği sağlanamadı.", null);
        return new(true, "Yalnız denetim için TASLAK icmal.",
            new DraftBatchSummary(Array.AsReadOnly(drafts.ToArray()), gross, advance, net));
    }

    public static string Money(decimal amount) => amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL";
    private static bool IsMoney(decimal value) => value >= -MaximumAllowed && value <= MaximumAllowed &&
        decimal.Round(value, 2, MidpointRounding.AwayFromZero) == value;
    private static bool ValidText(string? text, int max) => !string.IsNullOrWhiteSpace(text) &&
        text.Trim().Length <= max && !text.Any(char.IsControl);
    private static bool OptionalText(string? text, int max) =>
        text is null || (text.Trim().Length <= max && !text.Any(char.IsControl));
    private static DraftDecision Refuse(DraftDecisionStatus status, string reason) => new(status, reason, null);
}

public sealed record DraftBatchSummary(IReadOnlyList<DraftDocument> Documents, decimal GrossTry,
    decimal AdvanceTry, decimal NetTry)
{
    public bool IsOfficialPaymentDocument => false;
    public string VisibilityNotice => "KONTROL İCMALİ - ÖDEME EMRİ/BORDRO DEĞİLDİR";
}
public sealed record DraftBatchDecision(bool Ready, string Reason, DraftBatchSummary? Summary);
