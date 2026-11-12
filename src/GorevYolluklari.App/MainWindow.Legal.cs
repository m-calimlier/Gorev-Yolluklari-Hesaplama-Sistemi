using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private const string HarcirahOfficialUrl = "https://www.mevzuat.gov.tr/MevzuatMetin/1.3.6245.pdf";
    private const string H2026OfficialUrl = "https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf";

    private sealed record LegalSection(
        string Code, string Title, string Reference, string Highlight, string Explanation, string OfficialUrl);

    // Buradaki sarı satırlar mevzuattan kısa ilgili alıntı/ibarelerdir.
    // Uzun madde metni için resmî bağlantı açılır; panel yorum için onay sayılmaz.
    private static readonly IReadOnlyDictionary<string, LegalSection> LegalSections =
        new Dictionary<string, LegalSection>(StringComparer.OrdinalIgnoreCase)
        {
            ["H2026"] = new("H2026", "2026 H Cetveli, I-B ve dipnot (1)", "2026 H Cetveli I-B",
                "Memur/hizmetli gündelikleri: ek gösterge 8000+ 890 TL; 6400+ 880 TL; 3600+ 870 TL; derece 1–4 860 TL; 5–15 850 TL.",
                "Gündelik tutarı için 2026 Bütçe Kanunu H Cetveli I-B kullanılır. Konaklamada dipnot (1): ilk 10 gün %60 artırımlı, takip eden 80 gün %50, sonraki 90 gün hak edilen gündeliğin %40'ı tavan.",
                H2026OfficialUrl),
            ["M14"] = new("M14", "Geçici görev harcırahı", "6245 m.14",
                "Geçici görev için yol masrafı ile gündelik; bazı taşıma ve bagaj masrafları ayrıca ödenebilir.",
                "Yalnız 6245 m.14 kapsamına gerçekten giren diğer masrafları yazın. Her tür giderin otomatik olarak hak edildiği sonucuna varılamaz.",
                HarcirahOfficialUrl),
            ["M27"] = new("M27", "Yurt içi yol masrafı", "6245 m.27",
                "Yurt içinde yol masrafının hesabında tarife üzerinden bilet parası veya gerçek taşıma gideri esastır.",
                "Gidiş ve dönüş ücretleri seçilen ulaşım için girilen tutarlardır. Program yol belgesinin hak sahipliğini doğrulamış sayılmaz.",
                HarcirahOfficialUrl),
            ["M33D"] = new("M33D", "Yurt içinde konaklama", "6245 m.33(d)",
                "Belgelendirilen yatacak yer bedeli; kanun ve ilgili yıl H Cetveli üst sınırlarıyla sınırlıdır.",
                "Geceler ve günlük ücret girilir; hesap motoru 2026 H Cetveli dipnot (1)'deki dönemsel üst sınırları uygular. Fatura incelemesi kurumun görevidir.",
                HarcirahOfficialUrl),
            ["M39"] = new("M39", "Memuriyet mahalli dışı gündelik", "6245 m.39",
                "Öğle veya akşam zamanından biri 1/3; ikisi 2/3; geceyi geçirmek tam gündelik.",
                "Gidiş ve dönüşte seçilen oranlar kullanıcının sağladığı hesap varsayımıdır. Saatler bu kısa formda ayrıca sorgulanmaz.",
                HarcirahOfficialUrl),
            ["M42"] = new("M42", "Geçici görevde gündeliğin azami süresi", "6245 m.42(a)",
                "Yurt içinde aynı yer ve işte bir yılda 180 gün: ilk 90 gün tam, sonraki 90 gün 2/3.",
                "Kısa ekran, daha önce aynı yer ve iş için alınmış günleri sıfır varsayar; geçmiş gün durumu ve istisnalar ayrıca değerlendirilmelidir.",
                HarcirahOfficialUrl),
            ["M43"] = new("M43", "Seyahat günlerinin hesabı", "6245 m.43",
                "Seyahat süresi her 24 saat için hesaplanır; 24 saati aşan kesir tam gün sayılır.",
                "Gidiş ve dönüş yol süreleri kullanıcı tarafından gün cinsinden girilir. Ödeme hakkı güzergâh ve saatler görülmeden teyit edilmiş sayılmaz.",
                HarcirahOfficialUrl),
            ["M45"] = new("M45", "Yer değiştirme gideri", "6245 m.45",
                "Kişi ve aile sabit unsurları ile kilometreye bağlı değişken unsur ayrı hesaplanır.",
                "Sürekli Görev için ikinci memur eş seçimi kilometre unsurunu etkiler; geçici görev girdisi değildir.",
                HarcirahOfficialUrl)
        };

    private TextBox LegalField(Panel parent, string caption, object record, string property, params string[] codes)
    {
        var box = Field(parent, caption, record, property);
        CiteLastLabel(parent, caption, codes);
        return box;
    }

    private ComboBox LegalChoice(Panel parent, string caption, System.Collections.IEnumerable choices,
        object record, string property, params string[] codes)
    {
        var box = Choice(parent, caption, choices, record, property);
        CiteLastLabel(parent, caption, codes);
        return box;
    }

    private void CiteLastLabel(Panel parent, string caption, params string[] codes)
    {
        var label = parent.Children.OfType<Label>().LastOrDefault();
        if (label is null) return;
        var inline = new TextBlock { TextWrapping = TextWrapping.Wrap };
        inline.Inlines.Add(new Run(caption));
        foreach (var key in codes)
        {
            if (!LegalSections.TryGetValue(key, out var legal)) continue;
            inline.Inlines.Add(" ");
            var link = new Hyperlink(new Run("(" + legal.Reference + ")"))
            {
                Foreground = (Brush)FindResource("HakedisGoldBrush"),
                ToolTip = "Dayanağı göster: " + legal.Title
            };
            link.Click += (_, _) => ShowLegalSection(key);
            inline.Inlines.Add(link);
        }
        label.Content = inline;
    }

    private void ShowLegalSection(string code)
    {
        if (!LegalSections.TryGetValue(code, out var legal)) return;
        var viewer = new Window
        {
            Title = "Mevzuat Dayanağı · " + legal.Reference,
            Owner = this,
            Width = 740,
            Height = 515,
            MinWidth = 480,
            MinHeight = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (Brush)FindResource("Navy"),
            Foreground = (Brush)FindResource("HakedisTextBrush"),
            FontFamily = FontFamily,
            FontSize = 14
        };
        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock
        {
            Text = legal.Title, FontSize = 23, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("HakedisGoldBrush"), Margin = new(0, 0, 0, 15)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "İLGİLİ MADDE / CETVEL ÖZETİ — SARI VURGU",
            FontWeight = FontWeights.SemiBold, Margin = new(0, 0, 0, 10)
        });
        var excerpt = new TextBlock
        {
            Text = legal.Highlight, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(250, 219, 100)),
            Foreground = new SolidColorBrush(Color.FromRgb(22, 36, 55)),
            Padding = new Thickness(13), Margin = new(0, 0, 0, 15)
        };
        stack.Children.Add(excerpt);
        stack.Children.Add(new TextBlock
        {
            Text = legal.Explanation, TextWrapping = TextWrapping.Wrap,
            Margin = new(0, 0, 0, 15)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Not: Sarı bölüm tam kanun metni değil, ilgili hükmün kısa özetidir. " +
                   "Resmî metnin tamamını aşağıdaki düğmeyle açabilirsiniz.",
            TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue,
            FontSize = 12, Margin = new(0, 0, 0, 15)
        });
        var actions = new WrapPanel();
        stack.Children.Add(actions);
        Button(actions, "Resmî kaynağı aç", () =>
            Process.Start(new ProcessStartInfo(legal.OfficialUrl) { UseShellExecute = true }));
        Button(actions, "Kapat", viewer.Close);
        viewer.Content = new ScrollViewer
        {
            Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Show();
    }
}
