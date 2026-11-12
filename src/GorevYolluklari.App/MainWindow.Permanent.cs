using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private enum PermanentStep { Person, Assignment, Result }
    private PermanentStep _permanentStep;
    private CalculationSnapshot? _permanentPreview;
    private bool _permanentExpensesOpen;

    private void ResetPermanentStep()
    {
        _permanentStep = PermanentStep.Person;
        _permanentPreview = null;
        _permanentExpensesOpen = false;
    }

    private void ShowPermanentProgress()
    {
        var labels = new[] { "1 · Kişi", "2 · Atama Ve Güzergâh", "3 · Sonuç" };
        var steps = new WrapPanel { Margin = new(0, 0, 0, 14) };
        for (var index = 0; index < labels.Length; index++)
        {
            var active = index == (int)_permanentStep;
            steps.Children.Add(new Border
            {
                Background = (Brush)FindResource(active ? "Gold" : "Card"),
                Padding = new(12, 8, 12, 8),
                CornerRadius = new(7),
                Margin = new(0, 0, 8, 4),
                Child = new TextBlock { Text = labels[index], FontWeight = FontWeights.SemiBold,
                    Foreground = active ? (Brush)FindResource("Navy") : Brushes.LightSteelBlue }
            });
        }
        Page.Children.Add(steps);
    }

    private void BuildPermanentPerson()
    {
        var card = Card("Kişi");
        card.Children.Add(Text("Mevcut kişiyi seçin veya yeni kişinin bilgilerini girin. Kaydedince atama adımı açılır.", 12));
        BuildEntryPersonnel(card);
        Button(Page, "Vazgeç", CancelEntry).HorizontalAlignment = HorizontalAlignment.Left;
        RefreshEntryAvailability();
    }

    private void MoveToPermanentAssignment()
    {
        if (!PersonIsReady) throw new InvalidOperationException("Önce kişiyi kaydedin.");
        _permanentStep = PermanentStep.Assignment;
        _permanentPreview = null;
        ShowPage();
        Status.Text = "Atama ve güzergâh bilgilerini girin.";
    }

    private void BuildPermanentAssignmentActions()
    {
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        Page.Children.Add(actions);
        Button(actions, "Kişiye dön", () => { _permanentStep = PermanentStep.Person; ShowPage(); });
        Button(actions, "Vazgeç", CancelEntry);
        Button(actions, "Sonucu hesapla", PreviewPermanent);
    }

    private void PreviewPermanent()
    {
        if (!PersonIsReady) throw new InvalidOperationException("Önce kişiyi kaydedin.");
        var duty = Duty();
        CheckFormIssues(DutyIssues(duty, _formPerson!, includeExpenses: false));
        var preview = Service.Preview(_formPerson!, duty);
        if (preview.Amount is null)
            CheckFormIssues([("Departure", preview.Explanation)]);
        _permanentPreview = preview;
        _permanentStep = PermanentStep.Result;
        ShowPage();
        Status.Text = "Yer değiştirme ön hesabı hazır. Sonucu kaydedebilir veya giderlere geçebilirsiniz.";
    }

    private void BuildPermanentResult(DutyRecord duty)
    {
        var preview = _permanentPreview ?? throw new InvalidOperationException("Önce sonucu hesaplayın.");
        var row = EntryRow();
        var result = Card("Yer değiştirme ön hesabı", row.Left);
        result.Children.Add(Metric("Yer değiştirme", Money(preview.Amount)));
        result.Children.Add(Text("Gündelik " + Money(preview.DailyRate)));
        foreach (var component in preview.Components ?? [])
            result.Children.Add(Text($"{component.Label} · {Money(component.Amount)}"));
        if (preview.Amount is > 0)
            Button(result, "Yer değiştirmeyi giderlere aktar", () => TransferRelocation(duty, preview));

        var assignment = Card("Kişi ve güzergâh", row.Right);
        assignment.Children.Add(Text(_formPerson!.FullName));
        assignment.Children.Add(Text($"{duty.Origin} → {duty.Destination}"));
        assignment.Children.Add(Text($"{duty.Departure:dd.MM.yyyy} · {duty.Kilometers.ToString(Tr)} km · {duty.FamilyCount} aile ferdi"));
        assignment.Children.Add(Text("Taşıt gideri, avans veya PDF gerekiyorsa alttaki bölümü açın.", 12));

        var optional = new StackPanel { Margin = new(0, 10, 0, 0) };
        var panel = Card("İsteğe bağlı gider ve belge işlemleri");
        var expander = new Expander { Header = "Taşıt, Avans Ve PDF", Content = optional, IsExpanded = _permanentExpensesOpen,
            Foreground = (Brush)FindResource("Gold") };
        expander.Expanded += (_, args) =>
        {
            if (ReferenceEquals(args.OriginalSource, expander)) _permanentExpensesOpen = true;
        };
        expander.Collapsed += (_, args) =>
        {
            if (ReferenceEquals(args.OriginalSource, expander)) _permanentExpensesOpen = false;
        };
        panel.Children.Add(expander);
        BuildDocumentFields(duty, optional);

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        Page.Children.Add(actions);
        Button(actions, "Atama ve güzergâha dön", MoveToPermanentAssignment);
        Button(actions, "Vazgeç", CancelEntry);
        Button(actions, "Kaydet", SaveEntry);
    }

    private static string RelocationLineId(DutyRecord duty) => "OTO-YD-" + duty.Id.ToString("N");

    private static decimal TransferAmount(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private void TransferRelocation(DutyRecord duty, CalculationSnapshot preview)
    {
        var current = Service.Preview(_formPerson!, duty);
        if (current.Amount != preview.Amount || current.RuleVersion != preview.RuleVersion)
        {
            PreviewPermanent();
            _dialog("Ön hesap güncellendi. Aktarmadan önce yeni tutarı inceleyin.",
                "Yer değiştirme gideri", MessageBoxButton.OK);
            return;
        }
        if (preview.Amount is not decimal raw || raw <= 0m) return;
        if (preview.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal))
            throw new InvalidOperationException("Simülasyon tutarı belge giderlerine aktarılmaz.");
        var id = RelocationLineId(duty);
        var transferred = duty.Expenses.FirstOrDefault(row => row.LineId == id);
        if (duty.Expenses.Any(row => row.Kind == DraftLineKind.Relocation && !ReferenceEquals(row, transferred)))
        {
            _dialog("Bu kayıtta yer değiştirme gideri zaten var. Çifte gider oluşmaması için mevcut satırı kullanın.",
                "Yer değiştirme gideri", MessageBoxButton.OK);
            _permanentExpensesOpen = true;
            ShowPage();
            return;
        }

        var amount = TransferAmount(raw);
        if (amount <= 0m) throw new InvalidOperationException("Kuruşa çevrilen gider sıfır. Gider satırı oluşturulamadı.");
        if (transferred is not null && transferred.Amount == amount &&
            transferred.Kind == DraftLineKind.Relocation && transferred.Evidence == duty.AssignmentEvidence)
        {
            _permanentExpensesOpen = true;
            ShowPage();
            Status.Text = "Yer değiştirme gideri zaten aktarıldı.";
            return;
        }

        if ((raw != amount || transferred is not null) && _dialog(
            $"Yer değiştirme satırına {Money(amount)} yazılacak. Tutarı ve dayanağı kontrol edin. Devam edilsin mi?",
            "Aktarımı doğrula", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        if (transferred is null)
        {
            transferred = new ExpenseEntry { LineId = id };
            duty.Expenses.Add(transferred);
        }
        transferred.Kind = DraftLineKind.Relocation;
        transferred.Description = "Yer değiştirme gideri";
        transferred.Amount = amount;
        transferred.Evidence = duty.AssignmentEvidence;
        transferred.LegalBasis = "6245 m.45";
        Invalidate(duty);
        _permanentExpensesOpen = true;
        ShowPage();
        Status.Text = $"{Money(amount)} giderlere aktarıldı. Dayanağı inceleyip kaydedin.";
    }

    private static IEnumerable<(string, string)> RelocationTransferIssues(DutyRecord duty, CalculationSnapshot preview)
    {
        if (duty.Kind != DraftKind.PermanentDuty) yield break;
        var transferred = duty.Expenses.FirstOrDefault(row => row.LineId == RelocationLineId(duty));
        if (transferred is null) yield break;
        if (preview.Amount is not decimal raw || transferred.Amount != TransferAmount(raw) ||
            transferred.Kind != DraftLineKind.Relocation || preview.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal))
            yield return (ExpenseKey(transferred, nameof(transferred.Amount)), "Aktarılan tutar güncel hesapla eşleşmiyor. Yer değiştirmeyi yeniden aktarın veya eski satırı kaldırın.");
        if (duty.Expenses.Count(row => row.Kind == DraftLineKind.Relocation) > 1)
            yield return (ExpenseKey(transferred, nameof(transferred.Kind)), "Yer değiştirme gideri iki kez girilmiş. Fazla satırı kaldırın.");
    }
}
