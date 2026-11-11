using GorevYolluklari.Core.Integration;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
int passed=0;
void Check(string name,bool ok) { if(!ok) throw new Exception(name); passed++; Console.WriteLine("[OK] "+name); }
var temporary=typeof(TemporaryDutyEngine).Assembly; var permanent=typeof(PermanentDutyEngine).Assembly;
Check("Iki ayri cekirdek assembly",temporary!=permanent);
Check("Gecici cekirdek bagimsiz",temporary.GetReferencedAssemblies().All(x=>x.Name!="GorevYolluklari.Core" && x.Name!=permanent.GetName().Name));
Check("Surekli cekirdek bagimsiz",permanent.GetReferencedAssemblies().All(x=>x.Name!="GorevYolluklari.Core" && x.Name!=temporary.GetName().Name));
Check("Ayni gun kurali gecici cekirdekte",typeof(SameDayPerDiemPreview).Assembly==temporary);
Check("Cok gun kurali gecici cekirdekte",typeof(MultiDayDutyPreviewEngine2026).Assembly==temporary);
Check("Seyahat kurali gecici cekirdekte",typeof(TravelTimelinePreviewEngine2026).Assembly==temporary);
Check("Yer degistirme kurali surekli cekirdekte",typeof(RelocationExpensePreview).Assembly==permanent);
var tariff=DailyRateTariff2026.FromVerifiedJson(Path.Combine(args[0],"Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli_Dogrulanmis_OnHesap.json"));
var date=new DateOnly(2026,1,15); var person=new PersonnelTariffInput(date,PersonnelTariffGroup.GeneralCivilServant,4,6400);
var request=new SameDayDutyRequest(person,date.ToDateTime(new TimeOnly(8,0)),date.ToDateTime(new TimeOnly(20,0)),DutyStationAssessment.Outside,true,"SENTETIK");
Check("Gecici uyumluluk",new TemporaryDutyEngine(tariff).PreviewSameDay(request)==new HarcirahPreviewEngine(tariff).PreviewSameDay(request));
var relocation=new RelocationDutyRequest(person,DutyStationAssessment.Outside,true,true,false,5,765m,"SENTETIK");
Check("Surekli uyumluluk",new PermanentDutyEngine(tariff).PreviewRelocation(relocation)==new HarcirahPreviewEngine(tariff).PreviewRelocation(relocation));
// IUC 2026 guide, PDF page 3: rate 880, invoice 1500, first-period limit 1408.
// Date, grade/indicator and review references below are synthetic adapter context, not source facts.
var lodging=new MultiDayDutyPreviewEngine2026(tariff).Preview(new(person,DutyStationAssessment.Outside,true,true,true,true,true,true,0,new[]{new ReviewedDutyDay(date,1,1500m,"SENTETIK-FATURA")},"SENTETIK-OLUR","SENTETIK-MAHAL","SENTETIK-DEFTER"));
Check("IUC kaynakli gundelik 880",lodging.DailyRate==880m);
Check("IUC kaynakli konaklama 1408",lodging.LodgingTotalUnrounded==1408m);
Console.WriteLine("ORNEK IUC: beklenen konaklama 1408,00; gercek "+lodging.LodgingTotalUnrounded);
// KTU PDF pages 24-25 (zero-based 23-24), historical 31 TL rate.
// Only relocation: 620 + 1185.75 + 1240 = 3045.75. Full 3651.75 is outside this engine's scope.
var historic=RelocationExpensePreview.Calculate(31m,5,765m,DutyStationAssessment.Outside,true,true,false,"KTU-TARIHSEL","KTU-ORNEK1");
Check("KTU yer degistirme bileseni 3045.75",historic.UnroundedAmount==3045.75m);
Check("Ornek resmi belge degil",!lodging.IsOfficialPaymentDocument);
Check("Eski yil surekli tarifede bloke",new PermanentDutyEngine(tariff).PreviewRelocation(relocation with {Personnel=person with {DutyDate=new DateOnly(2017,1,15)}}).Status==IntegratedPreviewStatus.NoApplicableYear);
Check("Eski yil gecici tarifede bloke",new TemporaryDutyEngine(tariff).PreviewSameDay(request with {Personnel=person with {DutyDate=new DateOnly(2017,1,15)},Departure=new DateTime(2017,1,15,8,0,0),Return=new DateTime(2017,1,15,20,0,0)}).Status==IntegratedPreviewStatus.NoApplicableYear);
Console.WriteLine("ORNEK KTU: beklenen yer degistirme 3045,75; gercek "+historic.UnroundedAmount);
Console.WriteLine($"TOPLAM: {passed}/{passed} MODUL VE KAYNAK ORNEK KONTROLU BASARILI");
