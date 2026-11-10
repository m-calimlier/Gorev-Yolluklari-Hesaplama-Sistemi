using GorevYolluklari.Core.Tariffs;
using System.Text.Json;

if (args.Length != 1 || !File.Exists(args[0]))
    throw new ArgumentException("Doğrulanmış H Cetveli JSON dosyasının tam yolu zorunlu.");
var tariff = DailyRateTariff2026.FromVerifiedJson(args[0]);
int passed = 0;
void Check(string name, bool ok)
{
    if (!ok) throw new Exception("TEST BAŞARISIZ: " + name);
    Console.WriteLine("[OK] " + name);
    passed++;
}
DailyRateDecision R(int year, PersonnelTariffGroup group, int? grade, int? indicator) =>
    tariff.Resolve(new PersonnelTariffInput(new DateOnly(year, 6, 15), group, grade, indicator));
void Valid(string name, int? grade, int? indicator, string key, decimal amount)
{
    var r = R(2026, PersonnelTariffGroup.GeneralCivilServant, grade, indicator);
    Check(name, r.Status == RateDecisionStatus.ResolvedPreliminary && r.Category == key
          && r.DailyAmount == amount && r.SourceReference.Contains("BUT-002"));
}
Valid("Eşik 8000", 5, 8000, "B_a", 890m);
Valid("Eşik 7999", 5, 7999, "B_b", 880m);
Valid("Eşik 6400", 5, 6400, "B_b", 880m);
Valid("Eşik 6399", 5, 6399, "B_c", 870m);
Valid("Eşik 3600", 5, 3600, "B_c", 870m);
Valid("Eşik 3599, derece 1", 1, 3599, "B_d", 860m);
Valid("Eşik 3599, derece 5", 5, 3599, "B_e", 850m);
Valid("Ek göstergesiz derece 4", 4, 0, "B_d", 860m);
Valid("Ek göstergesiz derece 5", 5, 0, "B_e", 850m);
Valid("Ek göstergeli derece 15", 15, 8000, "B_a", 890m);
Check("Eksik derece blok", R(2026,PersonnelTariffGroup.GeneralCivilServant,null,3600).Status == RateDecisionStatus.NeedsInput);
Check("Eksik ek gösterge blok", R(2026,PersonnelTariffGroup.GeneralCivilServant,1,null).Status == RateDecisionStatus.NeedsInput);
Check("Derece 0 blok", R(2026,PersonnelTariffGroup.GeneralCivilServant,0,0).Status == RateDecisionStatus.NeedsInput);
Check("Derece 16 blok", R(2026,PersonnelTariffGroup.GeneralCivilServant,16,0).Status == RateDecisionStatus.NeedsInput);
Check("Negatif gösterge blok", R(2026,PersonnelTariffGroup.GeneralCivilServant,4,-1).Status == RateDecisionStatus.NeedsInput);
Check("Özel statü blok", R(2026,PersonnelTariffGroup.SpecialOrHighOffice,1,8000).Status == RateDecisionStatus.NeedsReview);
Check("Bilinmeyen statü blok", R(2026,PersonnelTariffGroup.Unknown,1,8000).Status == RateDecisionStatus.NeedsReview);
Check("2025 tarife yok", R(2025,PersonnelTariffGroup.GeneralCivilServant,4,0).Status == RateDecisionStatus.NoApplicableYear);
Check("2027 tarife yok", R(2027,PersonnelTariffGroup.GeneralCivilServant,4,0).Status == RateDecisionStatus.NoApplicableYear);
var end = tariff.Resolve(new(new DateOnly(2026,12,31),PersonnelTariffGroup.GeneralCivilServant,4,0));
Check("2026 son günü",end.Status == RateDecisionStatus.ResolvedPreliminary && end.DailyAmount == 860m);
var start = tariff.Resolve(new(new DateOnly(2026,1,1),PersonnelTariffGroup.GeneralCivilServant,5,0));
Check("2026 ilk günü",start.Status == RateDecisionStatus.ResolvedPreliminary && start.DailyAmount == 850m);
var text = File.ReadAllText(args[0]);
var tampered = text.Replace("\"tutar\": 890", "\"tutar\": 891");
Check("Tahrifat testi hazırlığı",tampered != text);
var tempFile = Path.Combine(Path.GetTempPath(), "h2026-test-" + Guid.NewGuid().ToString("N") + ".json");
try
{
    File.WriteAllText(tempFile, tampered);
    bool failed = false;
    try { _ = DailyRateTariff2026.FromVerifiedJson(tempFile); }
    catch (InvalidDataException) { failed = true; }
    Check("Yanlış tarife tutarı blok", failed);
}
finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
Console.WriteLine($"TOPLAM: {passed}/{passed} KONTROL BAŞARILI");
Console.WriteLine("UYARI: Tarife seçimi yalnız I-B ön hesap. Hak sahipliği/istisna ve resmî bordro doğrulanmadı.");
