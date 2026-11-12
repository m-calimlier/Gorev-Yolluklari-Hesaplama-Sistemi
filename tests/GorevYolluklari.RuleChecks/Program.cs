using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Records;

int passed = 0;
void Check(string label, bool condition)
{
    if (!condition) throw new Exception("TEST BAŞARISIZ: " + label);
    Console.WriteLine("[OK] " + label);
    passed++;
}

const decimal rate = 900m; // YALNIZ TEST VERİSİ, RESMÎ TARİFE DEĞİLDİR.
const string tariff = "TEST-2026-ORNEK";
const string evidence = "TEST-ONAY-001";
var t = new DateTime(2026, 11, 9);
var a = SameDayPerDiemPreview.Calculate(t.AddHours(9), t.AddHours(15), rate, DutyStationAssessment.Outside, tariff, evidence);
Check("39. madde - 1/3 ön hesap", a.Status == DraftStatus.Preliminary && a.UnroundedAmount == 300m);
var b = SameDayPerDiemPreview.Calculate(t.AddHours(9), t.AddHours(20), rate, DutyStationAssessment.Outside, tariff, evidence);
Check("39. madde - 2/3 ön hesap", b.Status == DraftStatus.Preliminary && b.UnroundedAmount == 600m);
var c = SameDayPerDiemPreview.Calculate(t.AddHours(9), t.AddHours(20), rate, DutyStationAssessment.Unknown, tariff, evidence);
Check("Memuriyet mahalli belirsizse hesap blokajı", c.Status == DraftStatus.NeedsReview && c.UnroundedAmount == null);
var d = SameDayPerDiemPreview.Calculate(t.AddHours(9), t.AddHours(20), rate, DutyStationAssessment.Inside, tariff, evidence);
Check("Memuriyet mahalli içi gündelik yok", d.Status == DraftStatus.NotEligibleByLocation && d.UnroundedAmount == 0m);
var e = SameDayPerDiemPreview.Calculate(t.AddHours(13), t.AddHours(15), rate, DutyStationAssessment.Outside, tariff, evidence);
Check("13:00 eşitliği incelemeye gider", e.Status == DraftStatus.NeedsReview);
var f = SameDayPerDiemPreview.Calculate(t.AddHours(9), t.AddDays(1), rate, DutyStationAssessment.Outside, tariff, evidence);
Check("Geceleme özel kurala yönlendirilir", f.Status == DraftStatus.NeedsReview);
var g = RelocationExpensePreview.Calculate(1000m, 2, 100m, DutyStationAssessment.Outside, true, true, false, tariff, "KGM-TEST-001");
Check("45. madde - 20x+20x+%5 x km", g.Status == DraftStatus.Preliminary && g.UnroundedAmount == 45000m);
var h = RelocationExpensePreview.Calculate(1000m, 7, 100m, DutyStationAssessment.Outside, true, true, false, tariff, "KGM-TEST-001");
Check("45. madde - aile bileşeninde 40 gündelik tavan", h.UnroundedAmount == 65000m);
var i = RelocationExpensePreview.Calculate(1000m, 2, 100m, DutyStationAssessment.Outside, true, true, true, tariff, "KGM-TEST-001");
Check("Aynı güzergâhta ikinci memur eş: yalnız mesafe unsuru yarım", 
    i.Status == DraftStatus.Preliminary && i.UnroundedAmount == 42500m &&
    i.Reason.Contains("%50"));
var j = RelocationExpensePreview.Calculate(1000m, 0, 100m, DutyStationAssessment.Outside, false, true, false, tariff, "KGM-TEST-001");
Check("Hak sahipliği incelenmeden taşınma hesabı yok", j.Status == DraftStatus.NeedsReview);
var caught = false;
try { RelocationExpensePreview.Calculate(1000m, 0, -1m, DutyStationAssessment.Outside, true, true, false, tariff, evidence); }
catch (ArgumentOutOfRangeException) { caught = true; }
Check("Negatif kilometre engellenir", caught);
// Yalnız yüklenen anonimleştirilmiş Örnek 27/28 rakamlarının aritmetik karşılaştırması.
var surekli1 = YollukOdemeOzeti2026.Hesapla(new[] { 860m, 1020m, 17200m, 4472m }
    .Select(x => new ExpenseEntry { Amount = x }));
Check("Örnek 28 / 1: brüt 23.552,00", surekli1.Brut == 23552m);
Check("Örnek 28 / 1: damga 178,76; net 23.373,24",
    surekli1.DamgaVergisi == 178.76m && surekli1.Net == 23373.24m);
var surekli2 = YollukOdemeOzeti2026.Hesapla(new[] { 860m, 860m, 250m, 250m, 17200m, 8600m, 2795m }
    .Select(x => new ExpenseEntry { Amount = x }));
Check("Örnek 28 / 2: brüt 30.815,00", surekli2.Brut == 30815m);
Check("Örnek 28 / 2: damga 233,89; net 30.581,11",
    surekli2.DamgaVergisi == 233.89m && surekli2.Net == 30581.11m);
var gecici1 = YollukOdemeOzeti2026.Hesapla(Enumerable.Repeat(200m, 5)
    .Select(x => new ExpenseEntry { Amount = x }));
Check("Örnek 27 / 1: yalnız beş taşıt ücreti 1.000,00", gecici1.Brut == 1000m);
var gecici2 = YollukOdemeOzeti2026.Hesapla(new[] { 286.67m, 470m, 470m }
    .Select(x => new ExpenseEntry { Amount = x }));
Check("Örnek 27 / 2: brüt 1.226,67", gecici2.Brut == 1226.67m);
var gecici3 = YollukOdemeOzeti2026.Hesapla(new[] { 286.67m, 1000m }
    .Select(x => new ExpenseEntry { Amount = x }));
Check("Örnek 27 / 3: brüt 1.286,67", gecici3.Brut == 1286.67m);
var avansli = YollukOdemeOzeti2026.Hesapla(new[] { new ExpenseEntry { Amount = 1000m } }, 100m);
Check("2026 damga ve avans ayrı düşülür", avansli.DamgaVergisi == 7.59m && avansli.Net == 892.41m);
var rejected = false;
try { _ = YollukOdemeOzeti2026.Hesapla(new[] { new ExpenseEntry { Amount = -1m } }); }
catch (ArgumentException) { rejected = true; }
Check("Negatif giderle ödeme özeti üretilemez", rejected);

Console.WriteLine($"TOPLAM: {passed} / {passed} KONTROL BAŞARILI");
Console.WriteLine("UYARI: Bu sonuçlar test amaçlı ön hesaptır, resmî ödeme bordrosu değildir.");
