using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Rules;
using Microsoft.Win32;
using Exchange = GorevYolluklari.Application.TabularExchange;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private TextBlock? _expenseSummary;
    private TextBlock? _expenseReviewStatus;

    private static string ExpenseKey(ExpenseEntry entry, string property) => $"Gider/{entry.LineId}/{property}";

    private void BuildDocumentFields(DutyRecord duty, Panel card)
    {
        var kinds = duty.Kind == DraftKind.TemporaryDuty
            ? new[] { DraftLineKind.DailyAllowance, DraftLineKind.TicketOrTravel, DraftLineKind.Accommodation }
            : new[] { DraftLineKind.DailyAllowance, DraftLineKind.TicketOrTravel, DraftLineKind.FamilyDaily,
                DraftLineKind.FamilyTravel, DraftLineKind.Relocation };
        card.Children.Add(Text("Gündelik veya yer değiştirme tutarı Kaydet ile otomatik eklenir. Yalnız ek taşıt, aile ya da konaklama gideriniz varsa aşağıdan girin.", 12));

        var actions = new WrapPanel();
        card.Children.Add(actions);
        var rows = new StackPanel();
        var empty = Text("Henüz gider eklenmedi.", 12);
        rows.Children.Add(empty);

        void Append(ExpenseEntry entry, bool open)
        {
            empty.Visibility = Visibility.Collapsed;
            ClearIssue("Expenses");
            AddExpenseEditor(duty, entry, kinds, rows, empty, open);
        }

        void Add(DraftLineKind kind)
        {
            var entry = new ExpenseEntry
            {
                LineId = "L" + Guid.NewGuid().ToString("N")[..8],
                Kind = kind,
                Description = ExpenseLabel(kind),
                LegalBasis = kind switch
                {
                    DraftLineKind.TicketOrTravel => "6245 m.27",
                    DraftLineKind.FamilyTravel => "6245 m.44",
                    DraftLineKind.Accommodation => "6245 m.33(d)",
                    DraftLineKind.DailyAllowance => duty.Kind == DraftKind.TemporaryDuty ? "6245 m.39" : "6245 m.43",
                    DraftLineKind.FamilyDaily => "6245 m.43, m.44",
                    _ => ""
                }
            };
            duty.Expenses.Add(entry);
            Invalidate(duty);
            Append(entry, true);
        }

        if (duty.Kind == DraftKind.TemporaryDuty)
        {
            Button(actions, "Taşıt gideri ekle", () => Add(DraftLineKind.TicketOrTravel));
            Button(actions, "Konaklama gideri ekle", () => Add(DraftLineKind.Accommodation));
        }
        else
        {
            Button(actions, "Personel gündeliği ekle", () => Add(DraftLineKind.DailyAllowance));
            Button(actions, "Personel taşıt gideri ekle", () => Add(DraftLineKind.TicketOrTravel));
            if (duty.FamilyCount > 0)
            {
                Button(actions, "Aile gündeliği ekle", () => Add(DraftLineKind.FamilyDaily));
                Button(actions, "Aile taşıt gideri ekle", () => Add(DraftLineKind.FamilyTravel));
            }
        }

        card.Children.Add(rows);
        foreach (var row in duty.Expenses) Append(row, false);

        _expenseSummary = Text("", 16);
        _expenseSummary.Foreground = (Brush)FindResource("Gold");
        card.Children.Add(Text("Bildirimdeki tutarlar", 20));
        card.Children.Add(_expenseSummary);
        card.Children.Add(Text("Tutarlar program tarafından hesaplanır. Bağımsız brüt/net girişi ve belge inceleme onayı gerekmez.", 12));

        var advance = new StackPanel();
        card.Children.Add(new Expander { Header = "Avans Varsa Girin", Content = advance,
            IsExpanded = duty.Advance != 0m, Margin = new(0, 8, 0, 8) });
        Field(advance, "Avans TL", duty, nameof(duty.Advance));
        Field(advance, "Avans kanıtı", duty, nameof(duty.AdvanceEvidence));

        UpdateExpenseSummary(duty);
    }

    private static string DailyLineId(DutyRecord duty) => "OTO-GD-" + duty.Id.ToString("N");

    private static decimal? DailyAmount(DutyRecord duty, CalculationSnapshot preview) =>
        duty.Departure.Date == duty.Return.Date ? preview.Amount :
        preview.Components?.FirstOrDefault(component => component.Label == "Gündelik")?.Amount;

    private static IEnumerable<(string, string)> CalculatedExpenseIssues(DutyRecord duty, CalculationSnapshot preview)
    {
        if (duty.Kind == DraftKind.PermanentDuty)
            return RelocationTransferIssues(duty, preview);
        return DailyTransferIssues(duty, preview);
    }

    private static IEnumerable<(string, string)> DailyTransferIssues(DutyRecord duty, CalculationSnapshot preview)
    {
        var transferred = duty.Expenses.FirstOrDefault(row => row.LineId == DailyLineId(duty));
        if (transferred is null) yield break;
        if (preview.Amount is null || DailyAmount(duty, preview) is not decimal raw || TransferAmount(raw) != transferred.Amount ||
            transferred.Kind != DraftLineKind.DailyAllowance || transferred.LegalBasis != preview.LegalBasis ||
            preview.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal))
            yield return (ExpenseKey(transferred, nameof(transferred.Amount)), "Gündelik satırı güncel hesapla eşleşmiyor. Gündeliği yeniden aktarın veya eski satırı kaldırın.");
        if (duty.Expenses.Count(row => row.Kind == DraftLineKind.DailyAllowance) > 1)
            yield return (ExpenseKey(transferred, nameof(transferred.Kind)), "Gündelik gideri iki kez girilmiş. Fazla satırı kaldırın.");
    }

    private void TransferDailyAllowance(DutyRecord duty, Action<ExpenseEntry> append)
    {
        if (!PersonIsReady) throw new InvalidOperationException("Önce kişi bilgilerini kaydedin.");
        var person = _formPerson!;
        CheckFormIssues(DutyIssues(duty, person, includeExpenses: false));
        var preview = Service.PreviewForCalculation(person, duty);
        if (preview.Amount is null) throw new InvalidOperationException(preview.Explanation);
        if (preview.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal))
            throw new InvalidOperationException("Simülasyon tutarı belge giderlerine aktarılmaz.");
        if (DailyAmount(duty, preview) is not decimal raw || TransferAmount(raw) <= 0m)
        {
            _dialog("Aktarılacak pozitif gündelik tutarı yok.", "Gündelik gideri", MessageBoxButton.OK);
            return;
        }
        var id = DailyLineId(duty);
        var existing = duty.Expenses.FirstOrDefault(row => row.LineId == id);
        if (duty.Expenses.Any(row => row.Kind == DraftLineKind.DailyAllowance && row.LineId != id))
        {
            _dialog("Gündelik gideri zaten girilmiş. Çifte gider oluşmaması için mevcut satırı kullanın.", "Gündelik gideri", MessageBoxButton.OK);
            return;
        }
        var amount = TransferAmount(raw);
        if (existing is not null && existing.Amount == amount && existing.Kind == DraftLineKind.DailyAllowance &&
            existing.LegalBasis == preview.LegalBasis)
        {
            Status.Text = "Gündelik gideri zaten aktarıldı.";
            return;
        }
        if (_dialog($"Giderlere {Money(amount)} gündelik aktarılacak. Tutarı ve dayanağı kontrol edin. Devam edilsin mi?",
            "Gündelik aktarımını doğrula", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        var row = existing ?? new ExpenseEntry { LineId = id, Evidence = duty.AssignmentEvidence };
        row.Kind = DraftLineKind.DailyAllowance;
        row.Amount = amount;
        row.Description = "Gündelik";
        row.LegalBasis = preview.LegalBasis;
        if (existing is null) duty.Expenses.Add(row);
        Invalidate(duty);
        if (existing is null) append(row);
        else ShowPage();
        Status.Text = $"{Money(amount)} gündelik giderlere aktarıldı.";
    }

    private static bool TryExpenseTotals(DutyRecord duty, out decimal gross, out decimal net)
    {
        try
        {
            gross = duty.Expenses.Sum(row => row.Amount);
            net = gross - duty.Advance;
            return true;
        }
        catch (OverflowException)
        {
            gross = net = 0m;
            return false;
        }
    }

    private static string ComputePaymentSummary(DutyRecord duty)
    {
        if (duty.Expenses.Count == 0) return "Gider ekleyin; brüt ve tahmini net burada görünecek.";
        try
        {
            var totals = YollukOdemeOzeti2026.Hesapla(duty.Expenses, duty.Advance);
            return $"Yolluk toplamı {Money(totals.Brut)}  ·  Damga vergisi {Money(totals.DamgaVergisi)}" +
                $"  ·  Avans {Money(totals.Avans)}  ·  Hesaplanan ödeme {Money(totals.Net)}";
        }
        catch (ArgumentException)
        {
            return "Gider ve avans tutarlarını kontrol edin. Eksik veya geçersiz tutar var.";
        }
    }

    private void UpdateExpenseSummary(DutyRecord duty)
    {
        if (_expenseSummary is not null)
            _expenseSummary.Text = ComputePaymentSummary(duty);

    }

    private void AddExpenseEditor(DutyRecord duty, ExpenseEntry entry, DraftLineKind[] kinds, StackPanel rows, TextBlock empty, bool open)
    {
        var start = _editors.Count;
        var body = new StackPanel { Margin = new(12, 5, 4, 8) };
        var details = new Expander
        {
            Header = ExpenseLabel(entry.Kind) + " · " + Money(entry.Amount),
            Content = body,
            IsExpanded = open,
            Margin = new(0, 6, 0, 6),
            Foreground = (Brush)FindResource("Gold")
        };
        AutomationProperties.SetName(details, "Gider " + entry.LineId);
        rows.Children.Add(details);

        var columns = new UniformGrid { Columns = 2 };
        body.Children.Add(columns);
        var left = new StackPanel();
        var right = new StackPanel();
        columns.Children.Add(left);
        columns.Children.Add(right);
        var calculated = entry.LineId == DailyLineId(duty) || entry.LineId == RelocationLineId(duty);
        var kind = calculated ? null : Choice(left, "Gider türü", kinds.Select(k => new Option(k, ExpenseLabel(k))).ToArray(),
            entry, nameof(entry.Kind));
        var amount = Field(right, "Gider tutarı (TL)", entry, nameof(entry.Amount));
        var supporting = new StackPanel();
        body.Children.Add(new Expander
        {
            Header = "Belge dayanağı (isteğe bağlı)",
            Content = supporting,
            IsExpanded = !string.IsNullOrWhiteSpace(entry.Evidence),
            Margin = new(0, 6, 0, 6)
        });
        Field(supporting, "Gider dayanağı", entry, nameof(entry.Evidence));
        if (calculated)
        {
            amount.IsReadOnly = true;
            RegisterIssue(ExpenseKey(entry, nameof(entry.Kind)), details, rows);
            right.Children.Add(Text("Ön hesaptan aktarıldı.", 12));
        }
        else
        {
            var advanced = new StackPanel();
            body.Children.Add(new Expander { Header = "Açıklama Ve Mevzuat", Content = advanced, Margin = new(0, 6, 0, 6) });
            Field(advanced, "Gider açıklaması", entry, nameof(entry.Description));
            Field(advanced, "Gider mevzuat referansı", entry, nameof(entry.LegalBasis));
        }

        void UpdateTitle() => details.Header = ExpenseLabel(entry.Kind) + " · " + Money(entry.Amount);
        amount.SourceUpdated += (_, _) => UpdateTitle();
        if (kind is not null) kind.SourceUpdated += (_, _) => UpdateTitle();

        var rowEditors = _editors.Skip(start).ToArray();
        var rowIssues = _issueTargets.Keys.Where(key => key.StartsWith($"Gider/{entry.LineId}/", StringComparison.Ordinal)).ToArray();
        var remove = new Button { Content = "Bu gideri kaldır", HorizontalAlignment = HorizontalAlignment.Left };
        AutomationProperties.SetName(remove, "Gideri kaldır " + entry.LineId);
        remove.Click += (_, _) =>
        {
            if (!duty.Expenses.Remove(entry)) return;
            foreach (var editor in rowEditors) _editors.Remove(editor);
            foreach (var key in rowIssues) _issueTargets.Remove(key);
            rows.Children.Remove(details);
            empty.Visibility = duty.Expenses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            Invalidate(duty);
            Status.Text = "Gider kaldırıldı.";
        };
        body.Children.Add(remove);

        if (open)
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (!rows.Children.Contains(details) || !details.IsLoaded) return;
                details.BringIntoView();
                amount.Focus();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ImportRows(DutyRecord duty, List<ExpenseEntry> entries, Action<ExpenseEntry> append)
    {
        _ = Exchange.ParseExpenses(Exchange.ToTsv(duty.Expenses.Concat(entries)));
        if (_dialog($"{entries.Count} gider eklensin mi?", "Aktarım önizlemesi", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        duty.Expenses.AddRange(entries);
        Invalidate(duty);
        foreach (var row in entries) append(row);
        Status.Text = "Giderler eklendi.";
    }

    private static string ExpenseLabel(DraftLineKind kind) => kind switch
    {
        DraftLineKind.TicketOrTravel => "Personel Taşıt Gideri",
        DraftLineKind.FamilyTravel => "Aile Taşıt Gideri",
        _ => Heading(Exchange.KindName(kind))
    };
}
