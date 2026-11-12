using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Tariffs;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private void SetQuickTemporaryDefaults(DutyRecord duty)
    {
        var person = _formPerson!;
        if (string.IsNullOrWhiteSpace(person.FullName))
            person.FullName = "AD SOYAD BELİRTİLMEDİ";
        if (person.Group == PersonnelTariffGroup.Unknown)
            person.Group = PersonnelTariffGroup.GeneralCivilServant;
        // İşlem tarihini 2026 tarife seçimi için içeride tutuyoruz.
        // Bu tarihler PDF'de seyahat tarihi olarak gösterilmez.
        if (duty.Departure == default)
            duty.Departure = new DateTime(2026, 1, 1);
        if (duty.Return == default)
            duty.Return = new DateTime(2026, 1, 1);
        if (string.IsNullOrWhiteSpace(duty.Origin))
            duty.Origin = "GÜZERGÂH BELİRTİLMEDİ";
        if (string.IsNullOrWhiteSpace(duty.Destination))
            duty.Destination = "GÜZERGÂH BELİRTİLMEDİ";
    }

    private void BuildQuickTemporaryBoard(DutyRecord duty)
    {
        SetQuickTemporaryDefaults(duty);
        var person = _formPerson!;
        var (tariff, travel, result) = BuildThreeCardWorkspace(
            "2 · Gidiş ve Dönüş", "1 · Bütçe ve Tarife");

        tariff.Children.Add(Text("2026 · Yurt içi geçici görev. Genel memur/hizmetli ön hesap.", 12));
        var period = new StackPanel();
        tariff.Children.Add(period);
        period.Children.Add(new Label
        {
            Content = "Bütçe Dönemi",
            Style = (Style)FindResource("HakedisFieldLabel")
        });
        CiteLastLabel(period, "Bütçe Dönemi", "H2026");
        period.Children.Add(new Border
        {
            Style = (Style)FindResource("HakedisInfoCard"),
            Child = new TextBlock
            {
                Text = "2026 (01 Ocak – 31 Aralık)",
                Foreground = (Brush)FindResource("HakedisTextBrush"),
                FontWeight = FontWeights.SemiBold
            }
        });
        LegalChoice(tariff, "Aylık Kadro Derecesi", Enumerable.Range(1, 15),
            person, nameof(person.Grade), "H2026");
        var indicatorOptions = new[]
        {
            new Option(8000, "8000 ve üzeri"),
            new Option(6400, "6400 – 7999"),
            new Option(3600, "3600 – 6399"),
            new Option(0, "3600 altı")
        };
        var indicator = new ComboBox
        {
            ItemsSource = indicatorOptions,
            DisplayMemberPath = nameof(Option.Label),
            SelectedValuePath = nameof(Option.Value),
            Style = (Style)FindResource("HakedisFieldCombo")
        };
        AutomationProperties.SetName(indicator, "Ek Gösterge Grubu");
        if (person.Indicator is int old) indicator.SelectedValue = IndicatorGroup(old);
        indicator.SelectionChanged += (_, _) =>
        {
            if (indicator.SelectedValue is not int group || person.Indicator == group) return;
            person.Indicator = group;
            Invalidate(person, nameof(person.Indicator));
        };
        tariff.Children.Add(new Label
        {
            Content = "Ek Gösterge Grubu", Target = indicator,
            Style = (Style)FindResource("HakedisFieldLabel")
        });
        CiteLastLabel(tariff, "Ek Gösterge Grubu", "H2026");
        tariff.Children.Add(indicator);
        RegisterIssue(nameof(person.Indicator), indicator, tariff);

        travel.Children.Add(Text("Gidiş ve dönüş giderlerini ayrı girin. Gündelik oranını görev koşullarına göre seçin.", 12));
        LegalField(travel, "Gidiş Ulaşım Ücretleri Toplamı (TL)", duty,
            nameof(duty.QuickOutboundFare), "M14", "M27");
        var shares = new[]
        {
            new Option(0, "0 (yok)"),
            new Option(1, "1/3"),
            new Option(2, "2/3"),
            new Option(3, "Tam (1)")
        };
        LegalChoice(travel, "Gidiş Gündelik Oranı", shares,
            duty, nameof(duty.QuickOutboundThirds), "M39");
        LegalField(travel, "Gidiş Yol Gündelik Süresi (gün)", duty,
            nameof(duty.QuickOutboundTravelDays), "M43");
        LegalField(travel, "Dönüş Ulaşım Ücretleri Toplamı (TL)", duty,
            nameof(duty.QuickReturnFare), "M14", "M27");
        LegalChoice(travel, "Dönüş Gündelik Oranı", shares,
            duty, nameof(duty.QuickReturnThirds), "M39");
        LegalField(travel, "Dönüş Yol Gündelik Süresi (gün)", duty,
            nameof(duty.QuickReturnTravelDays), "M43");

        result.Children.Add(Text("Görev, konaklama ve diğer uygun masraflar", 14));
        LegalField(result, "Görev Süresi (gün)", duty, nameof(duty.QuickWorkDays), "M42");
        LegalField(result, "Konaklama Süresi (gece)", duty,
            nameof(duty.QuickLodgingNights), "M33D", "H2026");
        LegalField(result, "Günlük Konaklama Ücreti (TL)", duty,
            nameof(duty.QuickLodgingDailyFare), "M33D", "H2026");
        LegalField(result, "Diğer Masraflar Toplamı (TL)", duty,
            nameof(duty.QuickOtherExpenses), "M14");

        _temporaryResultHint = Text(
            _temporaryPreview is { Amount: decimal }
                ? "2026 gündelik, görev ve konaklama hesap dökümü:"
                : "Derece, ek gösterge ve görev bilgilerini doldurup Hesapla'ya basın.", 12);
        _temporaryResultHint.Style = (Style)FindResource("HakedisCardSubtitle");
        result.Children.Add(new Border
        {
            Style = (Style)FindResource("HakedisInfoCard"),
            Child = _temporaryResultHint
        });
        var calculated = new StackPanel();
        _temporaryResultContent = calculated;
        result.Children.Add(calculated);
        if (_temporaryPreview is { Amount: decimal amount } preview)
        {
            calculated.Children.Add(Metric("Geçici Görev Yolluğu", Money(amount)));
            calculated.Children.Add(Text("Tarife gündeliği: " + Money(preview.DailyRate), 12));
            foreach (var line in preview.Components ?? [])
                if (line.Amount > 0m)
                    calculated.Children.Add(Text(line.Label + " · " + Money(line.Amount), 12));
            calculated.Children.Add(Text(
                "Ön hesapta önceki aynı görev günleri 0 kabul edilir; fiilî hak sahipliği ve belgelere kurum karar verir.", 12));
        }
        else calculated.Visibility = Visibility.Collapsed;
        Button(result, "Hesapla", PreviewTemporary);
        Button(result, "Kaydet ve PDF oluştur", SaveAndMakePdf);
    }
}
