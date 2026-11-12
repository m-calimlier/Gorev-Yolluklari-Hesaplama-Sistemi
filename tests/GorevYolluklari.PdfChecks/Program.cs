using System.IO;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Documents;
using PdfSharp.Pdf.IO;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static DraftDocument Create(string number, DraftKind kind=DraftKind.TemporaryDuty, int lineCount=1)
{
    var lines=Enumerable.Range(1,lineCount).Select(i=>new DraftExpenseLine("L"+i,
        DraftLineKind.DailyAllowance, "Örnek doğrulanmış gündelik",850m, "OLUR-001", "6245 m.33")).ToArray();
    var amount=850m*lineCount;
    var req=new DraftDocumentRequest(kind,number,"ÖRNEK PERSONEL","Öğretmen","ÖRNEK OKUL",
        "Ankara","Konya",new DateOnly(2026,1,5),new DateOnly(2026,1,6),
        2026,"BUT-002:2026:I-B","KURAL-7-TASLAK","OLUR-001","MAHAL-001",
        true,true,lines,0m,null,amount,amount);
    var decision=DraftDocumentService.Create(req);
    Check(decision.MayPrintDraft && decision.Document is not null,"Taslak veri modeli reddetti: " + decision.Reason);
    return decision.Document ?? throw new InvalidOperationException("Taslak belge olusturulamadi.");
}
static void PdfCheck(string path, int pages)
{
    Check(File.Exists(path), "PDF yok: " + path);
    var bytes=File.ReadAllBytes(path);
    Check(bytes.Length>1500 && bytes[0]=='%' && bytes[1]=='P' && bytes[2]=='D' && bytes[3]=='F', "PDF imzası veya boyut hatalı.");
    using var doc=PdfReader.Open(path, PdfDocumentOpenMode.Import);
    Check(doc.PageCount==pages, $"Sayfa sayısı: {doc.PageCount}, beklenen: {pages}");
    Check(doc.Pages[0].Width.Point>doc.Pages[0].Height.Point, "Yatay A4 değil.");
}
var dir=Path.Combine(Path.GetTempPath(), "GorevYolluklari_Asama7_"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(dir);
int passed=0;
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("[OK] "+name); }
    catch(Exception ex) { Console.Error.WriteLine("[HATA] "+name+": "+ex.Message); throw; }
}
try
{
    Test("Örnek 27 - taslak PDF oluşur",()=>{
        var path=Path.Combine(dir,"Ornek27_TASLAK.pdf");
        TravelDraftPdfWriter.SaveDraft(Create("GEC-2026-001"),path);
        PdfCheck(path,1);
    });
    Test("Örnek 28 - taslak PDF oluşur",()=>{
        var path=Path.Combine(dir,"Ornek28_TASLAK.pdf");
        TravelDraftPdfWriter.SaveDraft(Create("SUR-2026-001",DraftKind.PermanentDuty),path);
        PdfCheck(path,1);
    });
    Test("Doğrudan yolluk hesaplama PDF'si inceleme beyanı olmadan oluşur",()=>{
        var req=new DraftDocumentRequest(DraftKind.TemporaryDuty,"GEC-HES-001",
            "SENTETİK PERSONEL","Öğretmen","SENTETİK KURUM",
            "Adana/Aladağ","Adana/Ceyhan",new DateOnly(2026,6,15),new DateOnly(2026,6,15),
            2026,"BUT-002:2026:I-B","ASAMA8-2026-1",
            "","",false,false,
            new[]{new DraftExpenseLine("OTO-GD-001",DraftLineKind.DailyAllowance,"Gündelik",
                566.67m,"","6245 m.39")},0m,"",0m,0m);
        var decision=DraftDocumentService.CreateCalculationCopy(req);
        Check(decision.MayPrintDraft && decision.Document is { IsCalculationOutput: true }, decision.Reason);
        var path=Path.Combine(dir,"Gecici_Hesaplama_Ciktisi.pdf");
        TravelDraftPdfWriter.SaveDraft(decision.Document!,path);
        PdfCheck(path,1);
    });
    DraftDocument QuickForm()
    {
        var rows = new[]
        {
            new DraftExpenseLine("OTO-GQ-001", DraftLineKind.TicketOrTravel, "Gidiş ulaşım",120m,"","6245 m.27"),
            new DraftExpenseLine("OTO-RQ-001", DraftLineKind.TicketOrTravel, "Dönüş ulaşım",100m,"","6245 m.27"),
            new DraftExpenseLine("OTO-TQ-001", DraftLineKind.DailyAllowance, "Gündelik",3400m,"","6245 m.39, 42, 43"),
            new DraftExpenseLine("OTO-LQ-001", DraftLineKind.Accommodation, "Konaklama",1800m,"","6245 m.33(d)"),
            new DraftExpenseLine("OTO-OQ-001", DraftLineKind.OtherEligible, "Diğer",50m,"","6245 m.14")
        };
        var request = new DraftDocumentRequest(DraftKind.TemporaryDuty,"GEC-FORM-2026",
            "AD SOYAD BELİRTİLMEDİ","","","GÜZERGÂH BELİRTİLMEDİ",
            "GÜZERGÂH BELİRTİLMEDİ",new(2026,1,1),new(2026,1,1),2026,
            "BUT-002:2026:I-B","ASAMA8-2026-1","","",false,false,rows,0m,"",0m,0m,true);
        var decision=DraftDocumentService.CreateCalculationCopy(request);
        Check(decision.MayPrintDraft, "Kısa geçici PDF modeli: "+decision.Reason);
        return decision.Document! with
        {
            PrintDetails = new DraftPrintDetails(5,0,850m,0,0m,null,false,
                2,1,1,1,3,2,900m,new[]
                {
                    new DraftPrintComponent("Gidiş ulaşım",120m),
                    new DraftPrintComponent("Gidiş yol gündeliği",566.67m),
                    new DraftPrintComponent("Görev gündeliği",2550m),
                    new DraftPrintComponent("Dönüş ulaşım",100m),
                    new DraftPrintComponent("Dönüş yol gündeliği",283.33m),
                    new DraftPrintComponent("Konaklama",1800m),
                    new DraftPrintComponent("Diğer masraflar",50m)
                })
        };
    }
    DraftDocument PermanentForm()
    {
        var rows = new[]
        {
            new DraftExpenseLine("OTO-YD-001",DraftLineKind.Relocation,"Yer değiştirme gideri",29070m,"","6245 m.45"),
            new DraftExpenseLine("OTO-SG-001",DraftLineKind.DailyAllowance,"Seyahat gündeliği",850m,"","6245 m.43"),
            new DraftExpenseLine("OTO-AG-001",DraftLineKind.FamilyDaily,"Aile gündeliği",850m,"","6245 m.44"),
            new DraftExpenseLine("OTO-ST-001",DraftLineKind.TicketOrTravel,"Taşıt",250m,"","6245 m.27"),
            new DraftExpenseLine("OTO-AT-001",DraftLineKind.FamilyTravel,"Aile taşıt",250m,"","6245 m.44")
        };
        var request = new DraftDocumentRequest(DraftKind.PermanentDuty,"SUR-FORM-2026",
            "AD SOYAD BELİRTİLMEDİ","","","Adana/Aladağ","Adana/Ceyhan",
            new(2026,1,1),new(2026,1,1),2026,"BUT-002:2026:I-B",
            "ASAMA8-2026-1","","",false,false,rows,0m,"",0m,0m);
        var decision=DraftDocumentService.CreateCalculationCopy(request);
        Check(decision.MayPrintDraft, "Sürekli PDF modeli: "+decision.Reason);
        return decision.Document! with
        {
            PrintDetails = new DraftPrintDetails(5,0,850m,1,84m,250m,false,
                0,0,0,0,0,0,0m,new[]
                {
                    new DraftPrintComponent("Kişi sabit unsuru",17000m),
                    new DraftPrintComponent("Aile sabit unsuru",8500m),
                    new DraftPrintComponent("Mesafeye bağlı unsur",3570m)
                })
        };
    }
    Test("Örnek 27 biçimli gerçek hesaplama PDF: 5470 TL",()=>{
        var document=QuickForm();
        Check(document.GrossTry==5470m && document.IsQuickTemporaryCalculation,
            "Geçici PDF hesabı yanlış.");
        var path=Path.Combine(dir,"Ornek27_HESAPLAMA_CETVELI.pdf");
        TravelDraftPdfWriter.SaveDraft(document,path);
        PdfCheck(path,1);
    });
    Test("Örnek 28 biçimli gerçek hesaplama PDF: 31270 TL",()=>{
        var document=PermanentForm();
        Check(document.GrossTry==31270m && document.PrintDetails!.FamilyCount==1,
            "Sürekli PDF hesabı yanlış.");
        var path=Path.Combine(dir,"Ornek28_HESAPLAMA_CETVELI.pdf");
        TravelDraftPdfWriter.SaveDraft(document,path);
        PdfCheck(path,1);
    });
    Test("Gider dökümü değiştirilirse yeni PDF yazıcısı reddeder",()=>{
        var doc=PermanentForm() with
        {
            PrintDetails = PermanentForm().PrintDetails! with
            {
                Components = new[]
                {
                    new DraftPrintComponent("Kişi sabit unsuru",17000m),
                    new DraftPrintComponent("Aile sabit unsuru",8500m),
                    new DraftPrintComponent("Mesafeye bağlı unsur",2000m)
                }
            }
        };
        var path=Path.Combine(dir,"UYUSMAYAN_FORM.pdf");
        bool rejected=false;
        try { TravelDraftPdfWriter.SaveDraft(doc,path); }
        catch(InvalidDataException) { rejected=true; }
        Check(rejected && !File.Exists(path), "Yanlış PDF bileşeni reddedilmedi.");
    });
    Test("40 satır - sayfalar bölünür",()=>{
        var path=Path.Combine(dir,"CokSatir_TASLAK.pdf");
        TravelDraftPdfWriter.SaveDraft(Create("GEC-2026-040",DraftKind.TemporaryDuty,40),path);
        PdfCheck(path,4);
    });
    Test("2 personel - taslak icmal",()=>{
        var input=new[]{Create("GEC-2026-021"),Create("SUR-2026-022",DraftKind.PermanentDuty)};
        var sum=DraftDocumentService.CreateSummary(input);
        Check(sum.Ready,"İcmal modeli reddetti.");
        var path=Path.Combine(dir,"Icmal_TASLAK.pdf");
        TravelDraftPdfWriter.SaveSummary(sum.Summary!,path);
        PdfCheck(path,1);
    });
    Test("15 personel - 2 sayfa icmal",()=>{
        var input=Enumerable.Range(1,15).Select(i=>Create("GEC-2026-"+i.ToString("D3"))).ToArray();
        var sum=DraftDocumentService.CreateSummary(input);
        Check(sum.Ready,"İcmal hazırlanamadı.");
        var path=Path.Combine(dir,"Icmal15_TASLAK.pdf");
        TravelDraftPdfWriter.SaveSummary(sum.Summary!,path);
        PdfCheck(path,2);
    });
    Test("Mevcut PDF üzerine yazılmaz",()=>{
        var path=Path.Combine(dir,"Ornek27_TASLAK.pdf");
        bool thrown=false;
        try { TravelDraftPdfWriter.SaveDraft(Create("GEC-2026-001"),path); }
        catch(IOException) { thrown=true; }
        Check(thrown,"Üzerine yazma engellenmedi.");
    });
    Test("Resmî olarak işaretlenen/tutarsız belge reddedilir",()=>{
        var path=Path.Combine(dir,"Olmamali.pdf");
        bool thrown=false;
        try { TravelDraftPdfWriter.SaveDraft(Create("GEC-2026-099") with { NetTry=999m },path); }
        catch(InvalidDataException) { thrown=true; }
        Check(thrown && !File.Exists(path),"Tutar uyuşmazlığı engellenmedi.");
    });
    if(args.Length>0)
    {
        var demoDir=Path.Combine(args[0], "DEMO_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")[..5]);
        Directory.CreateDirectory(demoDir);
        foreach(var file in new[]{"Ornek27_TASLAK.pdf","Ornek28_TASLAK.pdf","Icmal_TASLAK.pdf",
            "Ornek27_HESAPLAMA_CETVELI.pdf","Ornek28_HESAPLAMA_CETVELI.pdf"})
            File.Copy(Path.Combine(dir,file),Path.Combine(demoDir,file),false);
        Console.WriteLine("[DEMO] Örnek kontrol çıktıları: " + demoDir);
    }
    Console.WriteLine($"TOPLAM: {passed}/11 PDF DUMAN TESTİ BAŞARILI");
    Console.WriteLine("UYARI: Taslaklar resmî Örnek 27/28 formunun birebir onaylı sureti değildir.");
    return 0;
}
finally
{
    if(Directory.Exists(dir)) Directory.Delete(dir,true);
}
