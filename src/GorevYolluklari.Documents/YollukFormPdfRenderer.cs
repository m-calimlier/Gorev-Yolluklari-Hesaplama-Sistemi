using System.Globalization;
using System.IO;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Rules;
using PdfSharp.Drawing;

namespace GorevYolluklari.Documents;

/// <summary>
/// M.Y.H.B.Y. Örnek 27/28 tablo düzeninden uyarlanmış A4 yatay HESAPLAMA çıktısı.
/// Onaylı matbu form, görev oluru veya ödeme emri DEĞİLDİR.
/// Ayrı hesaplama motoru kullanılmaz: yalnız doğrulanmış DraftDocument satırları
/// ve TravelService önizlemesinin DraftPrintDetails dökümü basılır.
/// </summary>
internal static class YollukFormPdfRenderer
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly XColor Ink = XColor.FromArgb(30, 43, 60);
    private static readonly XColor Faint = XColor.FromArgb(236, 240, 244);
    private static readonly XColor Grey = XColor.FromArgb(246, 247, 249);
    private static readonly XColor Stroke = XColor.FromArgb(97, 109, 125);
    private static readonly XColor Subtle = XColor.FromArgb(74, 88, 109);
    private const double L = 29d;
    private const double R = 29d;
    private const double Start = 157d;
    private const double HeaderOne = 21d;
    private const double HeaderTwo = 28d;
    private const double HeightRow = 25d;
    private const int VisibleRows = 6;

    internal static bool Supports(DraftDocument d) =>
        d.IsCalculationOutput && d.PrintDetails is not null && d.Lines.Count <= 8 &&
        (d.Kind == DraftKind.PermanentDuty || d.IsQuickTemporaryCalculation);

    internal static void Render(XGraphics g, double pageWidth, double pageHeight, DraftDocument doc)
    {
        if (!Supports(doc)) throw new InvalidDataException("Bu çıktı cetvelinin baskı bileşenleri bulunamadı.");
        var details = doc.PrintDetails!;
        DrawHeading(g, pageWidth, doc, details);
        if (doc.Kind == DraftKind.PermanentDuty)
            RenderPermanent(g, pageWidth, doc, details);
        else
            RenderTemporary(g, pageWidth, doc, details);
        DrawEnding(g, pageWidth, pageHeight, doc);
    }

    private static void DrawHeading(XGraphics g, double w, DraftDocument doc, DraftPrintDetails d)
    {
        Rect(g, 0, 0, w, 27, Grey);
        Txt(g, "GÖREV YOLLUKLARI HESAPLAMA CETVELİ", 10, true, L, 9, 430, 14, Ink);
        Txt(g, "2026  •  HESAPLAMA ÇIKTISI  •  RESMÎ ONAY DEĞİLDİR",
            8.0, true, w - 330, 9, 300, 13, Subtle, right: true);
        Center(g, doc.Kind == DraftKind.PermanentDuty
                ? "YURTİÇİ SÜREKLİ GÖREV YOLLUĞU BİLDİRİMİ"
                : "YURTİÇİ GEÇİCİ GÖREV YOLLUĞU BİLDİRİMİ",
            16, true, L, 55, w - L - R, 23, Ink);
        Center(g, doc.Kind == DraftKind.PermanentDuty
                ? "M.Y.H.B.Y. Örnek No: 28 düzeninden uyarlanmış hesaplama çıktısı"
                : "M.Y.H.B.Y. Örnek No: 27 düzeninden uyarlanmış hesaplama çıktısı",
            8.4, false, L, 79, w - L - R, 13, Subtle);

        var x = L;
        const double boxWidth = 304d;
        const double labelWidth = 121d;
        var values = new (string Label, string Value)[]
        {
            ("Adı Soyadı", Show(doc.PersonnelFullName)),
            ("Unvanı", Show(doc.PersonnelTitle)),
            ("Aylık Kadro Derecesi", d.Grade?.ToString(Tr) ?? "Girilmedi"),
            ("Ek Göstergesi", d.Indicator.HasValue
                ? d.Indicator.Value == 0 ? "3600 altı" : d.Indicator.Value.ToString(Tr)
                : "Girilmedi"),
            ("Gündeliği", d.DailyRateTry.HasValue ? Money(d.DailyRateTry.Value) + " TL" : "Girilmedi")
        };
        for (int i = 0; i < values.Length; i++)
        {
            var y = 93d + 12d*i;
            Rect(g, x, y, boxWidth, 12, XColors.White, Stroke);
            Line(g, x + labelWidth, y, x + labelWidth, y + 12);
            Txt(g, values[i].Label, 7.4, false, x+5, y+2, labelWidth - 8, 10);
            Txt(g, values[i].Value, 7.6, true, x+labelWidth+5, y+1.5, boxWidth-labelWidth-8, 10);
        }
        const double departmentWidth = 149d;
        var right = w-R-departmentWidth;
        Rect(g, right, 93, departmentWidth, 36, XColors.White, Stroke);
        Center(g, "KURUM / DAİRE", 8.2, true, right+3, 99, departmentWidth-6, 14);
        Center(g, Show(doc.Institution), 7.4, false, right+3, 114, departmentWidth-6, 12);
        Rect(g, right, 129, departmentWidth, 24, XColors.White, Stroke);
        Txt(g, "BÜTÇE YILI", 7.7, true, right+7, 136, 94, 12);
        Txt(g, doc.BudgetYear.ToString(Tr), 9, true, right+106, 134, 36, 14, Ink, right: true);
    }

    private static void RenderTemporary(XGraphics g, double w, DraftDocument doc, DraftPrintDetails d)
    {
        var c = new[] { L, 89d, 197d, 247d, 293d, 334d, 397d, 461d, 520d, 579d, 664d, 733d, w-R };
        var groupLabels = new[]
        {
            ("İŞLEM", 0, 1), ("GÜZERGÂH", 1, 2), ("HAREKET", 2, 4),
            ("GÜNDELİKLER", 4, 7), ("TAŞIT / ZORUNLU", 7, 9),
            ("KONAKLAMA / DİĞER", 9, 11), ("TOPLAM", 11, 12)
        };
        var second = new[]
        {
            "AŞAMA", "NEREDEN / NEREYE", "GİDİŞ", "DÖNÜŞ", "GÜN", "YEVMİYE",
            "TUTAR", "CİNSİ", "TUTAR", "GÜN / GİDER", "TUTAR", "TL"
        };
        DrawHeaders(g, c, groupLabels, second);
        decimal P(string label) => Round(d.Components.FirstOrDefault(x => x.Label == label)?.AmountTry ?? 0m);
        var outboundDaily = P("Gidiş yol gündeliği");
        var dutyDaily = P("Görev gündeliği");
        var returnDaily = P("Dönüş yol gündeliği");
        var lodging = P("Konaklama");
        var other = P("Diğer masraflar");
        var outboundFare = Sum(doc, DraftLineKind.TicketOrTravel, "OTO-GQ-");
        var returnFare = Sum(doc, DraftLineKind.TicketOrTravel, "OTO-RQ-");
        var fareTotal = Sum(doc, DraftLineKind.TicketOrTravel);
        if (fareTotal != outboundFare + returnFare)
            throw new InvalidDataException("Gidiş/dönüş yol gideri baskı dökümüne uymuyor.");
        if (outboundDaily + dutyDaily + returnDaily != Sum(doc, DraftLineKind.DailyAllowance) ||
            lodging != Sum(doc, DraftLineKind.Accommodation) ||
            other != Sum(doc, DraftLineKind.OtherEligible) ||
            outboundDaily + dutyDaily + returnDaily + fareTotal + lodging + other != doc.GrossTry)
            throw new InvalidDataException("Geçici görev baskı kalemleri kaydedilen brüt tutara uymuyor.");

        var rate = d.DailyRateTry;
        var noRoute = doc.Origin.Contains("BELİRTİLMEDİ", StringComparison.OrdinalIgnoreCase) ||
                      doc.Destination.Contains("BELİRTİLMEDİ", StringComparison.OrdinalIgnoreCase);
        var route = noRoute ? "Girilmedi" : doc.Origin + " - " + doc.Destination;
        string Number(decimal n) => n > 0 ? Amount(n) : "-";
        var items = new[]
        {
            new[] { "Gidiş", route, "-", "-", d.OutboundTravelDays+"x"+Fraction(d.OutboundDailyThirds),
                rate.HasValue ? Amount(rate.Value) : "-", Number(outboundDaily),
                outboundFare>0?"Yol":"-", Number(outboundFare), "-", "-",
                Number(outboundFare+outboundDaily) },
            new[] { "Görev", "Yer girilmedi", "-", "-", d.WorkDays.ToString(Tr),
                rate.HasValue ? Amount(rate.Value) : "-", Number(dutyDaily),
                "-", "-", "-", "-", Number(dutyDaily) },
            new[] { "Dönüş", route, "-", "-", d.ReturnTravelDays+"x"+Fraction(d.ReturnDailyThirds),
                rate.HasValue ? Amount(rate.Value) : "-", Number(returnDaily),
                returnFare>0?"Yol":"-", Number(returnFare), "-", "-",
                Number(returnFare+returnDaily) },
            new[] { "Konaklama", "-", "-", "-", "-", "-", "-",
                "-", "-", d.LodgingNights+" gece", Number(lodging), Number(lodging) },
            new[] { "Diğer gider", "-", "-", "-", "-", "-", "-",
                "-", "-", "6245 m.14", Number(other), Number(other) }
        };
        for (int i = 0; i < items.Length; i++) DrawCells(g, c, TableRowY(i), items[i], HeightRow, i%2==1);
        for (int i = items.Length; i < VisibleRows; i++)
            DrawCells(g, c, TableRowY(i), Enumerable.Repeat("",12).ToArray(), HeightRow, i%2==1);
        DrawTotals(g, c, new[]
        {
            "GENEL TOPLAM (TL)", "", "", "", "", "", Amount(outboundDaily+dutyDaily+returnDaily),
            "", Amount(fareTotal), "", Amount(lodging+other), Amount(doc.GrossTry)
        }, 6);
        Txt(g, "Gidiş + görev + dönüş + konaklama için hesap dökümü", 9, true, L, 419, 540, 14);
        Txt(g, "Kısa hesapta güzergâh ve hareket saatleri istenmediği için belgeye eklenmedi.",
            8.0, false, L, 437, 540, 14);
        Txt(g, "2026 H Cetveli konaklama sınırları uygulanmıştır; önceki görev günleri sıfır varsayılır.",
            7.8, false, L, 454, 540, 14);
        Payment(g, w, doc);
    }

    private static void RenderPermanent(XGraphics g, double w, DraftDocument doc, DraftPrintDetails d)
    {
        var c = new[] { L, 128d, 228d, 279d, 318d, 379d, 442d, 507d, 574d, 648d, 683d, 752d, w-R };
        var groups = new[]
        {
            ("NEREDEN / NEREYE", 0, 1), ("ADI SOYADI", 1, 2), ("AKRABALIK", 2, 3),
            ("GÜNDELİKLER", 3, 6), ("TAŞIT GİDERİ", 6, 8),
            ("YER DEĞİŞTİRME GİDERİ", 8, 11), ("TOPLAM", 11, 12)
        };
        var second = new[]
        {
            "GÜZERGÂH", "PERSONEL", "DERECESİ", "GÜN", "YEVMİYE", "TUTAR",
            "ÇEŞİDİ", "TUTAR", "SABİT", "KM", "DEĞİŞKEN", "TL"
        };
        DrawHeaders(g, c, groups, second);
        decimal P(string label) => Round(d.Components.FirstOrDefault(x => x.Label == label)?.AmountTry ?? 0m);
        var relocation = Sum(doc, DraftLineKind.Relocation);
        var selfFixed = P("Kişi sabit unsuru");
        var familyFixed = P("Aile sabit unsuru");
        // Motorun yuvarlanmış yer değiştirme satırı tek doğruluk kaynağıdır.
        var variable = relocation-selfFixed-familyFixed;
        if (variable < 0m || Math.Abs(variable-P("Mesafeye bağlı unsur")) > 0.02m)
            throw new InvalidDataException("Sürekli görev değişken unsuru kaydedilen tutarla tutarsız.");
        var personDaily = Sum(doc, DraftLineKind.DailyAllowance);
        var familyDaily = Sum(doc, DraftLineKind.FamilyDaily);
        var personFare = Sum(doc, DraftLineKind.TicketOrTravel);
        var familyFare = Sum(doc, DraftLineKind.FamilyTravel);
        if (personDaily+familyDaily+personFare+familyFare+relocation != doc.GrossTry)
            throw new InvalidDataException("Sürekli görev baskı kalemleri brüt tutara uymuyor.");
        var rate = d.DailyRateTry.HasValue ? Amount(d.DailyRateTry.Value) : "-";
        var selfTotal = personDaily+personFare+selfFixed+variable;
        var familyTotal = familyDaily+familyFare+familyFixed;
        var first = new[]
        {
            Show(doc.Origin), Show(doc.PersonnelFullName), "Kendisi", personDaily>0?"1":"-",
            rate, N(personDaily), personFare>0?"Otobüs/Yol":"-", N(personFare),
            N(selfFixed), d.Kilometers>0?d.Kilometers.ToString("N0",Tr):"-",
            N(variable), N(selfTotal)
        };
        DrawCells(g,c,TableRowY(0),first,HeightRow,false);
        int count = 1;
        if (d.FamilyCount > 0)
        {
            var family = new[]
            {
                Show(doc.Destination), "AİLE FERDİ", "Aile ("+d.FamilyCount+")",
                familyDaily>0?"1":"-", rate, N(familyDaily),
                familyFare>0?"Otobüs/Yol":"-", N(familyFare), N(familyFixed),"-","-",
                N(familyTotal)
            };
            DrawCells(g,c,TableRowY(1),family,HeightRow,true);
            count=2;
        }
        for(int i=count;i<VisibleRows;i++)
            DrawCells(g,c,TableRowY(i),Enumerable.Repeat("",12).ToArray(),HeightRow,i%2==1);
        DrawTotals(g,c,new[]
        {
            "GENEL TOPLAM (TL)","","","","",Amount(personDaily+familyDaily),
            "",Amount(personFare+familyFare),Amount(selfFixed+familyFixed),"",
            Amount(variable),Amount(doc.GrossTry)
        },6);
        Txt(g, "Güzergâh: " + Show(doc.Origin) + "  >  " + Show(doc.Destination),
            9.1, true, L, 419, 525, 14);
        Txt(g, "Mesafe: " + d.Kilometers.ToString("N0",Tr) + " km   •   Aile ferdi: " +
            d.FamilyCount + (d.SecondCivilServantSpouse ? "   •   İkinci memur eş: %50 mesafe unsuru" : ""),
            8.2, false, L, 437, 525, 14);
        Txt(g, "Ön hesap: seyahat gündeliği 1 gün, varsa taşıt bedeli kişi başına eşit varsayılmıştır.",
            7.8, false, L, 454, 525, 14);
        Payment(g,w,doc);
    }

    private static void Payment(XGraphics g, double w, DraftDocument doc)
    {
        var x=w-R-165;
        var gross=doc.GrossTry;
        var tax=Round(gross*YollukOdemeOzeti2026.DamgaVergisiOrani);
        var net=doc.NetTry-tax;
        var rows=doc.AdvanceTry>0 ? 4 : 3;
        var y=408d;
        var names=doc.AdvanceTry>0
            ? new[] {"BRÜT TOPLAM","DAMGA VERGİSİ","AVANS","NET ÖDENECEK"}
            : new[] {"BRÜT TOPLAM","DAMGA VERGİSİ","NET ÖDENECEK"};
        var values=doc.AdvanceTry>0
            ? new[] {gross,tax,doc.AdvanceTry,net}
            : new[] {gross,tax,net};
        for(int i=0;i<rows;i++)
        {
            Rect(g,x,y+23*i,165,23,i==rows-1?Faint:XColors.White,Stroke);
            Txt(g,names[i],7.2,true,x+6,y+23*i+7,87,12);
            Txt(g,Money(values[i])+" TL",8.1,true,x+91,y+23*i+6,68,12,Ink,right:true);
        }
    }

    private static void DrawEnding(XGraphics g, double w, double h, DraftDocument doc)
    {
        Line(g,L,513,w/2-55,513);
        Line(g,w/2+42,513,w-R,513);
        Center(g,"BİLDİRİM SAHİBİ (boş örnek alan)",7.9,false,L,519,w/2-L-55,14,Subtle);
        Center(g,"GÖREVLENDİRME / ONAY (boş örnek alan)",7.9,false,w/2+42,519,w/2-R-42,14,Subtle);
        Txt(g,"İsim, unvan ve imza alanları program tarafından doldurulmaz.",7.3,false,L,541,600,13,Subtle);
        Line(g,L,h-34,w-R,h-34);
        Txt(g,"ÖRNEK DÜZEN • YALNIZ HESAPLAMA ÇIKTISI • RESMÎ ÖDEME / ONAY BELGESİ DEĞİLDİR",
            7.7,true,L,h-28,w-2*L-135,12,Subtle);
        Txt(g,"Örnek "+(doc.Kind==DraftKind.PermanentDuty?"28":"27")+" uyarlaması • 1/1",
            7.2,false,w-165,h-28,136,12,Subtle,right:true);
        Txt(g,"Created by Murat ÇALIMLIER",7.3,false,L,h-13,300,12,Subtle);
    }

    private static void DrawHeaders(XGraphics g,double[] c,
        (string Name,int Begin,int End)[] groups,string[] subheadings)
    {
        foreach(var group in groups)
        {
            Rect(g,c[group.Begin],Start,c[group.End]-c[group.Begin],HeaderOne,Faint,Stroke);
            Center(g,group.Name,7.0,true,c[group.Begin]+2,Start+2,
                c[group.End]-c[group.Begin]-4,HeaderOne-4);
        }
        for(int i=0;i<subheadings.Length;i++)
        {
            Rect(g,c[i],Start+HeaderOne,c[i+1]-c[i],HeaderTwo,Faint,Stroke);
            Center(g,subheadings[i],6.3,true,c[i]+1,Start+HeaderOne+2,c[i+1]-c[i]-2,HeaderTwo-4);
        }
    }

    private static double TableRowY(int row) => Start+HeaderOne+HeaderTwo+row*HeightRow;
    private static void DrawCells(XGraphics g,double[] c,double y,string[] values,double h,bool alternate)
    {
        if(values.Length!=c.Length-1)throw new InvalidDataException("PDF tablo sütun sayısı hatalı.");
        for(int i=0;i<values.Length;i++)
        {
            Rect(g,c[i],y,c[i+1]-c[i],h,alternate?Grey:XColors.White,Stroke);
            var amount=(i==values.Length-1)||(values.Length==12&&i is 6 or 8 or 10);
            Txt(g,values[i],i==0?7.0:6.9,false,c[i]+3,y+7,c[i+1]-c[i]-6,14,Ink, right:amount);
        }
    }
    private static void DrawTotals(XGraphics g,double[] c,string[] totals,int rowIndex)
    {
        var y=TableRowY(rowIndex);
        for(int i=0;i<totals.Length;i++)
            Rect(g,c[i],y,c[i+1]-c[i],28,Faint,Stroke);
        // Örnek 27/28 tablosu gibi sol başlık ilk sütunlara yayılsın.
        Txt(g,"G E N E L  T O P L A M  (TL)",7.6,true,
            c[0]+7,y+9,c[5]-c[0]-15,14);
        for(int i=5;i<totals.Length;i++)
            if(!string.IsNullOrWhiteSpace(totals[i]))
                Txt(g,totals[i],7.4,true,c[i]+2,y+8,c[i+1]-c[i]-4,14,Ink,right:true);
    }
    private static void Rect(XGraphics g,double x,double y,double w,double h,XColor fill,XColor? border=null)
    {
        g.DrawRectangle(new XSolidBrush(fill),x,y,w,h);
        if(border.HasValue)
            g.DrawRectangle(new XPen(border.Value,0.55),x,y,w,h);
    }
    private static void Line(XGraphics g,double x,double y,double x2,double y2)
        => g.DrawLine(new XPen(Stroke,0.7),x,y,x2,y2);
    private static XFont Font(double size,bool bold) =>
        new("Arial",size,bold?XFontStyleEx.Bold:XFontStyleEx.Regular,XPdfFontOptions.UnicodeDefault);
    private static void Center(XGraphics g,string value,double size,bool bold,
        double x,double y,double w,double h,XColor? color=null)
    {
        if(w<=2)return;
        var f=Font(size,bold);
        var shown=Fit(g,value,f,w-4);
        g.DrawString(shown,f,new XSolidBrush(color??Ink),new XRect(x,y,w,h),XStringFormats.Center);
    }
    private static void Txt(XGraphics g,string value,double size,bool bold,
        double x,double y,double w,double h,XColor? color=null,bool right=false)
    {
        if(w<=5 || string.IsNullOrEmpty(value))return;
        var f=Font(size,bold);
        var text=Fit(g,value,f,w-1);
        if(string.IsNullOrEmpty(text))return;
        var ink=new XSolidBrush(color??Ink);
        var m=g.MeasureString(text,f).Width;
        var ox=right?x+Math.Max(0,w-m):x;
        g.DrawString(text,f,ink,new XRect(ox,y,Math.Max(m+1,1),h),XStringFormats.TopLeft);
    }
    private static string Fit(XGraphics g,string? raw,XFont f,double width)
    {
        var value=(raw??"").Replace('\r',' ').Replace('\n',' ');
        if(g.MeasureString(value,f).Width<=width)return value;
        while(value.Length>1&&g.MeasureString(value+"...",f).Width>width)
            value=value[..^1];
        return value+"...";
    }
    private static string Show(string? value) =>
        string.IsNullOrWhiteSpace(value)?"Girilmedi":value.Trim();
    private static decimal Sum(DraftDocument d,DraftLineKind kind,string? prefix=null) =>
        d.Lines.Where(x=>x.Kind==kind&&(prefix is null||x.LineId.StartsWith(prefix,StringComparison.Ordinal)))
            .Sum(x=>x.AmountTry);
    private static decimal Round(decimal amount) =>
        decimal.Round(amount,2,MidpointRounding.AwayFromZero);
    private static string Amount(decimal amount) => amount.ToString("N2",Tr);
    private static string Money(decimal amount) => amount.ToString("N2",Tr);
    private static string N(decimal amount) => amount==0m?"-":Amount(amount);
    private static string Fraction(int thirds) => thirds switch {0=>"0",1=>"1/3",2=>"2/3",3=>"1",_=>"?"};
}
