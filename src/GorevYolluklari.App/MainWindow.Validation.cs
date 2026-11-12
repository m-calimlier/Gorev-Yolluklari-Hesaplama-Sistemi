using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private sealed record IssueTarget(Control Control, TextBlock Message);
    private readonly Dictionary<string, IssueTarget> _issueTargets = new(StringComparer.Ordinal);

    private void RegisterIssue(string key, Control control, Panel parent)
    {
        var message = Text("", 12);
        message.Foreground = Brushes.LightCoral;
        message.Visibility = Visibility.Collapsed;
        parent.Children.Add(message);
        _issueTargets[key] = new(control, message);
    }

    private void ClearIssue(string key)
    {
        if (!_issueTargets.TryGetValue(key, out var target) || target.Message.Visibility != Visibility.Visible) return;
        target.Message.Visibility = Visibility.Collapsed;
        target.Control.ClearValue(Control.BorderBrushProperty);
        if (target.Control is CheckBox or Expander)
            target.Control.ClearValue(Control.ForegroundProperty);
    }

    private void CheckFormIssues(IEnumerable<(string Key, string Message)> items)
    {
        var issues = items.ToArray();
        foreach (var key in _issueTargets.Keys) ClearIssue(key);
        if (issues.Length == 0) return;
        Control? first = null;
        foreach (var group in issues.GroupBy(issue => issue.Key))
        {
            if (!_issueTargets.TryGetValue(group.Key, out var target)) continue;
            target.Control.BorderBrush = Brushes.LightCoral;
            if (target.Control is CheckBox or Expander) target.Control.Foreground = Brushes.LightCoral;
            target.Message.Text = string.Join(" ", group.Select(issue => issue.Message));
            target.Message.Visibility = Visibility.Visible;
            for (DependencyObject? parent = target.Control; parent is not null; parent = LogicalTreeHelper.GetParent(parent))
                if (parent is Expander expander) expander.IsExpanded = true;
            first ??= target.Control;
        }
        first?.BringIntoView();
        throw new InvalidOperationException(issues[0].Message + (issues.Length > 1 ? $" ({issues.Length} alanı kontrol edin.)" : ""));
    }

    private IEnumerable<(string, string)> PersonIssues(PersonnelRecord person)
    {
        if (string.IsNullOrWhiteSpace(person.FullName)) yield return ("FullName", "Ad soyad girin.");
        if (person.Group == PersonnelTariffGroup.Unknown) yield return ("Group", "Statü seçin.");
        if (person.Grade is null or < 1 or > 15) yield return ("Grade", "1–15 arasında derece seçin.");
        if (person.Indicator is null or < 0) yield return ("Indicator", "Ek göstergeyi girin. Ek gösterge yoksa 0 yazın.");
    }

    private IEnumerable<(string, string)> DutyIssues(DutyRecord duty, PersonnelRecord person, bool includeExpenses = true)
    {
        foreach (var issue in PersonIssues(person)) yield return issue;
        if (person.Group == PersonnelTariffGroup.SpecialOrHighOffice)
            yield return ("Group", "Bu personel statüsü ayrı tarife incelemesi gerektiriyor.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(duty.DocumentNumber ?? "", @"\A[A-Za-z0-9][A-Za-z0-9_-]{3,39}\z"))
            yield return ("DocumentNumber", "Kayıt kodu 4–40 karakter olmalı ve harf, rakam, tire veya alt çizgi içermeli.");
        if (string.IsNullOrWhiteSpace(duty.Origin)) yield return ("Origin", "Çıkış ili ve ilçesini seçin.");
        if (string.IsNullOrWhiteSpace(duty.Destination)) yield return ("Destination", "Varış ili ve ilçesini seçin.");
        if (duty.Departure.Year != 2026)
            yield return ("Departure", duty.Kind == DraftKind.PermanentDuty ? "2026 yılına ait atama / işlem tarihini seçin." :
                "2026 yılına ait ayrılış tarihini, saati ve dakikayı seçin.");
        if (duty.Kind == DraftKind.TemporaryDuty)
        {
            if (duty.Return.Year != 2026) yield return ("Return", "2026 yılına ait dönüş tarihini, saati ve dakikayı seçin.");
            else if (duty.Return <= duty.Departure) yield return ("Return", "Dönüş ayrılıştan sonra olmalı.");
            if (duty.Departure.Year == 2026 && duty.Return.Year == 2026 && duty.Departure.Date != duty.Return.Date)
            {
                if (duty.ReviewedDays.Count == 0) yield return ("ReviewedDays", "İncelenmiş hak günlerini girin.");
                if (duty.PriorDays is < 0) yield return ("PriorDays", "Önceki gün sayısı negatif olamaz.");
            }
        }
        else
        {
            if (duty.FamilyCount < 0) yield return ("FamilyCount", "Aile ferdi sayısı negatif olamaz.");
            if (duty.Kilometers < 0) yield return ("Kilometers", "Mesafe negatif olamaz.");
            if (duty.SpouseTransfer) yield return ("SpouseTransfer", "Eşin aynı güzergâhta nakli ayrı inceleme gerektiriyor.");
        }
        if (duty.Station == DutyStationAssessment.Unknown) yield return ("Station", "Memuriyet mahalli değerlendirmesini seçin.");
        if (includeExpenses)
            foreach (var issue in ExpenseIssues(duty)) yield return issue;
    }

    private static IEnumerable<(string, string)> ExpenseIssues(DutyRecord duty)
    {
        for (var index = 0; index < duty.Expenses.Count; index++)
        {
            var line = duty.Expenses[index];
            if (line.Amount <= 0 || line.Amount != decimal.Round(line.Amount, 2))
                yield return (ExpenseKey(line, nameof(line.Amount)), $"{index + 1}. gider için pozitif, iki ondalık basamaklı tutar girin.");
            if (string.IsNullOrWhiteSpace(line.Description))
                yield return (ExpenseKey(line, nameof(line.Description)), "Gider açıklamasını girin.");
            if (string.IsNullOrWhiteSpace(line.LegalBasis))
                yield return (ExpenseKey(line, nameof(line.LegalBasis)), "Giderin mevzuat referansını girin.");
        }
    }

    private static IEnumerable<(string Key, string Message)> DocumentIssues(DutyRecord duty, string? reason = null, bool includeReview = true)
    {
        if (includeReview && !duty.AmountsReviewed) yield return ("AmountsReviewed", "Belge ve tutar incelemesini tamamlayın.");
        if (duty.Expenses.Count == 0) yield return ("Expenses", "Belge için en az bir doğrulanmış gider satırı girin.");
        foreach (var issue in ExpenseIssues(duty)) yield return issue;
        if (!TryExpenseTotals(duty, out var gross, out var net))
        {
            yield return ("Expenses", "Gider toplamı hesaplanamadı. Tutarları kontrol edin.");
            yield break;
        }
        if (duty.IndependentGross is null || duty.IndependentGross != gross)
            yield return ("IndependentGross", "Bağımsız brüt tutarı girin ve gider toplamıyla karşılaştırın.");
        if (duty.IndependentNet is null || duty.IndependentNet != net)
            yield return ("IndependentNet", "Bağımsız net tutarı girin ve avans sonrası toplamla karşılaştırın.");
        if (duty.Advance < 0 || duty.Advance > gross) yield return ("Advance", "Avans sıfır ile brüt toplam arasında olmalı.");
        if (duty.Advance > 0 && string.IsNullOrWhiteSpace(duty.AdvanceEvidence))
            yield return ("AdvanceEvidence", "Avansın dayanağını girin.");
        if (reason is not null && duty.AmountsReviewed && duty.Expenses.Count > 0 &&
            duty.IndependentGross == gross && duty.IndependentNet == net)
            yield return ("AmountsReviewed", reason);
    }

    private void OpenPdfIssues(DutyRecord duty, string reason)
    {
        OpenEntry(duty, reviewPdf: true);
        if (_form?.Id != duty.Id || Navigation.SelectedIndex != 1) return;
        if (duty.Kind == DraftKind.PermanentDuty && _permanentPreview is null)
            CheckFormIssues(DutyIssues(_form, _formPerson!, includeExpenses: false));
        else
            CheckFormIssues(DocumentIssues(duty, reason));
    }
}
