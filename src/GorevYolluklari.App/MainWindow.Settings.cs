using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
using GorevYolluklari.Storage;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private void ConstantsPage()
    {
        Page.Children.Add(Text("Sabitler", 28));
        Page.Children.Add(Text("Düzenlenen değerler simülasyon hesabında kullanılır. PDF için kaynak değerleriyle yeniden hesaplayın."));
        var current = _vm.Workspace.Constants;
        var t = current.Temporary;
        var p = current.Permanent;
        var c = current.Common;
        var defaults = new CalculationConstants();

        ConstantsCard("Geçici", [
            ("Öğle saati", t.LunchHour), ("Akşam saati", t.DinnerHour), ("Günübirlik gündelik paydası", t.MealDivisor),
            ("Tam gündelik gün sınırı", t.FullDays), ("Toplam gün sınırı", t.MaxDays),
            ("İndirimli gündelik payı", t.ReducedNumerator), ("İndirimli gündelik paydası", t.ReducedDivisor),
            ("İlk konaklama dönemi gün sınırı", t.FirstLodgingDays), ("İlk dönem konaklama tavanı (%)", t.FirstLodgingPercent),
            ("İkinci dönem konaklama tavanı (%)", t.MiddleLodgingPercent), ("Son dönem konaklama tavanı (%)", t.LastLodgingPercent)
        ], values => UpdateConstants(x => x with { Temporary = new TemporaryConstants(
            Whole(values[0]), Whole(values[1]), Whole(values[2]), Whole(values[3]), Whole(values[4]),
            Whole(values[5]), Whole(values[6]), Whole(values[7]), values[8], values[9], values[10]) }),
           () => UpdateConstants(x => x with { Temporary = defaults.Temporary }));

        ConstantsCard("Sürekli", [
            ("Kişi gündelik katsayısı", p.SelfDays), ("Aile ferdi gündelik katsayısı", p.FamilyDays),
            ("Sabit unsurda aile ferdi üst sınırı", p.MaximumFamily), ("Kilometre başına gündelik oranı (%)", p.KilometerPercent)
        ], values => UpdateConstants(x => x with { Permanent = new PermanentConstants(values[0], values[1], Whole(values[2]), values[3]) }),
           () => UpdateConstants(x => x with { Permanent = defaults.Permanent }));

        var common = ConstantsCard("Ortak", [
            ("I-B a gündelik (TL)", c.RateA), ("I-B b gündelik (TL)", c.RateB), ("I-B c gündelik (TL)", c.RateC),
            ("I-B d gündelik (TL)", c.RateD), ("I-B e gündelik (TL)", c.RateE),
            ("I-B a ek gösterge alt sınırı", c.IndicatorA), ("I-B b ek gösterge alt sınırı", c.IndicatorB),
            ("I-B c ek gösterge alt sınırı", c.IndicatorC), ("I-B d en büyük derece", c.UpperGrade)
        ], values => UpdateConstants(x => x with { Common = new CommonConstants(values[0], values[1], values[2],
            values[3], values[4], Whole(values[5]), Whole(values[6]), Whole(values[7]), Whole(values[8])) }),
           () => UpdateConstants(x => x with { Common = defaults.Common }));
        common.Children.Insert(1, Text("Tarife dönemi 2026 · Genel memur / hizmetli · Derece 1–15"));
    }

    private StackPanel ConstantsCard(string title, (string Label, decimal Value)[] fields,
        Action<decimal[]> save, Action reset)
    {
        var card = Card(title);
        var editors = new List<TextBox>();
        var layout = new UniformGrid { Columns = 2 };
        card.Children.Add(layout);
        foreach (var field in fields)
        {
            var box = new TextBox { Text = field.Value.ToString(Tr), IsReadOnly = true,
                Style = (Style)FindResource("HakedisFieldText") };
            AutomationProperties.SetName(box, field.Label);
            var group = new StackPanel();
            group.Children.Add(new Label { Content = new TextBlock { Text = Heading(field.Label) }, Target = box,
                Style = (Style)FindResource("HakedisFieldLabel") });
            group.Children.Add(box);
            layout.Children.Add(group);
            editors.Add(box);
        }

        var actions = new WrapPanel();
        card.Children.Add(actions);
        var edit = new Button { Content = "Düzenle",
            Style = (Style)FindResource("HakedisPrimaryAction") };
        AutomationProperties.SetName(edit, title + " sabitlerini düzenle");
        var cancel = new Button { Content = "Vazgeç", Visibility = Visibility.Collapsed,
            Style = (Style)FindResource("HakedisSecondaryAction") };
        var resetButton = new Button { Content = "Kaynak değerlerini yükle", Visibility = Visibility.Collapsed,
            Style = (Style)FindResource("HakedisSecondaryAction") };
        actions.Children.Add(edit);
        actions.Children.Add(cancel);
        actions.Children.Add(resetButton);
        void SetEditing(bool editing)
        {
            foreach (var box in editors) box.IsReadOnly = !editing;
            edit.Content = editing ? "Kaydet" : "Düzenle";
            AutomationProperties.SetName(edit, title + (editing ? " sabitlerini kaydet" : " sabitlerini düzenle"));
            cancel.Visibility = resetButton.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            if (editing) editors[0].Focus();
        }

        edit.Click += (_, _) => Safe(() =>
        {
            if (editors[0].IsReadOnly)
            {
                SetEditing(true);
                return;
            }
            var values = editors.Select(box =>
                decimal.TryParse(box.Text, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Tr, out var value)
                    ? value : throw new ArgumentException(AutomationProperties.GetName(box) + " için geçerli bir sayı girin.")).ToArray();
            save(values);
            for (var i = 0; i < fields.Length; i++) fields[i] = (fields[i].Label, values[i]);
            SetEditing(false);
        });
        cancel.Click += (_, _) =>
        {
            for (var i = 0; i < fields.Length; i++) editors[i].Text = fields[i].Value.ToString(Tr);
            SetEditing(false);
        };
        resetButton.Click += (_, _) => Safe(() => { reset(); ShowPage(); });
        return card;
    }

    private static int Whole(decimal value) => value == decimal.Truncate(value) && value is >= 0 and <= int.MaxValue
        ? (int)value : throw new ArgumentException("Saat, gün, derece ve pay/payda için tam sayı girin.");

    private void UpdateConstants(Func<CalculationConstants, CalculationConstants> update)
    {
        var next = CurrentWorkspace();
        next.Constants = update(next.Constants);
        SaveWorkspace(next, _vm.SelectedDuty?.Id);
        Status.Text = "Sabitler kaydedildi. Değişen değerlerle ilgili görevleri yeniden hesaplayın.";
    }

    private void SourcesPage()
    {
        Page.Children.Add(Text("Hesap Dayanakları", 28));
        var card = Card("Yalnız kullanılan temel mevzuat");
        card.Children.Add(Text(
            "Geçici/Sürekli Görev hesapları için gerekli madde ve 2026 tarifesi. " +
            "İlgili olmayan eski yardımcı görüşler bu ekrandan kaldırıldı; mevcut arşiv dosyaları silinmedi.", 12));
        AddSource(card, "6245 sayılı Harcırah Kanunu", "01_Mevzuat/6245_Harcirah_Kanunu.pdf",
            HarcirahOfficialUrl);
        AddSource(card, "2026 H Cetveli I-B", "02_Butce_ve_Tarifeler/2026_H_Cetveli.pdf",
            H2026OfficialUrl);
        foreach (var id in new[] { "M14", "M27", "M33D", "M39", "M42", "M43", "M44", "M45", "H2026" })
        {
            var code = id;
            var source = LegalSections[id];
            Button(card, source.Title + " (" + source.Reference + ")", () => ShowLegalSection(code));
        }
    }

    private void AddSource(Panel parent, string title, string relativePath, string? officialUrl = null)
    {
        var path = Path.Combine(PortablePaths.Sources, relativePath);
        var row = new DockPanel { Margin = new(0, 0, 0, 4) };
        parent.Children.Add(row);
        var available = File.Exists(path);
        var open = Button(row, "PDF", () =>
        {
            var target = File.Exists(path) ? path : officialUrl ?? throw new FileNotFoundException(title + " bulunamadı.");
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        });
        open.IsEnabled = available || officialUrl is not null;
        AutomationProperties.SetName(open, title + " PDF");
        DockPanel.SetDock(open, Dock.Right);
        row.Children.Add(new TextBlock { Text = Heading(title) + (available ? "" : officialUrl is null ? " · dosya eksik" : " · çevrimiçi"),
            VerticalAlignment = VerticalAlignment.Center });
    }
}
