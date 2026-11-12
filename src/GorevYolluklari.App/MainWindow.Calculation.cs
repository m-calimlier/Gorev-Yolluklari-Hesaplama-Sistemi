using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    // Hesapla komutu kalıcı veri yazmaz. Hesap sonucu ayrı Hesapla panelinde
    // incelenir. Kullanıcı Kaydet / PDF oluştur dediğinde kayıt atomik yazılır.
    private DutyRecord? _pendingDuty;
    private PersonnelRecord? _pendingPerson;

    private void CalculateToResults()
    {
        var duty = Duty();
        if (_formPerson is null)
            throw new InvalidOperationException("Önce hesaplama tarifesini seçin.");

        if (duty.Kind == DraftKind.PermanentDuty)
            SetPermanentCalculationDefaults(duty);
        else if (duty.QuickTemporaryEnabled)
            SetQuickTemporaryDefaults(duty);

        var person = Copy(_formPerson);
        var candidate = Copy(duty);
        CheckFormIssues(DutyIssues(candidate, person, includeExpenses: false));
        var service = Service;
        var preview = service.PreviewForCalculation(person, candidate);
        if (preview.Amount is null)
        {
            var field = candidate.Kind == DraftKind.PermanentDuty ? "Destination"
                : candidate.QuickTemporaryEnabled ? "QuickWorkDays" : "ReviewedDays";
            CheckFormIssues([(field, preview.Explanation)]);
        }

        // Önizleme ile daha sonra kaydedilecek giderler birebir aynı olur.
        if (candidate.Kind == DraftKind.TemporaryDuty && candidate.QuickTemporaryEnabled)
            SyncQuickTemporaryExpenses(candidate, preview);
        else
            SyncCalculatedExpense(candidate, preview);
        if (candidate.Kind == DraftKind.PermanentDuty)
            SyncPermanentQuickExpenses(candidate, preview);

        candidate.Snapshot = service.PreviewForCalculation(person, candidate);
        if (!candidate.QuickTemporaryEnabled)
            CheckFormIssues(CalculatedExpenseIssues(candidate, candidate.Snapshot));
        CheckFormIssues(ExpenseIssues(candidate));
        if (candidate.Advance < 0 ||
            candidate.Advance > candidate.Expenses.Sum(x => x.Amount))
            CheckFormIssues([("Advance", "Avans, hesaplanan gider toplamını aşamaz.")]);

        _pendingDuty = candidate;
        _pendingPerson = person;
        Navigation.SelectedIndex = 2;
        Status.Text = "Hesaplandı. Hesapla panelinden tutarları inceleyin; isterseniz Kaydet / PDF oluştur seçin.";
    }

    private void EditPendingCalculation()
    {
        _pendingDuty = null;
        _pendingPerson = null;
        Navigation.SelectedIndex = 1;
        Status.Text = "Hesap girdilerine döndünüz. Değişiklikler henüz kaydedilmedi.";
    }

    private void DiscardPendingCalculation()
    {
        if (_dialog("Kaydedilmemiş hesaplama silinsin mi?", "Hesaplamayı temizle",
            System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes)
            return;
        _pendingDuty = null;
        _pendingPerson = null;
        ClearEntry();
        Navigation.SelectedIndex = 1;
        ShowPage();
        Status.Text = "Kaydedilmemiş hesap temizlendi.";
    }

    private void SavePendingAndMakePdf()
    {
        if (_pendingDuty is null || _pendingPerson is null || _form is null)
            throw new InvalidOperationException("Kaydedilecek güncel hesap yok.");
        // Önizleme verileri, kullanıcı sonuç ekranındayken değiştirilemez.
        // Kayıt işlemi orijinal formdan yeniden hesaplar ve sayısal doğrulamayı tekrar yapar.
        SaveEntryCore(notify: false);
        var saved = _vm.SelectedDuty ??
            throw new InvalidOperationException("Kayıt tamamlandı fakat görev seçilemedi.");
        MakePdf(saved);
    }
}
