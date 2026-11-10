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
        foreach(var file in new[]{"Ornek27_TASLAK.pdf","Ornek28_TASLAK.pdf","Icmal_TASLAK.pdf"})
            File.Copy(Path.Combine(dir,file),Path.Combine(demoDir,file),false);
        Console.WriteLine("[DEMO] Örnek kontrol çıktıları: " + demoDir);
    }
    Console.WriteLine($"TOPLAM: {passed}/7 PDF DUMAN TESTİ BAŞARILI");
    Console.WriteLine("UYARI: Taslaklar resmî Örnek 27/28 formunun birebir onaylı sureti değildir.");
    return 0;
}
finally
{
    if(Directory.Exists(dir)) Directory.Delete(dir,true);
}
