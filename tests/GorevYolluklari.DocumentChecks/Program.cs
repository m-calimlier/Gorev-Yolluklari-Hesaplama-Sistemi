using GorevYolluklari.Core.Documents;
using System.Diagnostics.CodeAnalysis;

var checks = new List<(string Name, Action Action)>();
void Test(string name, Action action) => checks.Add((name, action));
void Require([DoesNotReturnIf(false)] bool yes, string? message=null)
{
    if (!yes) throw new Exception(message ?? "Doğrulama başarısız.");
}

DraftExpenseLine Daily(decimal amount=850m) => new("GUN-1", DraftLineKind.DailyAllowance,
    "Onaylı gündelik günü", amount, "OLUR-2026-1", "6245 m.33/39");
DraftExpenseLine Travel(decimal amount=275.50m) => new("YOL-1", DraftLineKind.TicketOrTravel,
    "Otobüs bileti", amount, "BILET-2026-1", "6245 m.27");
DraftExpenseLine Hotel(decimal amount=450m) => new("OTEL-1", DraftLineKind.Accommodation,
    "Belgeli konaklama", amount, "FATURA-2026-1", "6245 m.33/d");
DraftExpenseLine Relocation(decimal amount=1700m) => new("TAS-1", DraftLineKind.Relocation,
    "Yer değiştirme kontrol tutarı", amount, "ATAMA-2026-1", "6245 m.45");
DraftExpenseLine Family(decimal amount=200m) => new("AILE-1", DraftLineKind.FamilyTravel,
    "Aile yol kontrol tutarı", amount, "AILE-2026-1", "6245 m.44");
DraftDocumentRequest Temporary() => new(
    DraftKind.TemporaryDuty,"GEC-2026-001", "ÖRNEK PERSONEL", "Öğretmen", "ÖRNEK KURUM",
    "Ankara", "Konya", new DateOnly(2026,6,15),new DateOnly(2026,6,15),
    2026,"BUT-002:2026:I-B","KURAL-7-ONHESAP", "OLUR-2026-1", "MAHAL-2026-1",
    true,true, new[] { Daily(), Travel(), Hotel() },75.50m,"AVANS-2026-1",1575.50m,1500m);
DraftDocumentRequest Permanent() => Temporary() with {
    Kind = DraftKind.PermanentDuty, DocumentNumber="SUR-2026-001", Lines = new[] { Daily(), Travel(), Relocation(), Family() },
    AdvanceTry=0m, AdvanceEvidence=null, IndependentGrossTry=3025.50m, IndependentNetTry=3025.50m
};
void Accepted(DraftDocumentRequest request)
{
    var r = DraftDocumentService.Create(request);
    Require(r.Status==DraftDecisionStatus.DraftReady && r.MayPrintDraft && r.Document is not null, r.Reason);
    Require(!r.IsOfficialPaymentDocument && !r.Document.IsOfficialPaymentDocument);
}
void Blocked(DraftDocumentRequest request, DraftDecisionStatus? status=null)
{
    var r = DraftDocumentService.Create(request);
    Require(!r.MayPrintDraft && r.Document is null, r.Reason);
    if (status is not null) Require(r.Status == status, $"Beklenen: {status}, alınan: {r.Status}");
}

Test("Hesaplama PDF'si ikinci toplam veya inceleme beyanı istemez", ()=>{
    var req = Temporary() with
    {
        AssignmentAndStationReviewed = false, EntitlementAndAmountsReviewed = false,
        AssignmentEvidence = "", StationEvidence = "", IndependentGrossTry = 0m,
        IndependentNetTry = 0m, AdvanceTry = 0m, AdvanceEvidence = "",
        Lines = new[] { Daily(850m) with { EvidenceReference = "" } }
    };
    var calculated = DraftDocumentService.CreateCalculationCopy(req);
    Require(calculated.MayPrintDraft && calculated.Document is { IsCalculationOutput: true }, calculated.Reason);
    Require(!calculated.Document!.IsOfficialPaymentDocument &&
        calculated.Document.GrossTry == 850m && calculated.Document.NetTry == 850m);
});
Test("Hesaplama çıktısında unvan ve kurum isteğe bağlıdır", ()=>{
    var req = Temporary() with {
        PersonnelTitle = "", Institution = "",
        AssignmentAndStationReviewed = false, EntitlementAndAmountsReviewed = false,
        IndependentGrossTry = 0m, IndependentNetTry = 0m
    };
    Require(DraftDocumentService.CreateCalculationCopy(req).MayPrintDraft);
    Require(!DraftDocumentService.Create(req).MayPrintDraft);
});
Test("Hesaplama PDF'si negatif veya kuruş altı gideri kabul etmez", ()=>{
    var req=Temporary() with { Lines=new[]{Daily(-1m)}, IndependentGrossTry=0m, IndependentNetTry=0m };
    Require(!DraftDocumentService.CreateCalculationCopy(req).MayPrintDraft);
    req=req with{ Lines=new[]{Daily(850.001m)} };
    Require(!DraftDocumentService.CreateCalculationCopy(req).MayPrintDraft);
});
Test("Hesaplama PDF'si avans brütten fazla olursa reddeder", ()=>{
    var req=Temporary() with { Lines=new[]{Daily(850m)}, AdvanceTry=851m,
        IndependentGrossTry=0m, IndependentNetTry=0m };
    Require(!DraftDocumentService.CreateCalculationCopy(req).MayPrintDraft);
});

Test("Geçici görev taslak kabul", ()=>Accepted(Temporary()));
Test("Sürekli görev taslak kabul", ()=>Accepted(Permanent()));
Test("Brüt 1.575,50", ()=>Require(DraftDocumentService.Create(Temporary()).Document?.GrossTry==1575.50m));
Test("Avans 75,50", ()=>Require(DraftDocumentService.Create(Temporary()).Document?.AdvanceTry==75.50m));
Test("Net 1.500,00", ()=>Require(DraftDocumentService.Create(Temporary()).Document?.NetTry==1500m));
Test("Örnek 27 etiket", ()=>Require(DraftDocumentService.Create(Temporary()).Document?.RequiredFormCode=="Örnek 27"));
Test("Örnek 28 etiket", ()=>Require(DraftDocumentService.Create(Permanent()).Document?.RequiredFormCode=="Örnek 28"));
Test("Ödeme belgesi her zaman false", ()=>Require(!DraftDocumentService.Create(Permanent()).Document!.IsOfficialPaymentDocument));
Test("Taslak uyarısı", ()=>Require(DraftDocumentService.Create(Temporary()).Document!.VisibilityNotice.Contains("ÖDEME BELGESİ DEĞİLDİR")));
Test("TR para gösterimi", ()=>Require(DraftDocumentService.Money(1234.56m)=="1.234,56 TL"));
Test("Taslak satırı kopyalanır", ()=>{
    var rows=new List<DraftExpenseLine>{ Daily() };
    var r=Temporary() with { Lines=rows, AdvanceTry=0m, AdvanceEvidence=null, IndependentGrossTry=850m, IndependentNetTry=850m };
    var draft=DraftDocumentService.Create(r).Document!;
    rows.Clear(); Require(draft.Lines.Count==1);
});
Test("Şablon sayfa belge türü ayrımı", ()=>{
    Require(DraftDocumentService.Create(Temporary()).Document!.Kind!=DraftDocumentService.Create(Permanent()).Document!.Kind);
});
Test("Boş belge numarası engeli", ()=>Blocked(Temporary() with { DocumentNumber="" }));
Test("Dosya yolu sızdıran belge numarası engeli", ()=>Blocked(Temporary() with { DocumentNumber="../../a" }));
Test("Çok kısa belge numarası engeli", ()=>Blocked(Temporary() with { DocumentNumber="A" }));
Test("41 karakter belge numarası engeli", ()=>Blocked(Temporary() with { DocumentNumber=new string('X',41) }));
Test("Geçersiz belge türü engeli", ()=>Blocked(Temporary() with { Kind=(DraftKind)999 }));
Test("Personel adı boş engeli", ()=>Blocked(Temporary() with { PersonnelFullName="" }));
Test("Personel unvanı boş engeli", ()=>Blocked(Temporary() with { PersonnelTitle=" " }));
Test("Kurum boş engeli", ()=>Blocked(Temporary() with { Institution="" }));
Test("Çıkış yeri boş engeli", ()=>Blocked(Temporary() with { Origin="" }));
Test("Varış yeri boş engeli", ()=>Blocked(Temporary() with { Destination="" }));
Test("Kontrol karakterleri engeli", ()=>Blocked(Temporary() with { Institution="KURUM\nDENEME" }));
Test("2025 tarife engeli", ()=>Blocked(Temporary() with { BudgetYear=2025 }));
Test("2027 tarife engeli", ()=>Blocked(Temporary() with { BudgetYear=2027 }));
Test("2025 görev başlangıç engeli", ()=>Blocked(Temporary() with { Begins=new DateOnly(2025,12,31) }));
Test("2027 görev bitiş engeli", ()=>Blocked(Temporary() with { Ends=new DateOnly(2027,1,1) }));
Test("Ters tarih aralığı engeli", ()=>Blocked(Temporary() with { Ends=new DateOnly(2026,6,14) }));
Test("Kaynak kodu yanlış engeli", ()=>Blocked(Temporary() with { TariffReference="BUT-001" }));
Test("Kural sürümü eksik engeli", ()=>Blocked(Temporary() with { RuleVersion="" }));
Test("Olur kaynağı boş engeli", ()=>Blocked(Temporary() with { AssignmentEvidence="" }));
Test("Mahallî kanıt boş engeli", ()=>Blocked(Temporary() with { StationEvidence="" }));
Test("Olur/mahal incelemesi yok engeli", ()=>Blocked(Temporary() with { AssignmentAndStationReviewed=false }));
Test("Hak sahipliği incelemesi yok engeli", ()=>Blocked(Temporary() with { EntitlementAndAmountsReviewed=false }));
Test("Satır listesi boş engeli", ()=>Blocked(Temporary() with { Lines=Array.Empty<DraftExpenseLine>() }));
Test("Satır listesi null engeli", ()=>Blocked(Temporary() with { Lines=null! }));
Test("41 satır engeli", ()=>Blocked(Temporary() with { Lines=Enumerable.Range(1,41).Select(i=>Daily() with{LineId="L-"+i}).ToArray() }));
Test("Tekrarlı satır kimliği engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily(), Daily() } }));
Test("Satır kimliği boş engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily() with{LineId=""} } }));
Test("Açıklama boş engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily() with{Description=""} } }));
Test("Belge kanıtı boş engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily() with{EvidenceReference=""} } }));
Test("Mevzuat dayanağı boş engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily() with{LegalReference=""} } }));
Test("0 TL satır engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily(0m) } }));
Test("Negatif satır engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily(-5m) } }));
Test("Kuruş altında kesir engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily(850.001m) } }));
Test("Sınırı aşan tutar engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily(100_000_001m) } }));
Test("Geçici forma taşınma satırı eklenemez", ()=>Blocked(Temporary() with { Lines=new[]{ Daily(), Relocation() } }));
Test("Sürekli forma konaklama satırı eklenemez", ()=>Blocked(Permanent() with { Lines=new[]{ Daily(), Hotel() } }));
Test("Geçersiz gider türü engeli", ()=>Blocked(Temporary() with { Lines=new[]{ Daily() with{Kind=(DraftLineKind)999} } }));
Test("Negatif avans engeli", ()=>Blocked(Temporary() with { AdvanceTry=-1m }));
Test("Avans brütten büyük engeli", ()=>Blocked(Temporary() with { AdvanceTry=2000m }));
Test("Avans kanıtı eksik engeli", ()=>Blocked(Temporary() with { AdvanceEvidence="" }));
Test("Avans kesirli kuruş engeli", ()=>Blocked(Temporary() with { AdvanceTry=.001m }));
Test("Bağımsız brüt uyuşmazlık", ()=>Blocked(Temporary() with { IndependentGrossTry=1575m }, DraftDecisionStatus.Inconsistent));
Test("Bağımsız net uyuşmazlık", ()=>Blocked(Temporary() with { IndependentNetTry=1499m }, DraftDecisionStatus.Inconsistent));
Test("Brüt kontrol kesirli kuruş", ()=>Blocked(Temporary() with { IndependentGrossTry=1575.501m }));
Test("Net kontrol kesirli kuruş", ()=>Blocked(Temporary() with { IndependentNetTry=1500.001m }));
Test("Avanssız geçici taslak", ()=>Accepted(Temporary() with { AdvanceTry=0m, AdvanceEvidence=null, IndependentNetTry=1575.50m }));
Test("Tek satırlı geçici taslak", ()=>Accepted(Temporary() with { Lines=new[]{Daily()}, AdvanceTry=0m,AdvanceEvidence=null, IndependentGrossTry=850m,IndependentNetTry=850m }));
Test("Tek satırlı sürekli taslak", ()=>Accepted(Permanent() with { Lines=new[]{Relocation()}, AdvanceTry=0m,AdvanceEvidence=null, IndependentGrossTry=1700m, IndependentNetTry=1700m }));
Test("İcmal 2 kişi toplanır", ()=>{
    var one=DraftDocumentService.Create(Temporary()).Document!;
    var two=DraftDocumentService.Create(Permanent()).Document!;
    var s=DraftDocumentService.CreateSummary(new[]{ one,two });
    Require(s.Ready && s.Summary is not null && s.Summary.GrossTry==4601m &&
        s.Summary.AdvanceTry==75.50m && s.Summary.NetTry==4525.50m);
});
Test("İcmal resmî değildir", ()=>Require(!DraftDocumentService.CreateSummary(new[]{ DraftDocumentService.Create(Temporary()).Document! }).Summary!.IsOfficialPaymentDocument));
Test("İcmal boş engeli", ()=>Require(!DraftDocumentService.CreateSummary(Array.Empty<DraftDocument>()).Ready));
Test("İcmal null engeli", ()=>Require(!DraftDocumentService.CreateSummary(null!).Ready));
Test("İcmal tekrarlı belge engeli", ()=>{
    var one=DraftDocumentService.Create(Temporary()).Document!;
    Require(!DraftDocumentService.CreateSummary(new[]{one,one}).Ready);
});
Test("İcmal hatalı net engeli", ()=>{
    var one=DraftDocumentService.Create(Temporary()).Document! with { NetTry=999m };
    Require(!DraftDocumentService.CreateSummary(new[]{one}).Ready);
});
Test("İcmal hatalı brüt engeli", ()=>{
    var one=DraftDocumentService.Create(Temporary()).Document! with { GrossTry=999m, NetTry=923.50m };
    Require(!DraftDocumentService.CreateSummary(new[]{one}).Ready);
});
Test("İcmalde kayıt listesi kopyalanır", ()=>{
    var input=new List<DraftDocument>{ DraftDocumentService.Create(Temporary()).Document! };
    var summary=DraftDocumentService.CreateSummary(input).Summary!;
    input.Clear(); Require(summary.Documents.Count==1);
});
Test("Fazla icmal kaydı engeli", ()=>{
    var one=DraftDocumentService.Create(Temporary()).Document!;
    var items=Enumerable.Range(1,501).Select(i=>one with{DocumentNumber="GEC-"+i}).ToArray();
    Require(!DraftDocumentService.CreateSummary(items).Ready);
});
Test("Draft istek null engeli", ()=>{
    bool threw=false;
    try { DraftDocumentService.Create(null!); } catch(ArgumentNullException) { threw=true; }
    Require(threw);
});
int passed=0;
foreach (var (name, run) in checks)
{
    try { run(); passed++; Console.WriteLine("[OK] " + name); }
    catch(Exception ex) { Console.Error.WriteLine("[HATA] " + name + ": " + ex.Message); }
}
Console.WriteLine($"TOPLAM: {passed}/{checks.Count} BELGE-MODEL TESTİ BAŞARILI");
Console.WriteLine("UYARI: Bu taslaklar Örnek 27/28'in resmî onaylı karşılığı ve ödeme bordrosu değildir.");
return passed==checks.Count ? 0 : 1;
