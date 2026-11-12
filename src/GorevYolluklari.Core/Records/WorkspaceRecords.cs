using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;
namespace GorevYolluklari.Core.Records;
public sealed class WorkspaceRecords
{
    public CalculationConstants Constants { get; set; } = new();
    public int SchemaVersion { get; set; } = 1;
    public int Revision { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<PersonnelRecord> Personnel { get; set; } = [];
    public List<DutyRecord> Duties { get; set; } = [];
}
public sealed record CalculationConstants
{
    public TemporaryConstants Temporary { get; init; } = new();
    public PermanentConstants Permanent { get; init; } = new();
    public CommonConstants Common { get; init; } = new();
}

public sealed class PersonnelRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Institution { get; set; } = "";
    public int? Grade { get; set; }
    public int? Indicator { get; set; }
    public PersonnelTariffGroup Group { get; set; } = PersonnelTariffGroup.Unknown;
    public override string ToString() => FullName;
}
public sealed class ExpenseEntry
{
    public string LineId { get; set; } = "";
    public DraftLineKind Kind { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Evidence { get; set; } = "";
    public string LegalBasis { get; set; } = "";
    public DraftExpenseLine ToLine() => new(LineId, Kind, Description, Amount, Evidence, LegalBasis);
}
public sealed class ReviewedDayEntry
{
    public DateTime Date { get; set; } = new(2026, 1, 1);
    public int? LodgingOrdinal { get; set; }
    public decimal? LodgingAmount { get; set; }
    public string? Receipt { get; set; }
}
public sealed class DutyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonnelId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DraftKind Kind { get; set; } = DraftKind.TemporaryDuty;
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime Departure { get; set; } = new(2026, 1, 1, 9, 0, 0);
    public DateTime Return { get; set; } = new(2026, 1, 1, 20, 0, 0);
    public DateTime? OutboundArrival { get; set; }
    public DateTime? WorkStarts { get; set; }
    public DateTime? WorkEnds { get; set; }
    public DateTime? InboundDeparture { get; set; }
    public string OutboundEvidence { get; set; } = "";
    public string InboundEvidence { get; set; } = "";
    public string WorkTimeEvidence { get; set; } = "";
    public bool WorkTimesReviewed { get; set; }
    public bool ClockConcern { get; set; }
    public DutyStationAssessment Station { get; set; }
    public string AssignmentEvidence { get; set; } = "";
    public string StationEvidence { get; set; } = "";
    public bool AssignmentReviewed { get; set; }
    public bool StationReviewed { get; set; }
    public bool AmountsReviewed { get; set; }
    public string Reviewer { get; set; } = "";
    public DateTimeOffset? ReviewedAt { get; set; }
    public bool FamilyReviewed { get; set; }
    public bool SpouseTransfer { get; set; }
    public int FamilyCount { get; set; }
    public decimal Kilometers { get; set; }
    // Dokuz alanlı sürekli görev hesaplamasında tek kişi için taşıt bedeli.
    // Eski JSON dosyalarında alan yoksa null olur; kayıt uyumluluğu korunur.
    public decimal? TravelFarePerPerson { get; set; }
    public string DistanceEvidence { get; set; } = "";
    public bool AnnualPeriodReviewed { get; set; }
    public bool PriorDaysReviewed { get; set; }
    public bool EligibleDaysReviewed { get; set; }
    public bool Article42ExceptionsExcluded { get; set; }
    public bool LodgingReviewed { get; set; }
    public int? PriorDays { get; set; }
    public string PriorDaysEvidence { get; set; } = "";
    public List<ReviewedDayEntry> ReviewedDays { get; set; } = [];
    public List<ExpenseEntry> Expenses { get; set; } = [];
    public decimal Advance { get; set; }
    public string AdvanceEvidence { get; set; } = "";
    public decimal? IndependentGross { get; set; }
    public decimal? IndependentNet { get; set; }
    public CalculationSnapshot? Snapshot { get; set; }
    public static DutyRecord CreateDemo(Guid personnelId) => new()
    {
        PersonnelId = personnelId, DocumentNumber = "DEMO-" + Guid.NewGuid().ToString("N")[..8],
        Origin = "Ankara", Destination = "Konya", Departure = new(2026, 6, 15, 9, 0, 0),
        Return = new(2026, 6, 15, 20, 0, 0), Station = DutyStationAssessment.Outside,
        AssignmentEvidence = "DEMO-OLUR", StationEvidence = "DEMO-MAHAL"
    };
    public override string ToString() => DocumentNumber + " · " + Origin + " → " + Destination;
}
public sealed record CalculationSnapshot(string Fingerprint, string Status, decimal? Amount,
    decimal? DailyRate, string? Category, string TariffReference, string Explanation,
    string LegalBasis, DateTimeOffset CreatedAt, PersonnelRecord PersonnelAtCalculation,
    string RuleVersion = "ASAMA8-2026-1", IReadOnlyList<CalculationComponent>? Components = null);

public sealed record CalculationComponent(string Label, decimal Amount);
