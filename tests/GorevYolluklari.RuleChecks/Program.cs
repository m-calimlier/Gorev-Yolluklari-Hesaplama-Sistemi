using GorevYolluklari.Core.Rules;

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
Check("Eşin aynı güzergâhta memur nakli manuel inceleme", i.Status == DraftStatus.NeedsReview);
var j = RelocationExpensePreview.Calculate(1000m, 0, 100m, DutyStationAssessment.Outside, false, true, false, tariff, "KGM-TEST-001");
Check("Hak sahipliği incelenmeden taşınma hesabı yok", j.Status == DraftStatus.NeedsReview);
var caught = false;
try { RelocationExpensePreview.Calculate(1000m, 0, -1m, DutyStationAssessment.Outside, true, true, false, tariff, evidence); }
catch (ArgumentOutOfRangeException) { caught = true; }
Check("Negatif kilometre engellenir", caught);
Console.WriteLine($"TOPLAM: {passed} / {passed} KONTROL BAŞARILI");
Console.WriteLine("UYARI: Bu sonuçlar test amaçlı ön hesaptır, resmî ödeme bordrosu değildir.");
