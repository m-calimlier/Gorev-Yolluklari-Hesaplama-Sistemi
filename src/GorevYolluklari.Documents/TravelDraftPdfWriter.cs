using System.IO;
using System.Globalization;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Rules;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace GorevYolluklari.Documents;

/// <summary>
/// Aşama 7: Yalnız kontrol amacıyla, Örnek 27/28 alanlarını karşılaştırmayı
/// kolaylaştıran yatay A4 PDF TASLAKLARI. Orijinal resmî formun onaylı kopyası
/// olduğu iddia edilmez; imza alanı ve resmî ödeme emri oluşturulmaz.
/// </summary>
public static class TravelDraftPdfWriter
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly XColor Navy = XColor.FromArgb(20, 37, 65);
    private static readonly XColor Gold = XColor.FromArgb(182, 133, 45);
    private static readonly XColor Light = XColor.FromArgb(238, 242, 247);
    private const double Left = 36d;
    private const double Right = 36d;
    private const double RowHeight = 22d;
    private const double TableTop = 190d;
    private const int LinesPerPage = 12;

    public static void SaveDraft(DraftDocument document, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(document);
        ValidateDocument(document);
        if (document.IsOfficialPaymentDocument || document.Lines is null || document.Lines.Count == 0 ||
            document.GrossTry != document.Lines.Sum(x => x.AmountTry) ||
            document.NetTry != document.GrossTry - document.AdvanceTry ||
            document.BudgetYear != 2026)
            throw new InvalidDataException("Tutarsız veya resmî olarak işaretlenmiş taslak PDF üretilemez.");
        ValidateOutputPath(destinationPath);
        using var pdf = new PdfDocument();
        pdf.Info.Title = $"{(document.IsCalculationOutput ? "YOLLUK HESAPLAMA ÇIKTISI" : "KONTROL TASLAĞI")} - {document.RequiredFormCode}";
        pdf.Info.Subject = "Yolluk hesabıdır; resmi onay belgesi değildir";
        pdf.Info.Creator = "Görev Yollukları Hesaplama Sistemi - Taslak Modülü";
        int pageCount = (document.Lines.Count + LinesPerPage - 1) / LinesPerPage;
        for (int pageNo = 0; pageNo < pageCount; pageNo++)
        {
            var page = pdf.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            using var g = XGraphics.FromPdfPage(page);
            RenderDocumentPage(g, page.Width.Point, page.Height.Point, document, pageNo, pageCount);
        }
        AtomicSave(pdf, destinationPath);
    }

    public static void SaveSummary(DraftBatchSummary summary, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(summary);
        if (summary.Documents is null || summary.Documents.Count is < 1 or > 500)
            throw new InvalidDataException("İcmal belge listesi geçersiz.");
        foreach (var item in summary.Documents) ValidateDocument(item);
        if (summary.GrossTry != summary.Documents.Sum(x => x.GrossTry) ||
            summary.AdvanceTry != summary.Documents.Sum(x => x.AdvanceTry) ||
            summary.NetTry != summary.Documents.Sum(x => x.NetTry))
            throw new InvalidDataException("İcmal toplamları belge toplamlarıyla uyuşmuyor.");
        if (summary.IsOfficialPaymentDocument || summary.Documents is null || summary.Documents.Count == 0 ||
            summary.GrossTry != summary.AdvanceTry + summary.NetTry ||
            summary.Documents.Select(x => x.DocumentNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count() != summary.Documents.Count)
            throw new InvalidDataException("Geçersiz taslak icmal.");
        ValidateOutputPath(destinationPath);
        using var pdf = new PdfDocument();
        pdf.Info.Title = "KONTROL TASLAĞI - Yolluk icmali";
        pdf.Info.Subject = "Ödeme emri değildir";
        pdf.Info.Creator = "Görev Yollukları Hesaplama Sistemi - Taslak Modülü";
        const int rowsPerPage = 14;
        int count = (summary.Documents.Count + rowsPerPage - 1) / rowsPerPage;
        for (int p = 0; p < count; p++)
        {
            var page = pdf.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            using var g = XGraphics.FromPdfPage(page);
            var width = page.Width.Point;
            DrawPageTop(g, width, "YURT İÇİ YOLLUK TASLAK İCMALİ", $"Sayfa {p+1}/{count}");
            var normal = Font(9);
            var bold = Font(9, true);
            var cols = new[] { Left, 120d, 330d, 420d, 536d, 642d, width - Right };
            var headers = new[] { "Belge No", "Personel", "Tür", "Brüt (TL)", "Avans (TL)", "Net (TL)" };
            DrawTableHeader(g, cols, 130, headers);
            var rows = summary.Documents.Skip(p * rowsPerPage).Take(rowsPerPage).ToArray();
            for (int i = 0; i < rows.Length; i++)
            {
                var doc = rows[i];
                var y = 154d + RowHeight * i;
                DrawRow(g, cols, y, new[] {
                    doc.DocumentNumber, doc.PersonnelFullName,
                    doc.Kind == DraftKind.TemporaryDuty ? "Geçici" : "Sürekli",
                    Currency(doc.GrossTry), Currency(doc.AdvanceTry), Currency(doc.NetTry)
                }, normal);
            }
            if (p == count - 1)
            {
                double totalY = 154d + RowHeight * rows.Length + 12d;
                DrawBar(g, width, totalY, "GENEL TOPLAM", Currency(summary.GrossTry),
                    Currency(summary.AdvanceTry), Currency(summary.NetTry), bold);
            }
            DrawFooter(g, width, page.Height.Point, "İcmal onay belgesi değildir. Çift kayıt, kalem ve mahsup kontrolü ayrıca yapılır.");
        }
        AtomicSave(pdf, destinationPath);
    }

    private static void RenderDocumentPage(XGraphics g, double width, double height,
        DraftDocument doc, int pageNo, int pageCount)
    {
        var title = doc.Kind == DraftKind.TemporaryDuty
            ? "YURT İÇİ GEÇİCİ GÖREV YOLLUĞU - Örnek 27 hesabı"
            : "YURT İÇİ SÜREKLİ GÖREV YOLLUĞU - Örnek 28 hesabı";
        if (!doc.IsCalculationOutput) title = doc.Kind == DraftKind.TemporaryDuty
            ? "YURT İÇİ GEÇİCİ GÖREV YOLLUĞU TASLAĞI - Örnek 27 alanları"
            : "YURT İÇİ SÜREKLİ GÖREV YOLLUĞU TASLAĞI - Örnek 28 alanları";
        DrawPageTop(g, width, title, $"{doc.DocumentNumber} - {pageNo + 1}/{pageCount}", doc.IsCalculationOutput);
        var normal = Font(9);
        var bold = Font(9, true);
        Text(g, "Kurum: " + doc.Institution, normal, Left, 103, 730);
        Text(g, "Personel: " + doc.PersonnelFullName + " | Unvan: " + doc.PersonnelTitle,
            normal, Left, 124, 730);
        Text(g, $"Güzergâh: {doc.Origin} - {doc.Destination}", normal, Left, 145, 470);
        Text(g, $"Tarih: {doc.Begins:dd.MM.yyyy} - {doc.Ends:dd.MM.yyyy} | Yıl: {doc.BudgetYear}", normal, Left + 482, 145, 310);
        Text(g, $"Tarife: {doc.TariffReference} | Kural: {doc.RuleVersion}", normal, Left, 166, 730);
        var cols = new[] { Left, 90d, 225d, 445d, 630d, width - Right };
        DrawTableHeader(g, cols, TableTop, new[] { "Satır", "Gider türü", "Açıklama", "Kanıt / Dayanak", "Tutar (TL)" });
        var lines = doc.Lines.Skip(pageNo * LinesPerPage).Take(LinesPerPage).ToArray();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            DrawRow(g, cols, TableTop + 24d + RowHeight * i,
                new[] { line.LineId, KindText(line.Kind), line.Description,
                    line.EvidenceReference + " / " + line.LegalReference, Currency(line.AmountTry) }, normal);
        }
        if (pageNo == pageCount - 1)
        {
            var y = TableTop + 24d + RowHeight * lines.Length + 11d;
            // 12 satırda 489 pt; alt bilgi 560 pt olduğundan taşma yoktur.
            if (y > 503d) y = 503d;
            g.DrawLine(new XPen(Gold, 1.6), Left, y, width - Right, y);
            Text(g, "Brüt: " + DraftDocumentService.Money(doc.GrossTry), bold, Left, y + 10, 245);
            Text(g, "Avans: " + DraftDocumentService.Money(doc.AdvanceTry), bold, 305, y + 10, 215);
            Text(g, "Damga öncesi: " + DraftDocumentService.Money(doc.NetTry), bold, 545, y + 10, 255);
        }
        var tax = decimal.Round(doc.GrossTry * YollukOdemeOzeti2026.DamgaVergisiOrani, 2, MidpointRounding.AwayFromZero);
        DrawFooter(g, width, height,
            doc.IsCalculationOutput
                ? "Girilen verilerden hesaplandı; hak sahipliği ve belgelerin incelemesi kurumca yapılır."
                : "Görev oluru: " + doc.AssignmentEvidence + " | Mahallî değerlendirme: " + doc.StationEvidence,
            pageNo == pageCount - 1
                ? $"2026 damga vergisi (binde 7,59): {Currency(tax)} TL | Tahmini ödenecek: {Currency(doc.NetTry - tax)} TL"
                : null, doc.IsCalculationOutput);
    }

    private static void DrawPageTop(XGraphics g, double width, string title, string detail, bool calculationOutput = false)
    {
        g.DrawRectangle(new XSolidBrush(Navy), 0, 0, width, 68);
        Text(g, "GÖREV YOLLUKLARI", Font(10, true), Left, 12, width - 2 * Left, XBrushes.White);
        Text(g, title, Font(13, true), Left, 30, width - 2 * Left, XBrushes.White);
        g.DrawRectangle(new XSolidBrush(Light), Left, 71, width - 2 * Left, 25);
        Text(g, calculationOutput ? "YOLLUK HESAPLAMA ÇIKTISI - RESMİ ONAY DEĞİLDİR"
                : "KONTROL TASLAĞI - ÖDEME BELGESİ DEĞİLDİR", Font(10, true), Left + 8, 76, 535,
            new XSolidBrush(Navy));
        Text(g, detail, Font(8), width - 280, 76, 240, new XSolidBrush(Navy));
    }

    private static void DrawTableHeader(XGraphics g, double[] cols, double y, string[] headings)
    {
        g.DrawRectangle(new XSolidBrush(Navy), cols[0], y, cols[^1] - cols[0], 24);
        for (int i = 0; i < headings.Length; i++)
            Text(g, headings[i], Font(8, true), cols[i] + 4, y + 5, cols[i+1] - cols[i] - 8, XBrushes.White);
    }
    private static void DrawRow(XGraphics g, double[] cols, double y, string[] items, XFont font)
    {
        if (((int)((y - TableTop)/RowHeight)) % 2 == 0)
            g.DrawRectangle(new XSolidBrush(Light), cols[0], y, cols[^1]-cols[0], RowHeight);
        for (int j=0;j<items.Length;j++)
        {
            Text(g, items[j], font, cols[j]+4, y+5, cols[j+1]-cols[j]-8);
            g.DrawLine(new XPen(XColors.LightGray, .5), cols[j+1], y, cols[j+1], y + RowHeight);
        }
        g.DrawLine(new XPen(XColors.LightGray, .5), cols[0], y+RowHeight, cols[^1], y+RowHeight);
    }
    private static void DrawBar(XGraphics g, double width, double y, string label,
        string gross, string advance, string net, XFont bold)
    {
        g.DrawRectangle(new XSolidBrush(Light), Left, y, width - Left - Right, 32);
        Text(g, label, bold, Left + 10, y+9, 145);
        Text(g, "Brüt: " + gross, bold, 195, y+9, 180);
        Text(g, "Avans: " + advance, bold, 400, y+9, 185);
        Text(g, "Net: " + net, bold, 612, y+9, 180);
    }
    private static void DrawFooter(XGraphics g, double width, double height, string note, string? payment = null, bool calculationOutput = false)
    {
        g.DrawLine(new XPen(Gold, 1.4), Left, height - 58, width - Right, height - 58);
        Text(g, calculationOutput ? "HESAPLAMA ÇIKTISI - RESMİ ÖDEME ONAYI DEĞİLDİR"
            : "ÖDEME BELGESİ DEĞİLDİR - İMZAYA / MUHASEBEYE SUNULAMAZ", Font(8, true), Left, height - 51, width-2*Left);
        Text(g, note, Font(7), Left, height - 35, width - 2 * Left);
        Text(g, payment ?? (calculationOutput
                ? "Hesaplama çıktısıdır; resmi Örnek 27/28 yerine geçmez."
                : "Bu taslak Örnek 27/28'in onaylı sureti değildir; mevzuat ve belge kontrolü bekleniyor."),
            Font(7), Left, height - 23, width - 2 * Left);
    }
    private static string KindText(DraftLineKind kind) => kind switch
    {
        DraftLineKind.DailyAllowance => "Gündelik", DraftLineKind.TicketOrTravel => "Yol",
        DraftLineKind.Accommodation => "Konaklama", DraftLineKind.FamilyDaily => "Aile gündeliği",
        DraftLineKind.FamilyTravel => "Aile yol", DraftLineKind.Relocation => "Yer değiştirme", _ => "Bilinmeyen"
    };
    private static XFont Font(double size, bool bold=false) =>
        new("Arial", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular, XPdfFontOptions.UnicodeDefault);
    private static string Currency(decimal x) => x.ToString("N2", Tr);

    private static void Text(XGraphics g, string value, XFont font,
        double x, double y, double w, XBrush? brush=null)
    {
        if (w < 12) return;
        var shown = value ?? "";
        if (g.MeasureString(shown, font).Width > w)
        {
            while (shown.Length > 1 && g.MeasureString(shown + "...", font).Width > w)
                shown = shown[..^1];
            shown += "...";
        }
        g.DrawString(shown, font, brush ?? XBrushes.Black, new XRect(x,y,w,16), XStringFormats.TopLeft);
    }
    private static void ValidateDocument(DraftDocument document)
    {
        static bool Money(decimal x) => x >= 0 && x <= 100_000_000m && decimal.Round(x, 2) == x;
        if (document is null || document.Kind is not (DraftKind.TemporaryDuty or DraftKind.PermanentDuty) ||
            document.BudgetYear != 2026 || document.Begins.Year != 2026 || document.Ends.Year != 2026 || document.Ends < document.Begins ||
            document.TariffReference != "BUT-002:2026:I-B" || document.Lines is null || document.Lines.Count is < 1 or > 40 ||
            !Money(document.GrossTry) || !Money(document.AdvanceTry) || !Money(document.NetTry) ||
            document.AdvanceTry > document.GrossTry || document.NetTry != document.GrossTry - document.AdvanceTry ||
            document.Lines.Any(x => x is null || !Enum.IsDefined(x.Kind) || !Money(x.AmountTry) || x.AmountTry <= 0 ||
                string.IsNullOrWhiteSpace(x.LineId) || (!document.IsCalculationOutput && string.IsNullOrWhiteSpace(x.EvidenceReference)) ||
                string.IsNullOrWhiteSpace(x.LegalReference) ||
                (document.Kind == DraftKind.TemporaryDuty && x.Kind is DraftLineKind.Relocation or DraftLineKind.FamilyDaily or DraftLineKind.FamilyTravel) ||
                (document.Kind == DraftKind.PermanentDuty && x.Kind == DraftLineKind.Accommodation)) ||
            document.Lines.Select(x => x.LineId).Distinct().Count() != document.Lines.Count ||
            document.GrossTry != document.Lines.Sum(x => x.AmountTry))
            throw new InvalidDataException("Yüklenen taslak yapısal/tutar kontrolünden geçmedi.");
    }
    private static void ValidateOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("PDF dosya yolu .pdf ile bitmelidir.", nameof(path));
        if (File.Exists(path)) throw new IOException("Mevcut PDF otomatik olarak ezilmez: " + path);
    }
    private static void AtomicSave(PdfDocument doc, string destination)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(destination))!;
        Directory.CreateDirectory(directory);
        var temp = Path.Combine(directory, "." + Guid.NewGuid().ToString("N") + ".pdf.tmp");
        try
        {
            doc.Save(temp);
            File.Move(temp, destination); // mevcut dosyayı ezmez
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
