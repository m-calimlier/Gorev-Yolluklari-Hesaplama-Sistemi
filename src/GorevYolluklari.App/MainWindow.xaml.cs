using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GorevYolluklari.App.ViewModels;
using GorevYolluklari.Application;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
using GorevYolluklari.Documents;
using GorevYolluklari.Storage;
using GorevYolluklari.Storage.Records;
using Microsoft.Win32;
using TravelService = GorevYolluklari.Application.TravelWorkflowService;
using Exchange = GorevYolluklari.Application.TabularExchange;
namespace GorevYolluklari.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private readonly JsonRecordRepository _repository = new(PortablePaths.Root);
    private TravelService Service => new(Path.Combine(PortablePaths.Sources, "02_Butce_ve_Tarifeler", "2026_H_Cetveli_Dogrulanmis_OnHesap.json"), _vm.Workspace.Constants);
    private readonly List<FrameworkElement> _editors = [];
    private readonly List<CheckBox> _reviewChecks = [];
    private bool _dirty, _loadFailed;
    private DraftKind? _selectedModule;
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private DutyRecord? _form;
    private PersonnelRecord? _formPerson;
    private bool _formChanged;
    private bool _navigating;
    private int _currentPage = 1;
    private readonly Func<string, string, MessageBoxButton, MessageBoxResult> _dialog;

    public MainWindow() : this(null, null) { }

    private readonly Action<string> _openFolder;

    internal MainWindow(Func<string, string, MessageBoxButton, MessageBoxResult>? dialog, Action<string>? openFolder, Func<KgmPlace, KgmPlace, CancellationToken, Task<int>>? fetchDistance = null)
    {
        _openFolder = openFolder ?? OpenFolder;
        _fetchDistance = fetchDistance ?? KgmDistanceLookup.FetchAsync;
        _dialog = dialog ?? ((message, title, buttons) => MessageBox.Show(this, message, title, buttons));
        InitializeComponent(); DataContext = _vm;
        try { _vm.Load(_repository.Load()); }
        catch (Exception ex) { _loadFailed = true; Status.Text = "Kayıt açılamadı. Kaydetme kapalı; Yedekleme bölümünden kurtarın. " + ex.Message; }
        Navigation.ItemsSource = new[] { "Ana Sayfa", "Veri Girişi", "Hesapla", "Sabitler", "Yedekleme / Ayarlar", "Kaynaklar" };
        // Mevcut sayfa indekslerini/kayıtları koru; günlük bildirim akışında yalnız ilgili sekmeleri göster.
        var navigationStyle = new Style(typeof(ListBoxItem), (Style)FindResource("WorkspaceNavigationItem"));
        foreach (var hiddenPage in new[] { "Ana Sayfa", "Sabitler", "Kaynaklar" })
        {
            var hidden = new DataTrigger { Binding = new Binding(), Value = hiddenPage };
            hidden.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            navigationStyle.Triggers.Add(hidden);
        }
        Navigation.ItemContainerStyle = navigationStyle;
        Navigation.SelectionChanged += (_, _) => Navigate();
        Navigation.SelectedIndex = Environment.GetCommandLineArgs().Contains("--sonuclar", StringComparer.Ordinal) ? 2 : 1;
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => Safe(SaveCurrent)));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Save, Key.S, ModifierKeys.Control));
        Closing += OnClosing;
        Closed += (_, _) => CancelDistance();
        try { var profile = JsonFileStore.Load<WindowProfile>(Path.Combine(PortablePaths.Data, "ekran.json")); if (profile is not null) ApplyProfile(profile.Width, profile.Height); }
        catch (Exception ex) { Status.Text = "Ekran ayarı okunamadı; kayıtlar korunuyor. " + ex.Message; }
    }
    private void Safe(Action action)
    {
        try { CommitEditors(); action(); }
        catch (Exception ex) { Status.Text = ex.Message; _dialog(ex.Message, "İşlem tamamlanamadı", MessageBoxButton.OK); }
    }
    private void CommitEditors()
    {
        Keyboard.ClearFocus();
        foreach (var grid in _editors.OfType<DataGrid>())
            if (!grid.CommitEdit(DataGridEditingUnit.Cell, true) || !grid.CommitEdit(DataGridEditingUnit.Row, true)) throw new InvalidOperationException("Tablodaki hatalı hücreyi düzeltin.");
        foreach (var editor in _editors.OfType<TextBox>()) { editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource(); if (Validation.GetHasError(editor)) { editor.BringIntoView(); throw new InvalidOperationException(AutomationProperties.GetName(editor) + ": " + Validation.GetErrors(editor)[0].ErrorContent); } }
    }
    private static string Heading(string value) => Tr.TextInfo.ToTitleCase(value.ToLower(Tr));
    private static TextBlock Text(string text, double size = 14) => new() { Text = size >= 20 ? Heading(text) : text, FontSize = size, Margin = new(0, 0, 0, 12) };
    private StackPanel Card(string title, Panel? parent = null)
    {
        var content = new StackPanel();
        var heading = new TextBlock { Text = Heading(title), Style = (Style)FindResource("HakedisCardTitle") };
        content.Children.Add(heading);
        (parent ?? Page).Children.Add(new Border { Style = (Style)FindResource("HakedisWorkspaceCard"), Child = content });
        return content;
    }
    private Border Metric(string caption, string amount)
    {
        var value = new TextBlock { Text = amount, FontSize = 26, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("HakedisGoldBrush"), Margin = new(0, 6, 0, 0) };
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = Heading(caption), Style = (Style)FindResource("HakedisCardSubtitle") });
        content.Children.Add(value);
        return new Border { Style = (Style)FindResource("HakedisMetricCard"), Child = content };
    }
    private Button Button(Panel parent, string label, Action action)
    {
        var important = label.Contains("PDF", StringComparison.OrdinalIgnoreCase) ||
            label is "Hesapla" or "Sonucu hesapla" or "Kaydet";
        var b = new Button { Content = label, Style = (Style)FindResource(important ?
            "HakedisPrimaryAction" : "HakedisSecondaryAction") };
        if (label == "Kaydet") b.Style = (Style)FindResource("HakedisSecondaryAction");
        AutomationProperties.SetName(b, label);
        b.Click += (_, _) => Safe(action);
        parent.Children.Add(b);
        return b;
    }
    private TextBox Field(Panel parent, string label, object source, string path, string? format = null)
    {
        var box = new TextBox { Style = (Style)FindResource("HakedisFieldText") };
        AutomationProperties.SetName(box, label);
        var binding = new Binding(path) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus, ValidatesOnExceptions = true, NotifyOnSourceUpdated = true, ConverterCulture = Tr, StringFormat = format };
        if (source.GetType().GetProperty(path)!.PropertyType == typeof(DateTime?))
        {
            binding.StringFormat = null; binding.Converter = new OptionalDateInput();
            binding.ValidationRules.Add(new OptionalDateRule());
        }
        box.SetBinding(TextBox.TextProperty, binding);
        var property = source.GetType().GetProperty(path)!;
        var previous = property.GetValue(source);
        var key = source is ExpenseEntry expense ? ExpenseKey(expense, path) : path;
        box.SourceUpdated += (_, _) => { var current = property.GetValue(source); if (!Equals(previous, current)) { previous = current; Invalidate(source, key); } };
        parent.Children.Add(new Label { Content = new TextBlock { Text = Heading(label) },
            Target = box, Style = (Style)FindResource("HakedisFieldLabel") });
        parent.Children.Add(box);
        RegisterIssue(key, box, parent);
        _editors.Add(box);
        return box;
    }
    private void Check(Panel parent, string label, object source, string path)
    {
        var box = new CheckBox { Content = new TextBlock { Text = label } }; AutomationProperties.SetName(box, label);
        box.SetBinding(ToggleButton.IsCheckedProperty, new Binding(path) { Source = source, Mode = BindingMode.TwoWay });
        box.Click += (_, _) => { ClearIssue(path); MarkChanged(source); if (source is DutyRecord d) { d.Snapshot = null; d.ReviewedAt = null; if (path != nameof(DutyRecord.AmountsReviewed)) { d.AmountsReviewed = false; foreach (var control in _reviewChecks) control.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget(); } if (path is nameof(DutyRecord.FamilyReviewed) or nameof(DutyRecord.AssignmentReviewed) or nameof(DutyRecord.StationReviewed)) InvalidatePermanentResult(); } };
        _reviewChecks.Add(box);
        parent.Children.Add(box);
        RegisterIssue(path, box, parent);
    }
    private ComboBox Choice(Panel parent, string label, System.Collections.IEnumerable items, object source, string path)
    {
        var combo = new ComboBox { ItemsSource = items is Option[] labels ? labels : Options(items),
            DisplayMemberPath = "Label", SelectedValuePath = "Value",
            Style = (Style)FindResource("HakedisFieldCombo") };
        AutomationProperties.SetName(combo, label);
        combo.SetBinding(Selector.SelectedValueProperty, new Binding(path) { Source = source, Mode = BindingMode.TwoWay, NotifyOnSourceUpdated = true });
        var property = source.GetType().GetProperty(path)!;
        var previous = property.GetValue(source);
        var key = source is ExpenseEntry expense ? ExpenseKey(expense, path) : path;
        combo.SourceUpdated += (_, _) =>
        {
            var current = property.GetValue(source);
            if (!Equals(previous, current)) { previous = current; Invalidate(source, key); }
        }; parent.Children.Add(new Label { Content = new TextBlock { Text = label },
            Style = (Style)FindResource("HakedisFieldLabel") }); parent.Children.Add(combo); RegisterIssue(key, combo, parent); return combo;
    }
    private void Invalidate(object? source = null, string? property = null)
    {
        if (property is not null) ClearIssue(property);
        MarkChanged(source);
        if (source is PersonnelRecord && ReferenceEquals(source, _formPerson)) RefreshEntryAvailability();
        if (_form?.Kind == DraftKind.TemporaryDuty && (source is PersonnelRecord || source is DutyRecord ||
            source is ReviewedDayEntry || source is ExpenseEntry))
            InvalidateTemporaryResult();
        IEnumerable<DutyRecord> affected = source switch {
            PersonnelRecord person when ReferenceEquals(person, _formPerson) && _form is not null => new[] { _form },
            PersonnelRecord person => _vm.Duties.Where(x => x.PersonnelId == person.Id),
            DutyRecord duty => new[] { duty },
            _ => _form is null ? Array.Empty<DutyRecord>() : new[] { _form }
        };
        foreach (var d in affected)
        {
            d.Snapshot = null; d.AmountsReviewed = false; d.ReviewedAt = null;
            if (property is "Origin" or "Destination" or "Departure" or "Return" or "PersonnelId")
            {
                d.AssignmentReviewed = false; d.StationReviewed = false; d.FamilyReviewed = false;
                d.EligibleDaysReviewed = false; d.AnnualPeriodReviewed = false; d.PriorDaysReviewed = false;
                d.LodgingReviewed = false; d.WorkTimesReviewed = false;
            }
            if (property is "Grade" or "Indicator" or "Group") { d.EligibleDaysReviewed = false; d.FamilyReviewed = false; }
            if (property == "AssignmentEvidence") d.AssignmentReviewed = false;
            if (property is "Station" or "StationEvidence") d.StationReviewed = false;
            if (property is "FamilyCount" or "Kilometers" or "DistanceEvidence") d.FamilyReviewed = false;
            if (property is "OutboundArrival" or "WorkStarts" or "WorkEnds" or "InboundDeparture" or "OutboundEvidence" or "InboundEvidence" or "WorkTimeEvidence") d.WorkTimesReviewed = false;
            if (property is "PriorDays" or "PriorDaysEvidence") d.PriorDaysReviewed = false;
            if (source is ReviewedDayEntry) { d.EligibleDaysReviewed = false; d.LodgingReviewed = false; }
            if (ReferenceEquals(d, _form))
            {
                UpdateExpenseSummary(d);
                if (d.Kind == DraftKind.PermanentDuty &&
                    (source is PersonnelRecord ||
                     property is "Origin" or "Destination" or "Departure" or "Station" or
                         "FamilyCount" or "Kilometers" or "DistanceEvidence" or "SpouseTransfer" or
                         "AssignmentEvidence" or "StationEvidence" or "Reviewer" or
                         "Grade" or "Indicator" or "Group"))
                    InvalidatePermanentResult();
            }
        }
        foreach (var check in _reviewChecks) check.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget();
    }
    private void ShowPage()
    {
        CancelDistance();
        _issueTargets.Clear();
        _expenseSummary = null;
        _entryBoardScroll = null;
        Page.Children.Clear(); _dependentEntryCards.Clear(); _personStatus = null; _taskStart = null; _editors.Clear(); _reviewChecks.Clear(); PageScroll.ScrollToTop();
        switch (Navigation.SelectedIndex)
        {
            case 0: Home(); break;
            case 1: EntryPage(); break;
            case 2: ResultsPage(false); break;
            case 3: ConstantsPage(); break;
            case 4: SettingsPage(); break;
            case 5: SourcesPage(); break;
        }
    }
    private void Home() => ResultsPage(true);

    private DataGrid Grid(System.Collections.IEnumerable items, bool add)
    {
        var grid = new DataGrid { ItemsSource = items, Height = 260, CanUserAddRows = add, Margin = new(0, 8, 0, 16) };
        grid.CellEditEnding += (_, e) => Invalidate(e.Row.Item); _editors.Add(grid); return grid;
    }
    private static void Col(DataGrid grid, string header, string path, double width, string? format = null) => grid.Columns.Add(new DataGridTextColumn {
        Header = Heading(header), Width = width, Binding = new Binding(path) { ValidatesOnExceptions = true, ConverterCulture = Tr, StringFormat = format } });
    private PersonnelRecord Person(DutyRecord d) => _vm.Personnel.FirstOrDefault(p => p.Id == d.PersonnelId) ?? throw new InvalidOperationException("Göreve ait personel bulunamadı.");
    private DutyRecord Duty() => _form ?? throw new InvalidOperationException("Görev formu açık değil.");

    // Ortak üç kartlı yerleşim: Geçici Görev de tek ekranda sonuç ve PDF üretir.
    private CalculationSnapshot? _temporaryPreview;
    private FrameworkElement? _temporaryResultContent;
    private TextBlock? _temporaryResultHint;

    private void DutyPage(DutyRecord d)
    {
        if (d.Kind == DraftKind.PermanentDuty)
        {
            BuildPermanentBoard(d);
            return;
        }
        var (person, task, result) = BuildThreeCardWorkspace("2 · Görev ve Güzergâh");
        person.Children.Add(Text("Personeli seçin veya yeni kişi oluşturun. Kayıtlı kişiyi tekrar yazmanız gerekmez.", 12));
        BuildEntryPersonnel(person);
        Button(person, "Vazgeç", CancelEntry).HorizontalAlignment = HorizontalAlignment.Left;

        var guarded = Dependent(task);
        var origin = new StackPanel();
        var destination = new StackPanel();
        guarded.Children.Add(origin);
        guarded.Children.Add(destination);
        AddRoute(origin, destination, d);

        FrameworkElement? multiDayArea = null;
        void RefreshMultiDay()
        {
            if (multiDayArea is not null)
                multiDayArea.Visibility = d.Departure != default && d.Return.Date > d.Departure.Date
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        DateField(guarded, "Ayrılış tarihi", d, nameof(d.Departure), changed: RefreshMultiDay);
        DateField(guarded, "Dönüş tarihi", d, nameof(d.Return), changed: RefreshMultiDay);
        Choice(guarded, "Memuriyet mahalli değerlendirmesi",
            Enum.GetValues<DutyStationAssessment>(), d, nameof(d.Station));

        var multiDay = new StackPanel();
        var dayDetails = new Expander
        {
            Header = "Çok günlü görev · gündelik ve konaklama günleri",
            IsExpanded = true,
            Content = multiDay,
            Foreground = (Brush)FindResource("Gold"),
            Margin = new(0, 12, 0, 10)
        };
        multiDayArea = dayDetails;
        guarded.Children.Add(dayDetails);
        multiDay.Children.Add(Text("Yalnız birden fazla güne yayılan görevlerde kullanılır. Tarihler arasındaki bütün günler varsayılan hak günü değildir; hesabınıza dahil edeceğiniz günleri seçin.", 12));
        Button(multiDay, "Tarih aralığındaki günleri hesaplamaya ekle", () =>
        {
            if (d.Departure == default || d.Return <= d.Departure ||
                (d.Return.Date - d.Departure.Date).TotalDays > 179)
                throw new InvalidOperationException("Önce geçerli ayrılış/dönüş tarihlerini girin (en fazla 180 gün).");
            d.ReviewedDays.Clear();
            for (var day = d.Departure.Date; day <= d.Return.Date; day = day.AddDays(1))
                d.ReviewedDays.Add(new ReviewedDayEntry { Date = day });
            Invalidate(d);
            ShowPage();
            Status.Text = "Tarih aralığındaki günler hesaplamaya eklendi. Hak edilmeyen günleri gün listesinden çıkarabilirsiniz.";
        });
        Field(multiDay, "Önceki gün sayısı · bilinmiyorsa boş", d, nameof(d.PriorDays));
        var reviewedDays = Grid(d.ReviewedDays, true);
        AddCalendarColumn(reviewedDays);
        Col(reviewedDays, "Konaklama sırası", nameof(ReviewedDayEntry.LodgingOrdinal), 125);
        Col(reviewedDays, "Fatura TL", nameof(ReviewedDayEntry.LodgingAmount), 110);
        multiDay.Children.Add(reviewedDays);
        RegisterIssue("ReviewedDays", dayDetails, guarded);
        RefreshMultiDay();
        Button(guarded, "Hesapla", PreviewTemporary);

        _temporaryResultHint = Text(_temporaryPreview?.Amount is null
            ? "Görev ve tarih bilgilerini girip Hesapla düğmesine basın."
            : "Girilen verilere göre hesaplanan yolluk:", 12);
        result.Children.Add(_temporaryResultHint);
        var calculated = new StackPanel();
        _temporaryResultContent = calculated;
        result.Children.Add(calculated);
        if (_temporaryPreview is { Amount: decimal amount } preview)
        {
            calculated.Children.Add(Metric("Görev tutarı", Money(amount)));
            calculated.Children.Add(Text("Tarife gündeliği " + Money(preview.DailyRate), 12));
            foreach (var part in preview.Components ?? [])
                calculated.Children.Add(Text($"{part.Label} · {Money(part.Amount)}", 12));
            calculated.Children.Add(Text("Kaydettiğinizde gündelik gideri otomatik aktarılır.", 12));
        }
        else calculated.Visibility = Visibility.Collapsed;

        var extras = Dependent(new StackPanel());
        var optional = new Expander
        {
            Header = "Varsa ek taşıt, konaklama ve avans",
            IsExpanded = d.Expenses.Count > 0 || d.Advance > 0m,
            Content = extras,
            Foreground = (Brush)FindResource("Gold"),
            Margin = new(0, 10, 0, 12)
        };
        result.Children.Add(optional);
        BuildDocumentFields(d, extras);
        var save = Button(result, "Kaydet", SaveEntry);
        var pdf = Button(result, "Kaydet ve PDF oluştur", SaveAndMakePdf);
        _dependentEntryCards.Add(save);
        _dependentEntryCards.Add(pdf);
        RefreshEntryAvailability();
    }

    private void PreviewTemporary()
    {
        if (!PersonIsReady) SaveEntryPerson();
        if (!PersonIsReady) throw new InvalidOperationException("Kişi bilgilerini tamamlayın.");
        var duty = Duty();
        CheckFormIssues(DutyIssues(duty, _formPerson!, includeExpenses: false));
        var preview = Service.PreviewForCalculation(_formPerson!, duty);
        if (preview.Amount is null)
            CheckFormIssues([("Departure", preview.Explanation)]);
        _temporaryPreview = preview;
        ShowPage();
        FocusResultCard();
        Status.Text = "Hesap hazır. Üçüncü karttan Kaydet ve PDF oluştur düğmesini kullanabilirsiniz.";
    }

    private void InvalidateTemporaryResult()
    {
        if (_form?.Kind != DraftKind.TemporaryDuty || _temporaryPreview is null) return;
        _temporaryPreview = null;
        if (_temporaryResultContent is not null)
            _temporaryResultContent.Visibility = Visibility.Collapsed;
        if (_temporaryResultHint is not null)
            _temporaryResultHint.Text = "Girdi değişti. Güncel tutar için tekrar Hesapla düğmesine basın.";
    }

    private StackPanel PageFooter()
    {
        var panel = new StackPanel();
        Page.Children.Add(panel);
        return panel;
    }

    private void ExportXlsx()
    {
        var dialog = new SaveFileDialog { Filter = "Excel değer dosyası (*.xlsx)|*.xlsx", FileName = "YOLLUK_GIDER_" + Guid.NewGuid().ToString("N")[..8] + ".xlsx", InitialDirectory = PortablePaths.Outputs };
        if (dialog.ShowDialog(this) == true) { Exchange.WriteXlsx(dialog.FileName, Duty().Expenses); Status.Text = "XLSX kaydedildi: " + dialog.FileName; }
    }
    private string OutputPath(string prefix) => Path.Combine(PortablePaths.Outputs, prefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..8] + ".pdf");
    private DraftDocument Prepare(DutyRecord d)
    {
        var result = Service.PrepareCalculationPdf(Person(d), d);
        return result.MayPrintDraft ? result.Document! : throw new InvalidOperationException(result.Reason);
    }
    private void MakePdf(DutyRecord duty)
    {
        var document = Prepare(duty);
        var path = OutputPath("YOLLUK_HESAPLAMA");
        TravelDraftPdfWriter.SaveDraft(document, path);
        Status.Text = "Yolluk hesaplama PDF'si oluşturuldu: " + path;
        _dialog(Path.GetFileName(path), "PDF oluşturuldu", MessageBoxButton.OK);
        _openFolder(Path.GetDirectoryName(path)!);
    }

    private void Save()
    {
        if (_loadFailed) throw new InvalidOperationException("Açılışta kayıt okunamadı; önce yedekten kurtarın. Bozuk kayıt ezilmeyecek.");
        _vm.Synchronize(); _repository.Save(_vm.Workspace, _vm.Workspace.Revision); _dirty = false;
        Status.Text = $"Kayıtlar kaydedildi · revizyon {_vm.Workspace.Revision}. Veri: {_repository.FilePath}";
    }
    private void SettingsPage()
    {
        Page.Children.Add(Text("Yedekleme ve ayarlar", 27)); var c = Card("Taşınabilir JSON kayıtları");
        c.Children.Add(Text(PortablePaths.Root)); c.Children.Add(Text("Her kayıt güncellemesinde tarihli yedek alınır. Geri yükleme öncesi mevcut dosya korunur. JSON yedekleri şifreli değildir; güvenli bir konumda saklayın."));
        Button(c, "Yedek oluştur", () => { Save(); Status.Text = "Yedek oluşturuldu: " + _repository.CreateBackup(); });
        Button(c, "Yedekten geri yükle", () => {
            var dialog = new OpenFileDialog { Filter = "JSON yedek (*.json)|*.json", InitialDirectory = PortablePaths.Backups };
            if (dialog.ShowDialog(this) != true) return;
            if (MessageBox.Show(this, "Mevcut disk kaydı koruyucu yedeğe alınacak. Kaydedilmemiş ekran değişiklikleri geri yüklenen kayıtla değiştirilecek. Devam edilsin mi?", "Yedek geri yükleme", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            _repository.Restore(dialog.FileName); ClearEntry(); _vm.Load(_repository.Load()); _dirty = false; _loadFailed = false; Status.Text = "Yedek geri yüklendi; önceki dosya da korundu."; ShowPage();
        });
        Button(c, "Veriler klasörü", () => OpenFolder(PortablePaths.Data)); Button(c, "Yedekler klasörü", () => OpenFolder(PortablePaths.Backups));
        var profiles = new ComboBox { ItemsSource = new[] { "1024 × 768", "1366 × 768", "1920 × 1080" }, SelectedIndex = 1 }; c.Children.Add(profiles);
        Button(c, "Pencere profilini uygula", () => { var sizes = new[] { (1024d, 768d), (1366d, 768d), (1920d, 1080d) }; var (w, h) = sizes[profiles.SelectedIndex]; ApplyProfile(w, h); JsonFileStore.Save(Path.Combine(PortablePaths.Data, "ekran.json"), new WindowProfile(w, h)); });
    }
    private void ApplyProfile(double width, double height)
    {
        if (width >= 1920 && height >= 1080)
        {
            WindowState = WindowState.Maximized;
            return;
        }

        WindowState = WindowState.Normal;
        var area = SystemParameters.WorkArea;
        Width = Math.Clamp(width, MinWidth, Math.Max(MinWidth, area.Width));
        Height = Math.Clamp(height, MinHeight, Math.Max(MinHeight, area.Height));
        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
    }
    private static void OpenFolder(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        try { CommitEditors(); } catch { e.Cancel = true; Status.Text = "Hatalı alanı düzeltmeden pencere kapatılamaz."; return; }
        if ((_formChanged || _personChanged) && _dialog("Açık görev formundaki değişiklikler kaydedilmedi. Kapatılsın mı?", "Görev formu", MessageBoxButton.YesNo) != MessageBoxResult.Yes) { e.Cancel = true; return; }
        if (!_dirty) return;
        var answer = MessageBox.Show(this, "Kaydedilmemiş değişiklikler var. Kaydedilsin mi?", "Kayıt kontrolü", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel) e.Cancel = true;
        else if (answer == MessageBoxResult.Yes) { try { Save(); } catch (Exception ex) { e.Cancel = true; Status.Text = ex.Message; } }
    }
    private static Option[] Options(System.Collections.IEnumerable values) => values.Cast<object>().Select(value => new Option(value, value switch {
        DutyStationAssessment.Unknown => "Belirsiz — inceleme gerekli", DutyStationAssessment.Inside => "Memuriyet mahalli içi", DutyStationAssessment.Outside => "Memuriyet mahalli dışı",
        PersonnelTariffGroup.Unknown => "Belirsiz statü", PersonnelTariffGroup.GeneralCivilServant => "Genel memur / hizmetli", PersonnelTariffGroup.SpecialOrHighOffice => "Özel statü / üst görev",
        DraftLineKind kind => Exchange.KindName(kind), _ => value.ToString() ?? "" })).ToArray();
    public sealed record Option(object Value, string Label);
    public sealed record WindowProfile(double Width, double Height);
}
