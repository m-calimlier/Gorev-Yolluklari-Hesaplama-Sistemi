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
        var heading = Text(title, 20);
        heading.Foreground = (Brush)FindResource("Gold");
        heading.FontWeight = FontWeights.SemiBold;
        content.Children.Add(heading);
        (parent ?? Page).Children.Add(new Border { Style = (Style)FindResource("WorkspaceCard"), Child = content });
        return content;
    }
    private Border Metric(string caption, string amount)
    {
        var value = new TextBlock { Text = amount, FontSize = 24, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("Gold") };
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = Heading(caption), Foreground = Brushes.LightSteelBlue });
        content.Children.Add(value);
        return new Border { Style = (Style)FindResource("OverviewMetric"), Child = content };
    }
    private Button Button(Panel parent, string label, Action action)
    {
        var b = new Button { Content = label }; AutomationProperties.SetName(b, label); b.Click += (_, _) => Safe(action); parent.Children.Add(b); return b;
    }
    private TextBox Field(Panel parent, string label, object source, string path, string? format = null)
    {
        var box = new TextBox(); AutomationProperties.SetName(box, label);
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
        parent.Children.Add(new Label { Content = new TextBlock { Text = Heading(label) }, Target = box });
        parent.Children.Add(box);
        RegisterIssue(key, box, parent);
        _editors.Add(box);
        return box;
    }
    private void Check(Panel parent, string label, object source, string path)
    {
        var box = new CheckBox { Content = new TextBlock { Text = label } }; AutomationProperties.SetName(box, label);
        box.SetBinding(ToggleButton.IsCheckedProperty, new Binding(path) { Source = source, Mode = BindingMode.TwoWay });
        box.Click += (_, _) => { ClearIssue(path); MarkChanged(source); if (source is DutyRecord d) { d.Snapshot = null; d.ReviewedAt = null; if (path != nameof(DutyRecord.AmountsReviewed)) { d.AmountsReviewed = false; foreach (var control in _reviewChecks) control.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget(); } } };
        _reviewChecks.Add(box);
        parent.Children.Add(box);
        RegisterIssue(path, box, parent);
    }
    private ComboBox Choice(Panel parent, string label, System.Collections.IEnumerable items, object source, string path)
    {
        var combo = new ComboBox { ItemsSource = items is Option[] labels ? labels : Options(items), DisplayMemberPath = "Label", SelectedValuePath = "Value" }; AutomationProperties.SetName(combo, label);
        combo.SetBinding(Selector.SelectedValueProperty, new Binding(path) { Source = source, Mode = BindingMode.TwoWay, NotifyOnSourceUpdated = true });
        var property = source.GetType().GetProperty(path)!;
        var previous = property.GetValue(source);
        var key = source is ExpenseEntry expense ? ExpenseKey(expense, path) : path;
        combo.SourceUpdated += (_, _) =>
        {
            var current = property.GetValue(source);
            if (!Equals(previous, current)) { previous = current; Invalidate(source, key); }
        }; parent.Children.Add(new Label { Content = new TextBlock { Text = label } }); parent.Children.Add(combo); RegisterIssue(key, combo, parent); return combo;
    }
    private void Invalidate(object? source = null, string? property = null)
    {
        if (property is not null) ClearIssue(property);
        MarkChanged(source);
        if (source is PersonnelRecord && ReferenceEquals(source, _formPerson)) RefreshEntryAvailability();
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
            if (ReferenceEquals(d, _form)) UpdateExpenseSummary(d);
        }
        foreach (var check in _reviewChecks) check.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget();
    }
    private void ShowPage()
    {
        CancelDistance();
        _issueTargets.Clear();
        _expenseSummary = null;
        _expenseReviewStatus = null;
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

    private void DutyPage(DutyRecord d)
    {
        var kind = d.Kind;
        var permanent = kind == DraftKind.PermanentDuty;
        if (permanent)
        {
            ShowPermanentProgress();
            if (_permanentStep == PermanentStep.Person)
            {
                BuildPermanentPerson();
                return;
            }
            if (_permanentStep == PermanentStep.Result)
            {
                BuildPermanentResult(d);
                return;
            }
        }

        var first = EntryRow();
        if (!permanent) BuildEntryPersonnel(Card("1 · Personel", first.Left));
        var c = Card(permanent ? "Atama ve güzergâh" : "2 · Görev bilgileri", Dependent(permanent ? first.Left : first.Right));
        Field(c, "Belge numarası", d, "DocumentNumber");
        _taskStart = _editors.Last();
        c.Children.Add(Text("Programın ürettiği kayıt kodu. Görev oluru veya atama kararının numarası kanıt bölümüne yazılır.", 12));
        var pair = new UniformGrid { Columns = 2 }; c.Children.Add(pair); var left = new StackPanel(); var right = new StackPanel(); pair.Children.Add(left); pair.Children.Add(right);
        AddRoute(left, right, d);
        Expander? multiDayReview = null;
        void RefreshMultiDayReview()
        {
            if (multiDayReview is null) return;
            var multiDay = d.Departure != default && d.Return.Date > d.Departure.Date;
            multiDayReview.Visibility = multiDay ? Visibility.Visible : Visibility.Collapsed;
        }
        if (kind == DraftKind.TemporaryDuty)
        {
            DateField(left, "Ayrılış tarihi", d, nameof(d.Departure), changed: RefreshMultiDayReview);
            DateField(right, "Dönüş tarihi", d, nameof(d.Return), changed: RefreshMultiDayReview);
        }
        else
        {
            DateField(c, "Atama / işlem tarihi", d, nameof(d.Departure), withTime: false);
        }
        Choice(c, "Memuriyet mahalli değerlendirmesi", Enum.GetValues<DutyStationAssessment>(), d, "Station");
        if (kind == DraftKind.PermanentDuty)
        {
            c.Children.Add(Text("Aile ve kaynaklı mesafe", 20));
            Field(c, "Hak sahibi aile ferdi sayısı (personel hariç)", d, "FamilyCount");
            c.Children.Add(Text("Hak sahipliği doğrulanan aile fertlerini sayın. Örneğin hak sahibi eş ve iki çocuk için 3 girin.", 12));
            Field(c, "Kaynaklı kilometre", d, "Kilometers");
            Field(c, "KGM / mesafe kanıtı", d, "DistanceEvidence");
            Button(c, "KGM mesafesini sorgula", () => _ = FillDistanceAsync(d));
            Check(c, "Aile hak sahipliği incelendi", d, "FamilyReviewed"); Choice(c, "Eşin aynı güzergâhta memur olarak nakli",
                new Option[] { new(false, "Yok"), new(true, "Var · ayrı inceleme") }, d, "SpouseTransfer");
        }
        c = Card(permanent ? "Kanıt ve değerlendirme" : "3 · Kanıt ve değerlendirme", Dependent(permanent ? first.Right : first.Left));
        c.Children.Add(Text("Belge numarası, tarih veya kurumunuzdaki kayıt referansını yazın. Kutuları incelemeyi tamamladıktan sonra işaretleyin.", 12));
        Field(c, "Görev oluru / atama kararı referansı", d, "AssignmentEvidence");
        c.Children.Add(Text(kind == DraftKind.PermanentDuty ? "Atama kararının tarih ve sayısı. Örnek: 15.06.2026 / AT-001." : "Görevlendirme olurunun tarih ve sayısı.", 12));
        Field(c, "Memuriyet mahalli kanıtı", d, "StationEvidence");
        c.Children.Add(Text("Eski ve yeni görev yerlerinin değerlendirilmesine dayanak olan kurum yazısı veya kayıt referansı.", 12));
        Field(c, "İnceleyen beyanı / kayıt referansı", d, "Reviewer");
        c.Children.Add(Text("Kontrolü yapan görevlinin kurum içi inceleme referansı. Örnek: İNCELEME-001.", 12));
        Check(c, "Görev oluru incelendi", d, "AssignmentReviewed");
        Check(c, "Mahalliyet incelendi", d, "StationReviewed");
        if (kind == DraftKind.TemporaryDuty)
        {
            var travel = new StackPanel();
            var timelineCard = Card("4 · Seyahat ve hak günleri", Dependent(first.Right));
            timelineCard.Children.Add(new Expander { Header = Heading("Seyahat takvimi · m.43 aday süreleri"), Foreground = (Brush)FindResource("Gold"), Content = travel, Margin = new(0, 8, 0, 14) });
            travel.Children.Add(Text("Gidiş hareketi ve dönüş varışı üstteki ayrılış/dönüş saatleridir. Aşağıdaki saatleri gerçek belgelerden girin; boş değer varsayılmaz."));
            DateField(travel, "Gidiş varışı", d, nameof(d.OutboundArrival));
            DateField(travel, "Fiilî görev başlangıcı", d, nameof(d.WorkStarts));
            DateField(travel, "Fiilî görev bitişi", d, nameof(d.WorkEnds));
            DateField(travel, "Dönüş hareketi", d, nameof(d.InboundDeparture));
            Field(travel, "Gidiş saat kanıtı", d, "OutboundEvidence"); Field(travel, "Dönüş saat kanıtı", d, "InboundEvidence"); Field(travel, "Fiilî görev saat kanıtı", d, "WorkTimeEvidence");
            Check(travel, "Fiilî görev saatleri belgeli incelendi", d, "WorkTimesReviewed");
            Check(travel, "Saat dilimi / saat değişimi şüphesi var", d, "ClockConcern");
            var travelResult = Text("Aday süreler henüz hesaplanmadı. Hak günlerine otomatik aktarım yok.");
            Button(travel, "Seyahat adaylarını göster", () => {
                var result = Service.PreviewTimeline(_formPerson!, d);
                travelResult.Text = result.Explanation + "\nGidiş aday birimi: " + result.OutboundArticle43CandidateUnits +
                    " · Dönüş aday birimi: " + result.InboundArticle43CandidateUnits +
                    "\nAday takvim: " + string.Join(", ", result.CandidateCalendarDates.Select(x => x.ToString("dd.MM.yyyy"))) +
                    "\nÇakışan tarihler: " + string.Join(", ", result.OverlapDates.Select(x => x.ToString("dd.MM.yyyy"))) +
                    "\nBunlar onaylı hak günleri değildir. Veri değişirse yeniden hesaplayın.";
                Status.Text = "Seyahat takvimi değerlendirildi; hak günleri veya tutar satırları değiştirilmedi.";
            });
            travel.Children.Add(travelResult);
            c = new StackPanel(); var expander = new Expander { Header = Heading("Çok günlü görev · incelenmiş hak günleri"), Foreground = (Brush)FindResource("Gold"), Content = c, Margin = new(0, 8, 0, 14) }; timelineCard.Children.Add(expander);
            multiDayReview = expander;
            RefreshMultiDayReview();
            RegisterIssue("ReviewedDays", expander, timelineCard);
            c.Children.Add(Text("Takvim günleri otomatik hak günü değildir. m.43 adayları buraya otomatik aktarılmaz."));
            Field(c, "Önceki gün sayısı · bilinmiyorsa boş", d, "PriorDays"); Field(c, "Önceki gün defteri kanıtı", d, "PriorDaysEvidence");
            Check(c, "Aynı kişi/yer/iş ve yıllık dönem incelendi", d, "AnnualPeriodReviewed"); Check(c, "Geçmiş gün defteri incelendi", d, "PriorDaysReviewed"); Check(c, "Hak günleri / seyahat incelemesi tamam", d, "EligibleDaysReviewed"); Check(c, "m.42 istisnaları ayrıca değerlendirildi", d, "Article42ExceptionsExcluded"); Check(c, "33(d) konaklama hak sahipliği incelendi", d, "LodgingReviewed");
            var grid = Grid(d.ReviewedDays, true); AddCalendarColumn(grid); Col(grid, "Konaklama sırası", "LodgingOrdinal", 125); Col(grid, "Fatura TL", "LodgingAmount", 110); Col(grid, "Fatura kanıtı", "Receipt", 200); c.Children.Add(grid);
        }
        if (permanent)
        {
            BuildPermanentAssignmentActions();
            RefreshEntryAvailability();
            return;
        }
        var documents = Card("Gider ve belge", Dependent(PageFooter()));
        BuildDocumentFields(d, documents);
        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        Page.Children.Add(actions);
        Button(actions, "Vazgeç", CancelEntry);
        var save = Button(actions, "Kaydet", SaveEntry);
        _dependentEntryCards.Add(save);
        RefreshEntryAvailability();
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
        var result = Service.PrepareDraft(Person(d), d);
        return result.MayPrintDraft ? result.Document! : throw new InvalidOperationException(result.Reason);
    }
    private void MakePdf(DutyRecord duty)
    {
        var document = Prepare(duty);
        var path = OutputPath("KONTROL_TASLAGI");
        TravelDraftPdfWriter.SaveDraft(document, path);
        Status.Text = "Kontrol taslağı oluşturuldu: " + path;
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
