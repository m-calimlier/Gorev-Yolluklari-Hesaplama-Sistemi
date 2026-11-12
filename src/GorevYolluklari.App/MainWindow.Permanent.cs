using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Rules;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private CalculationSnapshot? _permanentPreview;
    private bool _permanentExpensesOpen;
    private FrameworkElement? _permanentCalculatedArea;
    private TextBlock? _permanentResultHint;
    private ScrollViewer? _entryBoardScroll;

    private void ResetPermanentStep()
    {
        _permanentPreview = null;
        _permanentExpensesOpen = false;
        _permanentCalculatedArea = null;
        _permanentResultHint = null;
    }

    // Tek bir üç kartlı düzen, iki görev türünde de kullanılır.
    // Dış sayfa yalnız dikey; kartların kaydırması yalnız yataydır.
    private (StackPanel Person, StackPanel Task, StackPanel Result) BuildThreeCardWorkspace(string taskTitle)
    {
        var board = new Grid { MinWidth = 1140 };
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.08, GridUnitType.Star) });
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.08, GridUnitType.Star) });

        var holders = Enumerable.Range(0, 3).Select(_ => new Grid()).ToArray();
        for (var i = 0; i < holders.Length; i++)
        {
            System.Windows.Controls.Grid.SetColumn(holders[i], i * 2);
            board.Children.Add(holders[i]);
        }

        var boardScroll = new ScrollViewer
        {
            Content = board,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        // StackPanel kendi içeriğini sonsuz genişlikte ölçtüğünden scrollbar daha önce
        // gerçek bir viewport alamıyordu. Dış viewport genişliğine sabitle.
        boardScroll.SetBinding(FrameworkElement.WidthProperty,
            new Binding(nameof(ScrollViewer.ViewportWidth)) { Source = PageScroll });
        boardScroll.SizeChanged += (_, _) =>
            board.MinWidth = Math.Max(1140, boardScroll.ViewportWidth);

        boardScroll.PreviewMouseWheel += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                boardScroll.ScrollToHorizontalOffset(boardScroll.HorizontalOffset - e.Delta);
            else
                PageScroll.ScrollToVerticalOffset(PageScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        };
        // Dar ekranlarda ilk/orta/son kart arasında ekranın altında kaydırma
        // çubuğunu aramadan geçiş. Geniş ekranda gereksiz yere görünmez.
        var quickNav = new WrapPanel { Margin = new(0, 0, 0, 7), Visibility = Visibility.Collapsed };
        var steps = new[] { "1 · Kişi", "2 · Görev", "3 · Sonuç" };
        for (var i = 0; i < steps.Length; i++)
        {
            var step = i;
            var jump = new Button
            {
                Content = steps[i],
                Style = (Style)FindResource("HakedisSecondaryAction")
            };
            System.Windows.Automation.AutomationProperties.SetName(jump, "Git: " + steps[i]);
            jump.Click += (_, _) =>
            {
                boardScroll.UpdateLayout();
                var offset = step switch
                {
                    0 => 0d,
                    1 => Math.Min(boardScroll.ScrollableWidth, board.ActualWidth / 3d),
                    _ => boardScroll.ScrollableWidth
                };
                boardScroll.ScrollToHorizontalOffset(offset);
            };
            quickNav.Children.Add(jump);
        }
        void UpdateQuickNav() =>
            quickNav.Visibility = boardScroll.ScrollableWidth > 8d ? Visibility.Visible : Visibility.Collapsed;
        boardScroll.ScrollChanged += (_, _) => UpdateQuickNav();
        boardScroll.Loaded += (_, _) => UpdateQuickNav();
        Page.Children.Add(quickNav);
        Page.Children.Add(boardScroll);
        _entryBoardScroll = boardScroll;
        return (
            Card("1 · Kişi", holders[0]),
            Card(taskTitle, holders[1]),
            Card("3 · Sonuç", holders[2]));
    }

    private void FocusResultCard()
    {
        if (_entryBoardScroll is null) return;
        _entryBoardScroll.UpdateLayout();
        _entryBoardScroll.ScrollToRightEnd();
    }

    private void BuildPermanentBoard(DutyRecord duty)
    {
        var (person, assignment, result) = BuildThreeCardWorkspace("2 · Atama ve Güzergâh");
        person.Children.Add(Text("Personeli seçin veya bilgilerini girip kaydedin.", 12));
        BuildEntryPersonnel(person);
        Button(person, "Vazgeç", CancelEntry).HorizontalAlignment = HorizontalAlignment.Left;

        var guarded = Dependent(assignment);
        guarded.Children.Add(Text("Kayıt kodu otomatik oluşturulur.", 12));
        var origin = new StackPanel();
        var destination = new StackPanel();
        guarded.Children.Add(origin);
        guarded.Children.Add(destination);
        AddRoute(origin, destination, duty);
        DateField(guarded, "Atama / işlem tarihi", duty, nameof(duty.Departure), withTime: false);
        Choice(guarded, "Memuriyet mahalli değerlendirmesi",
            Enum.GetValues<GorevYolluklari.Core.Rules.DutyStationAssessment>(), duty, nameof(duty.Station));

        guarded.Children.Add(Text("Aile ve mesafe", 16));
        Field(guarded, "Hak sahibi aile ferdi sayısı (personel hariç)", duty, nameof(duty.FamilyCount));
        Field(guarded, "Kaynaklı kilometre", duty, nameof(duty.Kilometers));
        Button(guarded, "KGM mesafesini sorgula", () => _ = FillDistanceAsync(duty));
        Choice(guarded, "Eşin aynı güzergâhta memur olarak nakli",
            new Option[] { new(false, "Yok"), new(true, "Var · ayrı inceleme") }, duty, nameof(duty.SpouseTransfer));

        Button(guarded, "Sonucu hesapla", PreviewPermanent);

        _permanentResultHint = Text("Bilgileri girip Sonucu hesapla veya Kaydet ve PDF oluştur düğmesine basın.", 12);
        _permanentResultHint.Style = (Style)FindResource("HakedisCardSubtitle");
        _permanentResultHint.Margin = new(0);
        var resultInformation = new Border
        {
            Style = (Style)FindResource("HakedisInfoCard"),
            Child = _permanentResultHint
        };
        resultInformation.SetBinding(UIElement.VisibilityProperty,
            new Binding(nameof(UIElement.Visibility)) { Source = _permanentResultHint });
        result.Children.Add(resultInformation);
        _permanentCalculatedArea = new StackPanel();
        result.Children.Add(_permanentCalculatedArea);
        BuildPermanentResult(duty, (StackPanel)_permanentCalculatedArea);
        var optional = new StackPanel { Margin = new(0, 10, 0, 0) };
        var expander = new Expander
        {
            Header = "Varsa ek taşıt, aile gideri ve avans",
            Content = optional,
            IsExpanded = _permanentExpensesOpen,
            Foreground = (Brush)FindResource("Gold")
        };
        expander.Expanded += (_, args) =>
        {
            if (ReferenceEquals(args.OriginalSource, expander)) _permanentExpensesOpen = true;
        };
        expander.Collapsed += (_, args) =>
        {
            if (ReferenceEquals(args.OriginalSource, expander)) _permanentExpensesOpen = false;
        };
        result.Children.Add(expander);
        BuildDocumentFields(duty, optional);
        Button(result, "Kaydet", SaveEntry);
        Button(result, "Kaydet ve PDF oluştur", SaveAndMakePdf);
        RefreshEntryAvailability();
    }

    private void MoveToPermanentAssignment()
    {
        if (!PersonIsReady) throw new InvalidOperationException("Önce kişiyi kaydedin.");
        _permanentPreview = null;
        ShowPage();
        Status.Text = "Kişi kaydedildi. Ortadaki Atama ve Güzergâh kartından devam edin.";
        _taskStart?.Focus();
    }

    // Sonuç yeniden hesaplanmadan eski rakamların ekranda kalmasını engeller.
    private void InvalidatePermanentResult()
    {
        if (_form?.Kind != DraftKind.PermanentDuty || _permanentPreview is null) return;
        _permanentPreview = null;
        if (_permanentCalculatedArea is not null)
            _permanentCalculatedArea.Visibility = Visibility.Collapsed;
        if (_permanentResultHint is not null)
        {
            _permanentResultHint.Text = "Bilgiler değişti. Güncel tutarlar için yeniden Sonucu hesapla düğmesine basın.";
            _permanentResultHint.Visibility = Visibility.Visible;
        }
    }

    private void PreviewPermanent()
    {
        if (!PersonIsReady) SaveEntryPerson();
        if (!PersonIsReady) throw new InvalidOperationException("Kişi bilgilerini tamamlayın.");
        var duty = Duty();
        CheckFormIssues(DutyIssues(duty, _formPerson!, includeExpenses: false));
        var preview = Service.PreviewForCalculation(_formPerson!, duty);
        if (preview.Amount is null)
            CheckFormIssues([("Departure", preview.Explanation)]);
        _permanentPreview = preview;
        ShowPage();
        FocusResultCard();
        Status.Text = "Yer değiştirme hesabı hazır. Sağdaki Sonuç kartından PDF oluşturabilirsiniz.";
    }

    private void BuildPermanentResult(DutyRecord duty, StackPanel result)
    {
        if (_permanentPreview is not { } preview)
        {
            _permanentCalculatedArea!.Visibility = Visibility.Collapsed;
            return;
        }
        _permanentResultHint!.Visibility = Visibility.Collapsed;
        result.Children.Add(Metric("Yer değiştirme", Money(preview.Amount)));
        result.Children.Add(Text("Gündelik " + Money(preview.DailyRate)));
        foreach (var component in preview.Components ?? [])
            result.Children.Add(Text($"{component.Label} · {Money(component.Amount)}"));
        result.Children.Add(Text("Kaydettiğinizde yer değiştirme tutarı otomatik giderlere eklenir; ayrıca aktarım yapmanız gerekmez.", 12));


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
