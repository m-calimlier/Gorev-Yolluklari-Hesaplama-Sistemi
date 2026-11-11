using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GorevYolluklari.App.ViewModels;
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
    private readonly TravelService _service = new(Path.Combine(PortablePaths.Sources, "02_Butce_ve_Tarifeler", "2026_H_Cetveli_Dogrulanmis_OnHesap.json"));
    private readonly List<FrameworkElement> _editors = [];
    private readonly List<CheckBox> _reviewChecks = [];
    private bool _dirty, _loadFailed;
    private DraftKind? _selectedModule;
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    public MainWindow()
    {
        InitializeComponent(); DataContext = _vm;
        try { _vm.Load(_repository.Load()); }
        catch (Exception ex) { _loadFailed = true; Status.Text = "Kayıt açılamadı. Kaydetme kapalı; Yedekleme bölümünden kurtarın. " + ex.Message; }
        Navigation.ItemsSource = new[] { "Ana Sayfa", "Geçici Görev", "Sürekli Görev", "Personel ve Kayıtlar", "Belgeler / İcmaller", "Kaynak ve Tarifeler", "Yedekleme / Ayarlar" };
        Navigation.SelectionChanged += (_, _) => ShowPage(); Navigation.SelectedIndex = 0;
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => Safe(Save)));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Save, Key.S, ModifierKeys.Control));
        Closing += OnClosing;
        try { var profile = JsonFileStore.Load<WindowProfile>(Path.Combine(PortablePaths.Data, "ekran.json")); if (profile is not null) ApplyProfile(profile.Width, profile.Height); }
        catch (Exception ex) { Status.Text = "Ekran ayarı okunamadı; kayıtlar korunuyor. " + ex.Message; }
    }
    private void Safe(Action action)
    {
        try { CommitEditors(); action(); }
        catch (Exception ex) { Status.Text = ex.Message; MessageBox.Show(this, ex.Message, "İşlem tamamlanamadı", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void CommitEditors()
    {
        Keyboard.ClearFocus();
        foreach (var grid in _editors.OfType<DataGrid>())
            if (!grid.CommitEdit(DataGridEditingUnit.Cell, true) || !grid.CommitEdit(DataGridEditingUnit.Row, true)) throw new InvalidOperationException("Tablodaki hatalı hücreyi düzeltin.");
        foreach (var editor in _editors.OfType<TextBox>()) { editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource(); if (Validation.GetHasError(editor)) throw new InvalidOperationException(AutomationProperties.GetName(editor) + ": " + Validation.GetErrors(editor)[0].ErrorContent); }
    }
    private static TextBlock Text(string text, double size = 14) => new() { Text = text, FontSize = size, Margin = new(0, 0, 0, 12) };
    private StackPanel Card(string title)
    {
        var content = new StackPanel(); content.Children.Add(Text(title, 21));
        Page.Children.Add(new Border { Background = (Brush)FindResource("Card"), CornerRadius = new(12), Padding = new(18), Margin = new(0, 0, 0, 14), Child = content }); return content;
    }
    private Button Button(Panel parent, string label, Action action)
    {
        var b = new Button { Content = label }; AutomationProperties.SetName(b, label); b.Click += (_, _) => Safe(action); parent.Children.Add(b); return b;
    }
    private void Field(Panel parent, string label, object source, string path, string? format = null)
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
        box.SourceUpdated += (_, _) => { var current = property.GetValue(source); if (!Equals(previous, current)) { previous = current; Invalidate(source, path); } };
        parent.Children.Add(new Label { Content = label, Target = box }); parent.Children.Add(box); _editors.Add(box);
    }
    private void Check(Panel parent, string label, object source, string path)
    {
        var box = new CheckBox { Content = label }; AutomationProperties.SetName(box, label);
        box.SetBinding(ToggleButton.IsCheckedProperty, new Binding(path) { Source = source, Mode = BindingMode.TwoWay });
        box.Click += (_, _) => { _dirty = true; if (source is DutyRecord d) { d.Snapshot = null; d.ReviewedAt = null; if (path != nameof(DutyRecord.AmountsReviewed)) { d.AmountsReviewed = false; foreach (var control in _reviewChecks) control.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget(); } } };
        _reviewChecks.Add(box);
        parent.Children.Add(box);
    }
    private ComboBox Choice(Panel parent, string label, System.Collections.IEnumerable items, object source, string path)
    {
        var combo = new ComboBox { ItemsSource = Options(items), DisplayMemberPath = "Label", SelectedValuePath = "Value" }; AutomationProperties.SetName(combo, label);
        combo.SetBinding(Selector.SelectedValueProperty, new Binding(path) { Source = source, Mode = BindingMode.TwoWay });
        combo.SelectionChanged += (_, _) => Invalidate(source, path); parent.Children.Add(new Label { Content = label }); parent.Children.Add(combo); return combo;
    }
    private void Invalidate(object? source = null, string? property = null)
    {
        _dirty = true;
        IEnumerable<DutyRecord> affected = source switch {
            PersonnelRecord person => _vm.Duties.Where(x => x.PersonnelId == person.Id),
            DutyRecord duty => new[] { duty },
            _ => _vm.SelectedDuty is null ? Array.Empty<DutyRecord>() : new[] { _vm.SelectedDuty }
        };
        foreach (var d in affected)
        {
            d.Snapshot = null; d.AmountsReviewed = false; d.ReviewedAt = null;
            if (property is "Origin" or "Destination" or "Departure" or "Return" or "PersonnelId" or "Reviewer")
            {
                d.AssignmentReviewed = false; d.StationReviewed = false; d.FamilyReviewed = false;
                d.EligibleDaysReviewed = false; d.AnnualPeriodReviewed = false; d.PriorDaysReviewed = false;
                d.LodgingReviewed = false; d.WorkTimesReviewed = false;
            }
            if (property == "AssignmentEvidence") d.AssignmentReviewed = false;
            if (property is "Station" or "StationEvidence") d.StationReviewed = false;
            if (property is "FamilyCount" or "Kilometers" or "DistanceEvidence") d.FamilyReviewed = false;
            if (property is "OutboundArrival" or "WorkStarts" or "WorkEnds" or "InboundDeparture" or "OutboundEvidence" or "InboundEvidence" or "WorkTimeEvidence") d.WorkTimesReviewed = false;
            if (property is "PriorDays" or "PriorDaysEvidence") d.PriorDaysReviewed = false;
            if (source is ReviewedDayEntry) { d.EligibleDaysReviewed = false; d.LodgingReviewed = false; }
        }
        foreach (var check in _reviewChecks) check.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateTarget();
    }
    private void ShowPage()
    {
        Page.Children.Clear(); _editors.Clear(); _reviewChecks.Clear(); PageScroll.ScrollToTop();
        switch (Navigation.SelectedIndex)
        {
            case 0: Home(); break;
            case 1: _selectedModule = DraftKind.TemporaryDuty; TemporaryDutyPage(); break;
            case 2: _selectedModule = DraftKind.PermanentDuty; PermanentDutyPage(); break;
            case 3: PersonnelPage(); break;
            case 4: DocumentsPage(); break;
            case 5: SourcesPage(); break;
            case 6: SettingsPage(); break;
        }
    }
    private void Home()
    {
        Page.Children.Add(Text("Görev ve kayıt çalışma alanı", 27));
        var c = Card("Kaynaklı ön hesap, kontrollü belge taslağı");
        c.Children.Add(Text("Personel ekleyin, görevi ve kanıtları girin, ön hesabı inceleyin. Bağımsız tutar kontrolünden sonra yalnız kontrol taslağı oluşturulur."));
        var module = new ComboBox { ItemsSource = new[] { "Geçici görev", "Sürekli görev" }, SelectedIndex = -1 };
        AutomationProperties.SetName(module, "Hesaplama modülü");
        c.Children.Add(Text("Veri girişine başlamadan hesaplama modülünü seçin.")); c.Children.Add(module);
        var open = Button(c, "Seçili modülde veri girişini aç", () => { Navigation.SelectedIndex = module.SelectedIndex + 1; });
        open.IsEnabled = false; module.SelectionChanged += (_, _) => open.IsEnabled = module.SelectedIndex >= 0;
        var buttons = new WrapPanel(); c.Children.Add(buttons);
        Button(buttons, "Personel ekle", NewPerson); Button(buttons, "Geçici görev", () => { Navigation.SelectedIndex = 1; }); Button(buttons, "Sürekli görev", () => { Navigation.SelectedIndex = 2; });
        c = Card($"{_vm.Personnel.Count} personel · {_vm.Duties.Count} görev");
        c.Children.Add(Text("Veriler bilgisayarınızda JSON dosyalarında saklanır. SQL, SQLite veya Office gerekmez."));
        c.Children.Add(Text("Veri kökü: " + PortablePaths.Root));
        Button(c, "Sentetik demo kayıtlarını ekle", Demo);
        Page.Children.Add(Text("Örnek 27/28 resmî form ve ödeme mutabakatı bekliyor. Kanıt belirsizliği veya desteklenmeyen yıl hesaplamayı durdurur."));
    }
    private void NewPerson()
    {
        var p = new PersonnelRecord { FullName = "YENİ PERSONEL" }; _vm.Personnel.Add(p); _vm.SelectedPerson = p; _dirty = true;
        Navigation.SelectedIndex = 3; ShowPage();
    }
    private void Demo()
    {
        var p = new PersonnelRecord { FullName = "ÖRNEK PERSONEL", Title = "Öğretmen", Institution = "ÖRNEK KURUM", Grade = 5, Indicator = 0, Group = PersonnelTariffGroup.GeneralCivilServant };
        _vm.Personnel.Add(p); _vm.SelectedPerson = p; var d = DutyRecord.CreateDemo(p.Id); _vm.Duties.Add(d); _vm.SelectedDuty = d; _dirty = true;
        Status.Text = "Sentetik demo eklendi. Gerçek kanıt/onay yerine geçmez; incelemeleri kendiniz yapın."; Navigation.SelectedIndex = 1;
    }
    private void PersonnelPage()
    {
        Page.Children.Add(Text("Personel ve kayıtlar", 27));
        var buttons = new WrapPanel(); Page.Children.Add(buttons); Button(buttons, "Personel ekle", NewPerson); Button(buttons, "Kaydet · Ctrl+S", Save);
        Page.Children.Add(Text("Belirsiz ve özel statüde otomatik tarife yok. Derece ve ek gösterge açıkça girilmelidir; ek göstergede 0 geçerli bir beyandır."));
        var grid = Grid(_vm.Personnel, false); Col(grid, "Ad soyad", "FullName", 160); Col(grid, "Unvan", "Title", 120); Col(grid, "Kurum", "Institution", 150); Col(grid, "Derece", "Grade", 70); Col(grid, "Ek gösterge", "Indicator", 95);
        grid.Columns.Add(new DataGridComboBoxColumn { Header = "Statü", ItemsSource = Options(Enum.GetValues<PersonnelTariffGroup>()), DisplayMemberPath = "Label", SelectedValuePath = "Value", SelectedValueBinding = new Binding("Group"), Width = 170 });
        grid.SelectedItem = _vm.SelectedPerson; grid.SelectionChanged += (_, _) => _vm.SelectedPerson = grid.SelectedItem as PersonnelRecord;
        Page.Children.Add(grid);
        var c = Card("Görev kayıtları"); var list = new ListBox { ItemsSource = _vm.Duties, SelectedItem = _vm.SelectedDuty, Height = 150 }; c.Children.Add(list);
        Button(c, "Seçili görevi aç", () => { _vm.SelectedDuty = list.SelectedItem as DutyRecord ?? throw new InvalidOperationException("Görev seçin."); Navigation.SelectedIndex = _vm.SelectedDuty.Kind == DraftKind.PermanentDuty ? 2 : 1; });
    }
    private DataGrid Grid(System.Collections.IEnumerable items, bool add)
    {
        var grid = new DataGrid { ItemsSource = items, Height = 260, CanUserAddRows = add, Margin = new(0, 8, 0, 16) };
        grid.CellEditEnding += (_, e) => Invalidate(e.Row.Item); _editors.Add(grid); return grid;
    }
    private static void Col(DataGrid grid, string header, string path, double width, string? format = null) => grid.Columns.Add(new DataGridTextColumn {
        Header = header, Width = width, Binding = new Binding(path) { ValidatesOnExceptions = true, ConverterCulture = Tr, StringFormat = format } });
    private PersonnelRecord Person(DutyRecord d) => _vm.Personnel.FirstOrDefault(p => p.Id == d.PersonnelId) ?? throw new InvalidOperationException("Göreve ait personel bulunamadı.");
    private DutyRecord Duty() => _vm.SelectedDuty ?? throw new InvalidOperationException("Önce görev seçin veya yeni görev ekleyin.");
    private bool SelectDuty(DraftKind? kind = null)
    {
        var available = _vm.Duties.Where(x => kind is null || x.Kind == kind).ToArray();
        if (_vm.SelectedDuty is null || !available.Contains(_vm.SelectedDuty)) _vm.SelectedDuty = available.FirstOrDefault();
        var select = new ComboBox { ItemsSource = available, SelectedItem = _vm.SelectedDuty }; AutomationProperties.SetName(select, "Görev kaydı");
        select.SelectionChanged += (_, _) => { _vm.SelectedDuty = select.SelectedItem as DutyRecord; ShowPage(); }; Page.Children.Add(select);
        return _vm.SelectedDuty is not null;
    }
    private void NewDuty(DraftKind kind)
    {
        if (_selectedModule != kind) throw new InvalidOperationException("Önce hesaplama modülünü seçin.");
        var p = _vm.SelectedPerson ?? _vm.Personnel.FirstOrDefault() ?? throw new InvalidOperationException("Önce personel ekleyin.");
        var d = new DutyRecord { Kind = kind, PersonnelId = p.Id, DocumentNumber = (kind == DraftKind.TemporaryDuty ? "GEC-" : "SUR-") + Guid.NewGuid().ToString("N")[..8] };
        _vm.Duties.Add(d); _vm.SelectedDuty = d; _dirty = true; ShowPage();
    }
    private void TemporaryDutyPage() => DutyPage(DraftKind.TemporaryDuty);
    private void PermanentDutyPage() => DutyPage(DraftKind.PermanentDuty);
    private void DutyPage(DraftKind kind)
    {
        if (_selectedModule != kind) throw new InvalidOperationException("Önce hesaplama modülünü seçin.");
        Page.Children.Add(Text(kind == DraftKind.TemporaryDuty ? "Geçici görev ön hesabı" : "Sürekli görev · yer değiştirme", 27));
        var buttons = new WrapPanel(); Page.Children.Add(buttons); Button(buttons, "Yeni görev", () => NewDuty(kind)); Button(buttons, "Ön hesapla", () => Preview(false)); Button(buttons, "Kaydet", Save);
        if (!SelectDuty(kind)) { Page.Children.Add(Text("Bu türde görev yok. Yeni görev ekleyin.")); return; }
        var d = Duty(); var c = Card("Görev bilgileri");
        var person = new ComboBox { ItemsSource = _vm.Personnel, SelectedItem = Person(d) }; person.SelectionChanged += (_, _) => { if (person.SelectedItem is PersonnelRecord p) { d.PersonnelId = p.Id; Invalidate(d, "PersonnelId"); } }; c.Children.Add(person);
        Field(c, "Belge numarası", d, "DocumentNumber");
        var pair = new UniformGrid { Columns = 2 }; c.Children.Add(pair); var left = new StackPanel(); var right = new StackPanel(); pair.Children.Add(left); pair.Children.Add(right);
        Field(left, "Çıkış yeri", d, "Origin"); Field(right, "Varış yeri", d, "Destination");
        if (kind == DraftKind.TemporaryDuty)
        {
            Field(left, "Ayrılış · gg.AA.yyyy SS:dd", d, "Departure", "dd.MM.yyyy HH:mm"); Field(right, "Dönüş · gg.AA.yyyy SS:dd", d, "Return", "dd.MM.yyyy HH:mm");
        }
        else
        {
            Field(left, "Atama / işlem tarihi · gg.AA.yyyy", d, "Departure", "dd.MM.yyyy");
            Field(right, "İncelenen dönem bitiş tarihi · gg.AA.yyyy", d, "Return", "dd.MM.yyyy");
            c.Children.Add(Text("Yalnız yer değiştirme bileşeni hesaplanır. Tarih aralığı seyahat gündeliğine otomatik çevrilmez."));
        }
        Choice(c, "Memuriyet mahalli değerlendirmesi", Enum.GetValues<DutyStationAssessment>(), d, "Station");
        c = Card("Kanıt ve değerlendirme"); Field(c, "Görev oluru / atama kararı referansı", d, "AssignmentEvidence"); Field(c, "Memuriyet mahalli kanıtı", d, "StationEvidence"); Field(c, "İnceleyen beyanı / kayıt referansı", d, "Reviewer");
        Check(c, "Görev oluru incelendi", d, "AssignmentReviewed"); Check(c, "Mahalliyet incelendi", d, "StationReviewed");
        if (kind == DraftKind.PermanentDuty)
        {
            c = Card("Aile ve kaynaklı mesafe"); c.Children.Add(Text("Bu hesap tam sürekli görev toplamı değildir; aile yol/seyahat kalemleri ayrıca incelenir."));
            Field(c, "İncelenmiş aile ferdi sayısı", d, "FamilyCount"); Field(c, "Kaynaklı kilometre", d, "Kilometers"); Field(c, "KGM / mesafe kanıtı", d, "DistanceEvidence");
            Check(c, "Aile hak sahipliği incelendi", d, "FamilyReviewed"); Check(c, "Eş aynı güzergâhta memur olarak naklediliyor", d, "SpouseTransfer");
        }
        else
        {
            var travel = new StackPanel();
            Page.Children.Add(new Expander { Header = "Seyahat takvimi · m.43 aday süreleri", Foreground = (Brush)FindResource("Gold"), Content = travel, Margin = new(0, 8, 0, 14) });
            travel.Children.Add(Text("Gidiş hareketi ve dönüş varışı üstteki ayrılış/dönüş saatleridir. Aşağıdaki saatleri gerçek belgelerden girin; boş değer varsayılmaz."));
            Field(travel, "Gidiş varışı · gg.AA.yyyy SS:dd", d, "OutboundArrival", "dd.MM.yyyy HH:mm");
            Field(travel, "Fiilî görev başlangıcı", d, "WorkStarts", "dd.MM.yyyy HH:mm");
            Field(travel, "Fiilî görev bitişi", d, "WorkEnds", "dd.MM.yyyy HH:mm");
            Field(travel, "Dönüş hareketi", d, "InboundDeparture", "dd.MM.yyyy HH:mm");
            Field(travel, "Gidiş saat kanıtı", d, "OutboundEvidence"); Field(travel, "Dönüş saat kanıtı", d, "InboundEvidence"); Field(travel, "Fiilî görev saat kanıtı", d, "WorkTimeEvidence");
            Check(travel, "Fiilî görev saatleri belgeli incelendi", d, "WorkTimesReviewed");
            Check(travel, "Saat dilimi / saat değişimi şüphesi var", d, "ClockConcern");
            var travelResult = Text("Aday süreler henüz hesaplanmadı. Hak günlerine otomatik aktarım yok.");
            Button(travel, "Seyahat adaylarını göster", () => {
                var result = _service.PreviewTimeline(Person(d), d);
                travelResult.Text = result.Explanation + "\nGidiş aday birimi: " + result.OutboundArticle43CandidateUnits +
                    " · Dönüş aday birimi: " + result.InboundArticle43CandidateUnits +
                    "\nAday takvim: " + string.Join(", ", result.CandidateCalendarDates.Select(x => x.ToString("dd.MM.yyyy"))) +
                    "\nÇakışan tarihler: " + string.Join(", ", result.OverlapDates.Select(x => x.ToString("dd.MM.yyyy"))) +
                    "\nBunlar onaylı hak günleri değildir. Veri değişirse yeniden hesaplayın.";
                Status.Text = "Seyahat takvimi değerlendirildi; hak günleri veya tutar satırları değiştirilmedi.";
            });
            travel.Children.Add(travelResult);
            c = new StackPanel(); var expander = new Expander { Header = "Çok günlü görev · incelenmiş hak günleri", Foreground = (Brush)FindResource("Gold"), Content = c, Margin = new(0, 8, 0, 14) }; Page.Children.Add(expander);
            c.Children.Add(Text("Takvim günleri otomatik hak günü değildir. m.43 adayları buraya otomatik aktarılmaz."));
            Field(c, "Önceki gün sayısı · bilinmiyorsa boş", d, "PriorDays"); Field(c, "Önceki gün defteri kanıtı", d, "PriorDaysEvidence");
            Check(c, "Aynı kişi/yer/iş ve yıllık dönem incelendi", d, "AnnualPeriodReviewed"); Check(c, "Geçmiş gün defteri incelendi", d, "PriorDaysReviewed"); Check(c, "Hak günleri / seyahat incelemesi tamam", d, "EligibleDaysReviewed"); Check(c, "m.42 istisnaları ayrıca değerlendirildi", d, "Article42ExceptionsExcluded"); Check(c, "33(d) konaklama hak sahipliği incelendi", d, "LodgingReviewed");
            var grid = Grid(d.ReviewedDays, true); Col(grid, "Tarih", "Date", 130, "dd.MM.yyyy"); Col(grid, "Konaklama sırası", "LodgingOrdinal", 125); Col(grid, "Fatura TL", "LodgingAmount", 110); Col(grid, "Fatura kanıtı", "Receipt", 200); c.Children.Add(grid);
        }
        c = Card("Ön hesap sonucu"); c.Children.Add(Text(d.Snapshot is null ? "Henüz hesaplanmadı veya veri değişti. Ön hesapla düğmesini kullanın." : ResultText(d.Snapshot)));
        Button(c, "Gider ve belge kontrolüne geç", () => { Navigation.SelectedIndex = 4; });
    }
    private static string ResultText(CalculationSnapshot s) => (s.Amount is null ? "HESAP BLOKE" : s.Amount.Value.ToString("N6", Tr) + " TL · ham ön hesap") + "\n" + s.Explanation + "\n" + s.LegalBasis + "\nKategori: " + s.Category + " · " + s.TariffReference;
    private void Preview(bool review)
    {
        var d = Duty();
        if (review)
        {
            if (!d.AmountsReviewed || string.IsNullOrWhiteSpace(d.Reviewer)) throw new InvalidOperationException("Bağımsız tutar incelemesini işaretleyin ve görev ekranında inceleyen beyanını girin.");
            d.ReviewedAt = DateTimeOffset.UtcNow;
        }
        d.Snapshot = _service.Preview(Person(d), d); _dirty = true; Status.Text = ResultText(d.Snapshot); ShowPage();
    }
    private void DocumentsPage()
    {
        Page.Children.Add(Text("Belge taslakları ve kontrol icmali", 27));
        Page.Children.Add(Text("Tam kuruş satırları bağımsız belgelerden girilir. Ham ön hesap otomatik ödeme tutarına çevrilmez."));
        if (_selectedModule is null) { Page.Children.Add(Text("Önce ana sayfadan Geçici veya Sürekli görev modülünü seçin.")); return; }
        Page.Children.Add(Text(_selectedModule == DraftKind.TemporaryDuty ? "Geçici görev kayıtları" : "Sürekli görev kayıtları"));
        if (!SelectDuty(_selectedModule)) return; var d = Duty();
        var buttons = new WrapPanel(); Page.Children.Add(buttons);
        Button(buttons, "Gider satırı ekle", () => { d.Expenses.Add(new() { LineId = "L" + Guid.NewGuid().ToString("N")[..6] }); Invalidate(); ShowPage(); });
        Button(buttons, "TSV yapıştır", () => ImportRows(Exchange.ParseExpenses(Clipboard.GetText())));
        Button(buttons, "XLSX al", ImportXlsx); Button(buttons, "XLSX ver", ExportXlsx);
        var grid = Grid(d.Expenses, false); Col(grid, "Satır", "LineId", 80);
        grid.Columns.Add(new DataGridComboBoxColumn { Header = "Gider türü", ItemsSource = Options(Enum.GetValues<DraftLineKind>()), DisplayMemberPath = "Label", SelectedValuePath = "Value", SelectedValueBinding = new Binding("Kind"), Width = 150 });
        Col(grid, "Açıklama", "Description", 180); Col(grid, "Tutar (TL)", "Amount", 100); Col(grid, "Kanıt", "Evidence", 150); Col(grid, "Mevzuat", "LegalBasis", 150); Page.Children.Add(grid);
        Button(Page, "Seçili gider satırını kaldır", () => { if (grid.CurrentItem is ExpenseEntry row) { d.Expenses.Remove(row); Invalidate(); ShowPage(); } });
        var c = Card("Avans ve bağımsız toplam kontrolü"); Field(c, "Avans TL", d, "Advance"); Field(c, "Avans kanıtı", d, "AdvanceEvidence"); Field(c, "Bağımsız brüt TL", d, "IndependentGross"); Field(c, "Bağımsız net TL", d, "IndependentNet");
        Check(c, "Hak sahipliği, kanıtlar ve tam kuruş tutarlar bağımsız incelendi", d, "AmountsReviewed");
        buttons = new WrapPanel(); c.Children.Add(buttons); Button(buttons, "İncelemeyi kaydet / hesapla", () => Preview(true)); Button(buttons, "Taslak PDF oluştur / aç", MakePdf); Button(buttons, "Kontrol icmali", MakeSummary); Button(buttons, "Çıktı klasörü", () => OpenFolder(PortablePaths.Outputs));
        c.Children.Add(Text("Veri değişince inceleme ve önceki sonuç geçersiz olur. Çıktılar imzaya veya muhasebeye sunulacak resmî ödeme belgeleri değildir."));
    }
    private void ImportRows(List<ExpenseEntry> rows)
    {
        var d = Duty(); var all = d.Expenses.Concat(rows).ToArray();
        _ = Exchange.ParseExpenses(Exchange.ToTsv(all));
        if (MessageBox.Show(this, $"{rows.Count} gider satırı doğrulandı. Mevcut {d.Expenses.Count} satıra eklensin mi?", "Aktarım önizlemesi", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        d.Expenses.AddRange(rows); Invalidate(); ShowPage(); Status.Text = "Giderler eklendi. İnceleme işaretleri aktarılmadı; yeniden kontrol edin.";
    }
    private void ImportXlsx() { var dialog = new OpenFileDialog { Filter = "Yolluk gider şablonu (*.xlsx)|*.xlsx" }; if (dialog.ShowDialog(this) == true) ImportRows(Exchange.ReadXlsx(dialog.FileName)); }
    private void ExportXlsx()
    {
        var dialog = new SaveFileDialog { Filter = "Excel değer dosyası (*.xlsx)|*.xlsx", FileName = "YOLLUK_GIDER_" + Guid.NewGuid().ToString("N")[..8] + ".xlsx", InitialDirectory = PortablePaths.Outputs };
        if (dialog.ShowDialog(this) == true) { Exchange.WriteXlsx(dialog.FileName, Duty().Expenses); Status.Text = "XLSX kaydedildi: " + dialog.FileName; }
    }
    private string OutputPath(string prefix) => Path.Combine(PortablePaths.Outputs, prefix + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..8] + ".pdf");
    private DraftDocument Prepare(DutyRecord d)
    {
        var result = _service.PrepareDraft(Person(d), d);
        return result.MayPrintDraft ? result.Document! : throw new InvalidOperationException(result.Reason);
    }
    private void MakePdf()
    {
        var d = Prepare(Duty()); var path = OutputPath("KONTROL_TASLAGI");
        TravelDraftPdfWriter.SaveDraft(d, path); Status.Text = "KONTROL TASLAĞI oluşturuldu: " + path;
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    private void MakeSummary()
    {
        var docs = _vm.Duties.Where(x => x.AmountsReviewed).Select(Prepare).ToArray();
        var result = DraftDocumentService.CreateSummary(docs);
        if (!result.Ready) throw new InvalidOperationException(result.Reason);
        var path = OutputPath("KONTROL_ICMALI"); TravelDraftPdfWriter.SaveSummary(result.Summary!, path); Status.Text = "Kontrol icmali: " + path;
    }
    private void Save()
    {
        if (_loadFailed) throw new InvalidOperationException("Açılışta kayıt okunamadı; önce yedekten kurtarın. Bozuk kayıt ezilmeyecek.");
        _vm.Synchronize(); _repository.Save(_vm.Workspace, _vm.Workspace.Revision); _dirty = false;
        Status.Text = $"Kayıtlar kaydedildi · revizyon {_vm.Workspace.Revision}. Veri: {_repository.FilePath}";
    }
    private void SourcesPage()
    {
        Page.Children.Add(Text("Kaynak ve tarifeler", 27)); var c = Card("2026 H Cetveli · I-B");
        c.Children.Add(Text("850–890 TL genel memur/hizmetli kategorileri mevcut C# motorundan seçilir. Diğer yıl ve özel statüler bloke edilir."));
        c.Children.Add(Text("Dosya varlığı veya SHA-256 hukuki güncellik kanıtı değildir. BUT-003 başlık/içerik eşlemesi ve resmî form mutabakatı inceleme bekliyor."));
        c.Children.Add(Text("Kaynak klasörü: " + PortablePaths.Sources));
        c.Children.Add(Text($"Yerel kaynak: {Directory.EnumerateFiles(PortablePaths.Sources, "*.pdf", SearchOption.AllDirectories).Count()} PDF. Kaynaklar GitHub'a otomatik yüklenmez."));
        Button(c, "Kaynak klasörünü aç", () => OpenFolder(PortablePaths.Sources));
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
            _repository.Restore(dialog.FileName); _vm.Load(_repository.Load()); _dirty = false; _loadFailed = false; Status.Text = "Yedek geri yüklendi; önceki dosya da korundu."; ShowPage();
        });
        Button(c, "Veriler klasörü", () => OpenFolder(PortablePaths.Data)); Button(c, "Yedekler klasörü", () => OpenFolder(PortablePaths.Backups));
        var profiles = new ComboBox { ItemsSource = new[] { "1024 × 768", "1366 × 768", "1920 × 1080" }, SelectedIndex = 1 }; c.Children.Add(profiles);
        Button(c, "Pencere profilini uygula", () => { var sizes = new[] { (1024d, 768d), (1366d, 768d), (1920d, 1080d) }; var (w, h) = sizes[profiles.SelectedIndex]; ApplyProfile(w, h); JsonFileStore.Save(Path.Combine(PortablePaths.Data, "ekran.json"), new WindowProfile(w, h)); });
    }
    private void ApplyProfile(double w, double h) { Width = Math.Clamp(w, MinWidth, Math.Max(MinWidth, SystemParameters.WorkArea.Width)); Height = Math.Clamp(h, MinHeight, Math.Max(MinHeight, SystemParameters.WorkArea.Height)); }
    private static void OpenFolder(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        try { CommitEditors(); } catch { e.Cancel = true; Status.Text = "Hatalı alanı düzeltmeden pencere kapatılamaz."; return; }
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
