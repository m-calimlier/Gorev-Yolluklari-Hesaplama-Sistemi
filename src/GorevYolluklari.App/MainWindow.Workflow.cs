using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;
using GorevYolluklari.Core.Tariffs;
using GorevYolluklari.Storage;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private void Navigate()
    {
        if (_navigating) return;
        var target = Navigation.SelectedIndex;
        if (target < 0 || target == _currentPage)
        {
            if (Page.Children.Count == 0 && target >= 0) ShowPage();
            return;
        }
        var previous = _currentPage;
        try
        {
            CommitEditors();
            _currentPage = target;
            ShowPage();
        }
        catch (Exception ex)
        {
            _navigating = true;
            Navigation.SelectedIndex = _currentPage = previous;
            _navigating = false;
            Status.Text = ex.Message;
        }
    }

    private void MarkChanged(object? source)
    {
        if (ReferenceEquals(source, _formPerson) && source is not null)
        {
            _personChanged = true;
            return;
        }
        if (Navigation.SelectedIndex == 1 &&
            (source is null or ReviewedDayEntry or ExpenseEntry || ReferenceEquals(source, _form) || ReferenceEquals(source, _formPerson)))
            _formChanged = true;
        else
            _dirty = true;
    }

    private string? _formPersonBaseline;
    private bool _personChanged;
    private TextBlock? _personStatus;
    private FrameworkElement? _taskStart;
    private readonly List<FrameworkElement> _dependentEntryCards = [];

    private bool PersonIsReady => _formPerson is not null && _formPersonBaseline is not null &&
        JsonSerializer.Serialize(_formPerson) == _formPersonBaseline;

    private (StackPanel Left, StackPanel Right) EntryRow()
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        var left = new StackPanel();
        var right = new StackPanel();
        System.Windows.Controls.Grid.SetColumn(right, 2);
        row.Children.Add(left);
        row.Children.Add(right);
        Page.Children.Add(row);
        return (left, right);
    }

    private Panel Dependent(Panel panel)
    {
        _dependentEntryCards.Add(panel);
        return panel;
    }

    private void RefreshEntryAvailability()
    {
        var ready = PersonIsReady;
        foreach (var card in _dependentEntryCards) card.IsEnabled = true;
        if (_personStatus is not null)
            _personStatus.Text = ready ? _formPerson!.FullName + " · Hazır" : "Hesaplama/PDF sırasında kişi otomatik kaydedilir.";
    }


    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    private void EntryPage()
    {
        Page.Children.Add(Text("Veri girişi", 28));
        var tabs = new TabControl { Margin = new(0, 0, 0, 14), Style = (Style)FindResource("DutyTabs") };
        var tabStyle = (Style)FindResource("DutyTab");
        // WPF otomatik olarak bir sekme seçer. Görünmeyen boş sekme, açık kullanıcı seçimini korur.
        tabs.Items.Add(new TabItem { Visibility = Visibility.Collapsed, IsEnabled = false });
        tabs.Items.Add(new TabItem { Header = "Geçici Görev", Style = tabStyle });
        tabs.Items.Add(new TabItem { Header = "Sürekli Görev", Style = tabStyle });
        tabs.SelectedIndex = _selectedModule switch { DraftKind.TemporaryDuty => 1, DraftKind.PermanentDuty => 2, _ => 0 };
        AutomationProperties.SetName(tabs, "Görev türü");
        tabs.SelectionChanged += (_, e) =>
        {
            if (!ReferenceEquals(e.Source, tabs) || tabs.SelectedIndex < 1) return;
            Safe(() =>
            {
                var requested = tabs.SelectedIndex == 1 ? DraftKind.TemporaryDuty : DraftKind.PermanentDuty;
                if ((_formChanged || _personChanged) && _dialog("Bu formdaki değişiklikler bırakılsın mı?", "Sekme değiştir", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    ShowPage();
                    return;
                }
                StartEntry(requested);
            });
            if (Page.Children.Contains(tabs) && tabs.SelectedIndex != (_selectedModule switch
                { DraftKind.TemporaryDuty => 1, DraftKind.PermanentDuty => 2, _ => 0 }))
                ShowPage();
        };
        Page.Children.Add(tabs);
        if (_form is null) Card("Görev türünü seçin").Children.Add(Text("Geçici veya Sürekli görev sekmesinden başlayın."));
        else
        {
            var existing = _vm.Duties.Where(d => d.Kind == _form.Kind).ToArray();
            if (existing.Length > 0)
            {
                var picker = new ComboBox { ItemsSource = existing, SelectedItem = existing.FirstOrDefault(d => d.Id == _form.Id),
                    Style = (Style)FindResource("HakedisFieldCombo") };
                AutomationProperties.SetName(picker, "Mevcut görev");
                Page.Children.Add(new Label { Content = "Mevcut Görevi Aç" });
                Page.Children.Add(picker);
                var restoring = false;
                picker.SelectionChanged += (_, _) =>
                {
                    if (restoring || picker.SelectedItem is not DutyRecord duty || duty.Id == _form?.Id) return;
                    Safe(() => OpenEntry(duty));
                    restoring = true;
                    picker.SelectedItem = existing.FirstOrDefault(d => d.Id == _form?.Id);
                    restoring = false;
                };
            }
            DutyPage(_form);
        }
    }

    private void StartEntry(DraftKind kind)
    {
        ResetPermanentStep();
        _temporaryPreview = null;
        _selectedModule = kind;
        _formPerson = new PersonnelRecord();
        _formPersonBaseline = null;
        _form = new DutyRecord
        {
            Kind = kind,
            QuickTemporaryEnabled = kind == DraftKind.TemporaryDuty,
            PersonnelId = _formPerson.Id,
            Departure = default,
            Return = default,
            DocumentNumber = (kind == DraftKind.TemporaryDuty ? "GEC-" : "SUR-") + Guid.NewGuid().ToString("N")[..8]
        };
        _formChanged = false;
        _personChanged = false;
        ShowPage();
    }

    private void OpenEntry(DutyRecord duty)
    {
        if ((_formChanged || _personChanged) && _dialog("Açık formdaki değişiklikler bırakılsın mı?", "Görev formu", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;
        ResetPermanentStep();
        _temporaryPreview = null;
        _selectedModule = duty.Kind;
        _form = Copy(duty);
        _formPerson = Copy(Person(duty));
        _formPersonBaseline = JsonSerializer.Serialize(_formPerson);
        _formChanged = false;
        _personChanged = false;
        if (Navigation.SelectedIndex == 1) ShowPage();
        else Navigation.SelectedIndex = 1;
    }

    private void BuildEntryPersonnel(Panel card)
    {
        var person = _formPerson!;
        var choices = new[] { new PersonChoice(null, "Yeni kişi") }
            .Concat(_vm.Personnel.Select(p => new PersonChoice(p, p.FullName))).ToArray();
        var picker = new ComboBox
        {
            Style = (Style)FindResource("HakedisFieldCombo"),
            ItemsSource = choices,
            DisplayMemberPath = nameof(PersonChoice.Label),
            SelectedItem = choices.FirstOrDefault(x => x.Person?.Id == person.Id) ?? choices[0]
        };
        AutomationProperties.SetName(picker, "Personel seçimi");
        card.Children.Add(new Label { Content = "Personel Seçimi",
            Style = (Style)FindResource("HakedisFieldLabel") });
        card.Children.Add(picker);
        var restoring = false;
        picker.SelectionChanged += (_, _) =>
        {
            if (restoring || picker.SelectedItem is not PersonChoice selected) return;
            var oldChoice = choices.FirstOrDefault(x => x.Person?.Id == _formPerson!.Id) ?? choices[0];
            Safe(() =>
            {
                if (_personChanged && _dialog("Kişi bilgilerindeki değişiklikler bırakılsın mı?", "Personel seçimi",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                SetEntryPerson(selected.Person is null ? new PersonnelRecord() : Copy(selected.Person));
            });
            restoring = true;
            picker.SelectedItem = oldChoice;
            restoring = false;
        };
        var fields = new StackPanel();
        card.Children.Add(fields);
        Field(fields, "Ad soyad", person, nameof(person.FullName));
        Choice(fields, "Statü", Enum.GetValues<PersonnelTariffGroup>(), person, nameof(person.Group));
        Choice(fields, "Derece", Enumerable.Range(1, 15), person, nameof(person.Grade));
        Field(fields, "Ek gösterge", person, nameof(person.Indicator));
        var optionalPerson = new StackPanel();
        card.Children.Add(new Expander
        {
            Header = "İsteğe bağlı: unvan ve kurum",
            Content = optionalPerson,
            IsExpanded = !string.IsNullOrWhiteSpace(person.Title) ||
                         !string.IsNullOrWhiteSpace(person.Institution),
            Margin = new(0, 8, 0, 6),
            Foreground = (Brush)FindResource("Gold")
        });
        Field(optionalPerson, "Unvan", person, nameof(person.Title));
        Field(optionalPerson, "Kurum", person, nameof(person.Institution));
        card.Children.Add(Text("Bilgiler Hesapla veya PDF aşamasında otomatik kaydedilir.", 12));
        Button(card, "Kişiyi kaydet", SaveEntryPerson);
        _personStatus = Text("", 12);
        card.Children.Add(_personStatus);
    }

    public sealed record PersonChoice(PersonnelRecord? Person, string Label);

    private void SetEntryPerson(PersonnelRecord person)
    {
        _formPerson = person;
        _personChanged = false;
        _formPersonBaseline = _vm.Personnel.Any(p => p.Id == person.Id) ? JsonSerializer.Serialize(person) : null;
        var duty = Duty();
        duty.PersonnelId = person.Id;
        Invalidate(duty, nameof(duty.PersonnelId));
        ShowPage();
    }

    private void CancelEntry()
    {
        if ((_formChanged || _personChanged) && _dialog("Formdaki değişiklikler bırakılsın mı?", "Veri girişi", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;
        ClearEntry();
        ShowPage();
    }

    private void ClearEntry()
    {
        ResetPermanentStep();
        _temporaryPreview = null;
        _form = null;
        _formPerson = null;
        _formPersonBaseline = null;
        _selectedModule = null;
        _formChanged = false;
        _personChanged = false;
    }

    private void SaveCurrent()
    {
        if (Navigation.SelectedIndex != 1 || _form is null) Save();
        else SaveEntry(); // Kişi kaydı gerekiyorsa görev kaydı sırasında otomatik yapılır.
    }

    private void SaveEntryPerson()
    {
        var person = _formPerson!;
        CheckFormIssues(PersonIssues(person));
        if (!PersonIsReady)
        {
            var next = CurrentWorkspace();
            var index = next.Personnel.FindIndex(p => p.Id == person.Id);
            if (index < 0) next.Personnel.Add(Copy(person));
            else next.Personnel[index] = Copy(person);
            SaveWorkspace(next, _vm.SelectedDuty?.Id);
            _formPersonBaseline = JsonSerializer.Serialize(person);
            _personChanged = false;
            if (_form?.Kind != DraftKind.PermanentDuty) ShowPage();
        }
        _personChanged = false;
        if (_form?.Kind == DraftKind.PermanentDuty)
        {
            MoveToPermanentAssignment();
            return;
        }
        RefreshEntryAvailability();
        Status.Text = "Kişi kaydedildi. Sağdaki görev kartından devam edin.";
        _taskStart?.Focus();
    }

    private void SaveEntry() => SaveEntryCore(notify: true);

    private void SaveAndMakePdf()
    {
        SaveEntryCore(notify: false);
        if (_vm.SelectedDuty is { } saved) MakePdf(saved);
    }

    private void SaveEntryCore(bool notify)
    {
        if (_form?.Kind == DraftKind.PermanentDuty)
            SetPermanentCalculationDefaults(Duty());
        if (_form?.Kind == DraftKind.TemporaryDuty && _form.QuickTemporaryEnabled)
            SetQuickTemporaryDefaults(Duty());
        if (!PersonIsReady) SaveEntryPerson();
        if (!PersonIsReady) throw new InvalidOperationException("Kişi bilgilerini tamamlayın.");
        var person = _formPerson!;
        var candidate = Copy(Duty());
        if (_formPersonBaseline is not null &&
            JsonSerializer.Serialize(_vm.Personnel.FirstOrDefault(p => p.Id == person.Id)) != _formPersonBaseline)
            throw new InvalidOperationException("Personel kaydı form açıkken değişti. Güncel personeli seçip yeniden deneyin.");
        CheckFormIssues(DutyIssues(candidate, person));
        if (_vm.Duties.Any(d => d.Id != candidate.Id && string.Equals(d.DocumentNumber.Trim(),
            candidate.DocumentNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            CheckFormIssues([("DocumentNumber", "Bu belge numarası başka bir görevde kullanılıyor.")]);

        // Belge incelemesi yapıldığını varsaymadan tutarı hesapla.
        // Tutarı iki kez yazdırma ve "kontrol edildi" beyanı isteme.
        candidate.AmountsReviewed = false;
        candidate.ReviewedAt = null;
        var service = Service;
        var calculation = service.PreviewForCalculation(person, candidate);
        if (calculation.Amount is null)
            CheckFormIssues([(candidate.Kind == DraftKind.TemporaryDuty && candidate.Departure.Date != candidate.Return.Date
                ? "ReviewedDays" : "Departure", calculation.Explanation)]);
        if (candidate.Kind == DraftKind.TemporaryDuty && candidate.QuickTemporaryEnabled)
            SyncQuickTemporaryExpenses(candidate, calculation);
        else
            SyncCalculatedExpense(candidate, calculation);
        if (candidate.Kind == DraftKind.PermanentDuty)
            SyncPermanentQuickExpenses(candidate, calculation);
        candidate.Snapshot = service.PreviewForCalculation(person, candidate);
        if (!candidate.QuickTemporaryEnabled)
            CheckFormIssues(CalculatedExpenseIssues(candidate, candidate.Snapshot));
        CheckFormIssues(ExpenseIssues(candidate));
        if (candidate.Advance < 0m || (candidate.Expenses.Count > 0 &&
                candidate.Advance > candidate.Expenses.Sum(x => x.Amount)))
            CheckFormIssues([("Advance", "Avans tutarı gider toplamını aşamaz veya negatif olamaz.")]);

        var next = CurrentWorkspace();
        var dutyIndex = next.Duties.FindIndex(d => d.Id == candidate.Id);
        if (dutyIndex < 0) next.Duties.Add(candidate);
        else next.Duties[dutyIndex] = candidate;
        SaveWorkspace(next, candidate.Id);
        ClearEntry();
        Status.Text = "Görev ve hesap sonucu kaydedildi.";
        if (notify) _dialog("Görev ve hesap sonucu kaydedildi.", "Kayıt başarılı", MessageBoxButton.OK);
        Navigation.SelectedIndex = 2;
    }

    private static void SyncQuickTemporaryExpenses(DutyRecord duty, CalculationSnapshot preview)
    {
        var parts = preview.Components ?? [];
        decimal Part(string name) => parts.FirstOrDefault(x => x.Label == name)?.Amount ?? 0m;
        void Add(string key, DraftLineKind kind, decimal amount, string text, string basis)
        {
            var id = "OTO-" + key + "-" + duty.Id.ToString("N");
            var current = duty.Expenses.FirstOrDefault(x => x.LineId == id);
            if (amount <= 0m)
            {
                if (current is not null) duty.Expenses.Remove(current);
                return;
            }
            if (current is null)
            {
                current = new ExpenseEntry { LineId = id };
                duty.Expenses.Add(current);
            }
            current.Kind = kind;
            current.Amount = TransferAmount(amount);
            current.Description = text;
            current.Evidence = "";
            current.LegalBasis = basis;
        }
        Add("GQ", DraftLineKind.TicketOrTravel, Part("Gidiş ulaşım"),
            "Gidiş ulaşım bedeli", "6245 m.14, m.27");
        Add("RQ", DraftLineKind.TicketOrTravel, Part("Dönüş ulaşım"),
            "Dönüş ulaşım bedeli", "6245 m.14, m.27");
        Add("TQ", DraftLineKind.DailyAllowance,
            Part("Gidiş yol gündeliği") + Part("Görev gündeliği") + Part("Dönüş yol gündeliği"),
            "Gidiş, görev ve dönüş gündeliği", "6245 m.39, m.42, m.43");
        Add("LQ", DraftLineKind.Accommodation, Part("Konaklama"),
            "Konaklama (2026 H Cetveli tavanlı)", "6245 m.33(d), 2026 H Cetveli I dipnot 1");
        Add("OQ", DraftLineKind.OtherEligible, Part("Diğer masraflar"),
            "6245 m.14 kapsamı diğer masraflar", "6245 m.14");
    }

    private static void SyncCalculatedExpense(DutyRecord duty, CalculationSnapshot preview)
    {
        if (preview.Amount is null || preview.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal)) return;
        var permanent = duty.Kind == DraftKind.PermanentDuty;
        var raw = permanent ? preview.Amount : DailyAmount(duty, preview);
        var kind = permanent ? DraftLineKind.Relocation : DraftLineKind.DailyAllowance;
        var id = permanent ? RelocationLineId(duty) : DailyLineId(duty);
        if (raw is not decimal candidate || candidate <= 0m)
        {
            duty.Expenses.RemoveAll(x => x.LineId == id);
            return;
        }
        var amount = TransferAmount(candidate);
        var row = duty.Expenses.FirstOrDefault(x => x.LineId == id);
        // Önceden elle girilmiş aynı türdeki kalemin üzerine otomatik ikinci satır ekleme.
        if (row is null && duty.Expenses.Any(x => x.Kind == kind)) return;
        if (row is null)
        {
            row = new ExpenseEntry { LineId = id };
            duty.Expenses.Add(row);
        }
        row.Kind = kind;
        row.Amount = amount;
        row.Description = permanent ? "Yer değiştirme gideri" : "Gündelik";
        row.LegalBasis = permanent ? "6245 m.45" : preview.LegalBasis;
        row.Evidence = duty.AssignmentEvidence; // Boş olabilir: belgeyi memur kontrol eder.

        // Çok günlü görevde motorun hesapladığı konaklama üst sınırını PDF'ye
        // ayrıca aktarmak gerekir; aksi halde gündelik+konaklama eksik görünür.
        if (!permanent)
        {
            var accommodationId = "OTO-KN-" + duty.Id.ToString("N");
            if (preview.Components?.FirstOrDefault(x => x.Label == "Konaklama") is
                not { Amount: > 0m } lodging)
            {
                duty.Expenses.RemoveAll(x => x.LineId == accommodationId);
                return;
            }
            var accommodation = duty.Expenses.FirstOrDefault(x => x.LineId == accommodationId);
            if (accommodation is null && duty.Expenses.Any(x => x.Kind == DraftLineKind.Accommodation)) return;
            if (accommodation is null)
            {
                accommodation = new ExpenseEntry { LineId = accommodationId };
                duty.Expenses.Add(accommodation);
            }
            accommodation.Kind = DraftLineKind.Accommodation;
            accommodation.Description = "Konaklama (gündelik üst sınırı)";
            accommodation.Amount = TransferAmount(lodging.Amount);
            accommodation.Evidence = "";
            accommodation.LegalBasis = "6245 m.33(d)";
        }
    }

    // Referans dokuz alanlı sürekli görev ekranındaki giderler otomatik oluşturulur.
    // Genel varsayım: bir seyahat günü; tek kişilik taşıt ücreti bütün yolcular için eşit.
    // Kaydın önceki manuel giderleri otomatik silinmez / tekrarlanmaz.
    private static void SyncPermanentQuickExpenses(DutyRecord duty, CalculationSnapshot preview)
    {
        if (preview.DailyRate is not decimal rate || rate <= 0m) return;
        void SetLine(DraftLineKind kind, string key, decimal raw, string label, string legal)
        {
            var id = "OTO-" + key + "-" + duty.Id.ToString("N");
            var old = duty.Expenses.FirstOrDefault(x => x.LineId == id);
            if (raw <= 0m)
            {
                if (old is not null) duty.Expenses.Remove(old);
                return;
            }
            var sameKind = duty.Expenses.Where(x => x.Kind == kind && x.LineId != id).ToArray();
            if (sameKind.Length != 0)
            {
                // Eski kayıtların elle girilmiş satırlarına müdahale etmeyin.
                // Çift ödeme oluşmaması için yeni bir otomatik satır da üretmeyin.
                if (old is not null) duty.Expenses.Remove(old);
                return;
            }
            if (old is null)
            {
                old = new ExpenseEntry { LineId = id };
                duty.Expenses.Add(old);
            }
            old.Kind = kind;
            old.Amount = TransferAmount(raw);
            old.Description = label;
            old.Evidence = "";
            old.LegalBasis = legal;
        }
        var family = duty.FamilyCount;
        var fare = duty.TravelFarePerPerson ?? 0m;
        SetLine(DraftLineKind.DailyAllowance, "SG", rate,
            "Personel seyahat gündeliği (1 gün)", "6245 m.43");
        SetLine(DraftLineKind.FamilyDaily, "AG", family * rate,
            "Aile seyahat gündeliği (1 gün / kişi)", "6245 m.44");
        SetLine(DraftLineKind.TicketOrTravel, "ST", fare,
            "Personel taşıt bedeli", "6245 m.27");
        SetLine(DraftLineKind.FamilyTravel, "AT", family * fare,
            "Aile taşıt bedeli (eşit ücret varsayımı)", "6245 m.44");
    }

    private WorkspaceRecords CurrentWorkspace() => new()
    {
        Revision = _vm.Workspace.Revision,
        Constants = _vm.Workspace.Constants,
        Personnel = _vm.Personnel.ToList(),
        Duties = _vm.Duties.ToList()
    };

    private void SaveWorkspace(WorkspaceRecords next, Guid? selectedDutyId)
    {
        if (_loadFailed) throw new InvalidOperationException("Kayıt dosyası açılamadı. Yedekten kurtarın.");
        _repository.Save(next, _vm.Workspace.Revision);
        _vm.Load(next);
        _vm.SelectedDuty = _vm.Duties.FirstOrDefault(d => d.Id == selectedDutyId);
        _dirty = false;
    }

    private void DeleteDuty(DutyRecord duty)
    {
        if (_dialog(duty + " kaydı silinsin mi?", "Görevi sil", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        var next = CurrentWorkspace();
        next.Duties.RemoveAll(d => d.Id == duty.Id);
        SaveWorkspace(next, null);
        if (_form?.Id == duty.Id) ClearEntry();
        Status.Text = "Görev kaydı silindi.";
        ShowPage();
    }

    private void ResultsPage(bool home)
    {
        Page.Children.Add(Text(home ? "Ana Sayfa" : "Hesapla · sonuçlar", 28));
        var records = _vm.Duties.Where(d => d.Snapshot?.Amount is not null)
            .OrderByDescending(d => d.Snapshot!.CreatedAt).ToArray();
        var first = records.Length > 0 ? EntryRow() : default;
        var summary = Card(home ? "Hesap arşivi" : "Hesaplanan görevler", first.Left);
        if (home)
        {
            var metrics = new UniformGrid { Columns = 3, Margin = new(0, 0, 0, 12) };
            metrics.Children.Add(Metric("Hesaplanan kayıt", records.Length.ToString(Tr)));
            metrics.Children.Add(Metric("Geçici görev", records.Count(d => d.Kind == DraftKind.TemporaryDuty).ToString(Tr)));
            metrics.Children.Add(Metric("Sürekli görev", records.Count(d => d.Kind == DraftKind.PermanentDuty).ToString(Tr)));
            summary.Children.Add(metrics);
        }
        if (records.Length == 0)
        {
            summary.Children.Add(Text("Henüz hesaplanmış görev yok."));
            Button(summary, "Veri girişine git", () => Navigation.SelectedIndex = 1);
            return;
        }

        var selected = records.Contains(_vm.SelectedDuty) ? _vm.SelectedDuty! : records[0];
        _vm.SelectedDuty = selected;
        var picker = new ComboBox { ItemsSource = records, SelectedItem = selected };
        AutomationProperties.SetName(picker, "Hesaplanmış görev");
        summary.Children.Add(new Label { Content = "Görev Seçimi" });
        summary.Children.Add(picker);
        picker.SelectionChanged += (_, _) =>
        {
            if (picker.SelectedItem is not DutyRecord duty) return;
            _vm.SelectedDuty = duty;
            ShowPage();
        };

        var snapshot = selected.Snapshot!;
        var person = Person(selected);
        var current = Service.IsCurrent(person, selected);
        var calculation = Card(person.FullName, first.Right);
        calculation.Children.Add(Text(selected.Kind == DraftKind.TemporaryDuty ? "Geçici görev · ön hesap" : "Sürekli görev · yer değiştirme"));
        var figures = new UniformGrid { Columns = 2, Margin = new(0, 0, 0, 12) };
        figures.Children.Add(Metric("Ham ön hesap", Money(snapshot.Amount)));
        figures.Children.Add(Metric("Gündelik", Money(snapshot.DailyRate)));
        calculation.Children.Add(figures);
        foreach (var part in snapshot.Components ?? [])
            calculation.Children.Add(Text($"{part.Label} · {Money(part.Amount)}"));
        calculation.Children.Add(Text(snapshot.Explanation));
        calculation.Children.Add(Text(snapshot.LegalBasis + " · " + snapshot.TariffReference));
        if (!current) calculation.Children.Add(Text("Eski hesap. Güncel bilgilerle yeniden kaydedin."));
        if (snapshot.RuleVersion.StartsWith("SIM-", StringComparison.Ordinal))
            calculation.Children.Add(Text("Simülasyon · düzenlenmiş sabitler kullanıldı."));

        var second = EntryRow();
        var identity = Card("Personel ve güzergâh", second.Left);
        identity.Children.Add(Text($"{person.Title} · {person.Institution}"));
        var dates = selected.Kind == DraftKind.PermanentDuty ? $"{selected.Departure:dd.MM.yyyy}"
            : $"{selected.Departure:dd.MM.yyyy} – {selected.Return:dd.MM.yyyy}";
        identity.Children.Add(Text($"{selected.Origin} → {selected.Destination}  ·  {dates}"));

        var review = Service.PrepareCalculationPdf(person, selected);
        var document = Card("Belge durumu", second.Right);
        if (review.MayPrintDraft) document.Children.Add(Text("Yolluk hesaplama PDF'si hazır."));
        else
        {
            var reason = Text(review.Reason);
            reason.Foreground = Brushes.LightCoral;
            document.Children.Add(reason);
            Button(document, "Kaydı düzenle", () => OpenEntry(selected));
        }
        if (selected.Expenses.Count > 0)
        {
            foreach (var row in selected.Expenses)
                document.Children.Add(Text($"{row.Description} · {Money(row.Amount)}"));
        }
        if (selected.Expenses.Count > 0)
        {
            var payment = Text(ComputePaymentSummary(selected), 16);
            payment.Foreground = (Brush)FindResource("Gold");
            document.Children.Add(payment);
            document.Children.Add(Text("Bu belge yolluk hesaplama çıktısıdır; kurumun resmi inceleme ve onayının yerine geçmez.", 12));
        }
        var actions = new WrapPanel();
        Page.Children.Add(actions);
        Button(actions, "Sil", () => DeleteDuty(selected));
        Button(actions, "Düzenle", () => OpenEntry(selected));
        Button(actions, "PDF", () => MakePdf(selected)).IsEnabled = review.MayPrintDraft;
    }

    private static string Money(decimal? amount) => amount is decimal value ? value.ToString("N2", Tr) + " TL" : "—";
}
