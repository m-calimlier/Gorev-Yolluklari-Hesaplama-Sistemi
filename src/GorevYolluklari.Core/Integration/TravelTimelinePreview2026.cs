using GorevYolluklari.Core.Rules;
using GorevYolluklari.Core.Tariffs;

namespace GorevYolluklari.Core.Integration;

/// <summary>
/// Aşama 6: Gerçek zaman damgalarından aday görevlendirme ve seyahat takvimi.
/// 6245 m.39 günübirlik hesabı var olan Aşama 4 motoruna yönlendirilir.
/// m.43 için hesaplanan süre birimleri ONAYLI ödeme günü değildir;
/// m.42/33(d) listesine otomatik aktarılmaz, mükerrer ödeme üretilmez.
/// </summary>
public enum TimelineStatus { PreliminarySameDay, NeedsInput, NeedsReview, NoApplicableYear, NotEligibleByLocation }

/// <param name="Departure">Taşıtın gerçek hareket zamanı (yerel duvar saati, DateTimeKind.Unspecified).</param>
/// <param name="Arrival">Taşıtın gerçek varış zamanı (yerel duvar saati, DateTimeKind.Unspecified).</param>
/// <param name="EvidenceReference">Bilet, görev emri ya da onaylı zaman belgesi yerel referansı.</param>
public sealed record JourneyLeg(DateTime Departure, DateTime Arrival, string? EvidenceReference);

/// <summary>
/// Tüm tarih-saatler Türkiye yerel saatine çevrilip Unspecified olarak girilmelidir.
/// Yaz/kış saati, saat dilimi veya zaman kayması şüphesi ayrıca kontrol edilmelidir.
/// </summary>
public sealed record TemporaryDutyItinerary2026(
    PersonnelTariffInput Personnel,
    DutyStationAssessment Station,
    bool AssignmentReviewed,
    string? AssignmentEvidenceReference,
    string? StationEvidenceReference,
    JourneyLeg Outbound,
    DateTime WorkStarts,
    DateTime WorkEnds,
    string? WorkTimeEvidenceReference,
    bool WorkTimesReviewed,
    JourneyLeg Inbound,
    bool TimeZoneOrClockChangeConcern = false);

public sealed record TimelinePreviewResult(
    TimelineStatus Status,
    string Explanation,
    string LegalBasis,
    string TariffReference,
    IReadOnlyList<DateOnly> CandidateWorkDates,
    IReadOnlyList<DateOnly> CandidateTravelDates,
    IReadOnlyList<DateOnly> CandidateCalendarDates,
    IReadOnlyList<DateOnly> OverlapDates,
    decimal? OutboundDurationHours,
    decimal? InboundDurationHours,
    int? OutboundArticle43CandidateUnits,
    int? InboundArticle43CandidateUnits,
    IntegratedPreview? SameDayPreview)
{
    public bool IsOfficialPaymentDocument => false;
    public bool IsReviewedEligibleDayLedger => false;
}

public sealed class TravelTimelinePreviewEngine2026
{
    private const string Legal = "6245 sayılı Kanun m.39, m.42 ve m.43 (seyahat zaman çizelgesi / ön hesap)";
    private const string TariffReference = "BUT-002:2026:I-B";
    private readonly TemporaryDutyEngine _sameDayEngine;
    private readonly DailyRateTariff2026 _tariff;

    public TravelTimelinePreviewEngine2026(DailyRateTariff2026 tariff)
    {
        ArgumentNullException.ThrowIfNull(tariff);
        _tariff = tariff;
        _sameDayEngine = new TemporaryDutyEngine(tariff);
    }

    public TimelinePreviewResult Preview(TemporaryDutyItinerary2026 request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Personnel);
        if (request.Outbound is null || request.Inbound is null)
            return Block(TimelineStatus.NeedsInput, "Gidiş ve dönüş yolculuk kayıtları zorunludur.");

        var points = new[] {
            request.Outbound.Departure, request.Outbound.Arrival,
            request.WorkStarts, request.WorkEnds,
            request.Inbound.Departure, request.Inbound.Arrival
        };
        if (points.Any(x => x == default))
            return Block(TimelineStatus.NeedsInput, "Tarih-saat alanları boş olamaz.");
        if (points.Any(x => x.Kind != DateTimeKind.Unspecified) || request.TimeZoneOrClockChangeConcern)
            return Block(TimelineStatus.NeedsReview, "Saat dilimi, UTC/Local DateTime veya saat değişimi şüphesi: yerel saatler doğrulanmalı.");
        if (points.Any(x => x.Year != 2026) || request.Personnel.DutyDate.Year != 2026)
            return Block(TimelineStatus.NoApplicableYear, "Bu sürüm yalnız 2026 tarihli yurt içi görevlendirmeleri işler.");
        if (request.Personnel.DutyDate != DateOnly.FromDateTime(request.Outbound.Departure))
            return Block(TimelineStatus.NeedsReview, "Tarife işlem tarihi gerçek yolculuk başlangıç tarihiyle aynı olmalı.");
        if (request.Outbound.Arrival <= request.Outbound.Departure ||
            request.WorkStarts < request.Outbound.Arrival ||
            request.WorkEnds <= request.WorkStarts ||
            request.Inbound.Departure < request.WorkEnds ||
            request.Inbound.Arrival <= request.Inbound.Departure)
            return Block(TimelineStatus.NeedsReview, "Gidiş, görev, dönüş saatleri kronolojik değil veya bir süre sıfır/negatif.");
        if ((request.Inbound.Arrival - request.Outbound.Departure).TotalDays > 366d)
            return Block(TimelineStatus.NeedsReview, "366 günü aşan güzergâh bu aşamada manuel inceleme gerektirir.");
        if (request.Station == DutyStationAssessment.Inside)
            return Block(TimelineStatus.NotEligibleByLocation, "Memuriyet mahalli içinde gündelik ön hesabı yapılmaz.");
        if (request.Station != DutyStationAssessment.Outside || string.IsNullOrWhiteSpace(request.StationEvidenceReference))
            return Block(TimelineStatus.NeedsReview, "Memuriyet mahalli dışı olma durumu dayanaklı doğrulanmalı.");
        if (!request.AssignmentReviewed || string.IsNullOrWhiteSpace(request.AssignmentEvidenceReference))
            return Block(TimelineStatus.NeedsReview, "Görevlendirme oluru belgesi veya incelemesi eksik.");
        if (!request.WorkTimesReviewed || string.IsNullOrWhiteSpace(request.WorkTimeEvidenceReference))
            return Block(TimelineStatus.NeedsReview, "Fiilî görev başlangıç/bitiş saatleri belgeli incelenmeli.");
        if (string.IsNullOrWhiteSpace(request.Outbound.EvidenceReference) ||
            string.IsNullOrWhiteSpace(request.Inbound.EvidenceReference))
            return Block(TimelineStatus.NeedsReview, "Gidiş ve dönüş hareket/varış saatleri için belge referansları zorunlu.");

        var rateDecision = _tariff.Resolve(request.Personnel);
        if (rateDecision.Status != RateDecisionStatus.ResolvedPreliminary)
        {
            var status = rateDecision.Status switch
            {
                RateDecisionStatus.NoApplicableYear => TimelineStatus.NoApplicableYear,
                RateDecisionStatus.NeedsInput => TimelineStatus.NeedsInput,
                _ => TimelineStatus.NeedsReview
            };
            return Block(status, "Tarife doğrulanamadı: " + rateDecision.Explanation);
        }

        var work = DaysBetween(request.WorkStarts.Date, request.WorkEnds.Date);
        var travel = DaysBetween(request.Outbound.Departure.Date, request.Outbound.Arrival.Date)
            .Concat(DaysBetween(request.Inbound.Departure.Date, request.Inbound.Arrival.Date))
            .Distinct().OrderBy(x => x).ToArray();
        var covered = work.Concat(travel).Distinct().OrderBy(x => x).ToArray();
        var overlaps = work.Intersect(travel).OrderBy(x => x).ToArray();
        var outwardHours = (decimal)(request.Outbound.Arrival - request.Outbound.Departure).Ticks / TimeSpan.TicksPerHour;
        var inwardHours = (decimal)(request.Inbound.Arrival - request.Inbound.Departure).Ticks / TimeSpan.TicksPerHour;
        var oneDay = request.Outbound.Departure.Date == request.Inbound.Arrival.Date;

        // Günübirlik seyahatlerde m.39 uygulanır; m.43 birimleri ayrıca eklenmez.
        if (oneDay)
        {
            var preview = _sameDayEngine.PreviewSameDay(new SameDayDutyRequest(
                request.Personnel, request.Outbound.Departure, request.Inbound.Arrival,
                request.Station, request.AssignmentReviewed, request.StationEvidenceReference));
            var status = preview.Status switch
            {
                IntegratedPreviewStatus.Preliminary => TimelineStatus.PreliminarySameDay,
                IntegratedPreviewStatus.NeedsInput => TimelineStatus.NeedsInput,
                IntegratedPreviewStatus.NoApplicableYear => TimelineStatus.NoApplicableYear,
                IntegratedPreviewStatus.NotEligibleByLocation => TimelineStatus.NotEligibleByLocation,
                _ => TimelineStatus.NeedsReview
            };
            return new(status,
                "Aynı gün gidip dönülen görev: m.39 sonucu ayrıdır; m.43 yolculuk birimleri gündeliğe EKLENMEZ. " + preview.Explanation,
                Legal, preview.TariffReference,
                work, travel, covered, overlaps, outwardHours, inwardHours,
                null, null, preview);
        }

        // m.43 her ulaşım ayağı için 24 saat kesirleri yukarı tamamlar; bu sadece
        // ham seyahat SÜRESİ birimi, ödeme günü değildir. Yol/görev çakışmaları
        // ve m.42 sıralaması inceleme bekler. Toplam gündelik ASLA oluşturulmaz.
        return new(TimelineStatus.NeedsReview,
            "Çok günlü seyahat: m.43 süre birimleri ADAYDIR. Yol ve görev tarihleri çakışabilir; " +
            "m.39 geceleme, m.42 gün sırası, fiilî süre ve 33(d) konaklama ayrı onaylanmadan ödeme günleri oluşmaz.",
            Legal, TariffReference,
            work, travel, covered, overlaps, outwardHours, inwardHours,
            RoundTravel24HourUnits(request.Outbound.Departure, request.Outbound.Arrival),
            RoundTravel24HourUnits(request.Inbound.Departure, request.Inbound.Arrival), null);
    }

    private static DateOnly[] DaysBetween(DateTime begin, DateTime end)
    {
        var start = DateOnly.FromDateTime(begin);
        var finish = DateOnly.FromDateTime(end);
        var size = finish.DayNumber - start.DayNumber + 1;
        var result = new DateOnly[size];
        for (int i = 0; i < size; i++) result[i] = start.AddDays(i);
        return result;
    }

    // m.43: Bir günden kısa süre de bir gün; 24 saatin kesri tam gün sayılır.
    private static int RoundTravel24HourUnits(DateTime from, DateTime to)
    {
        var ticks = (to - from).Ticks;
        return checked((int)((ticks + TimeSpan.TicksPerDay - 1) / TimeSpan.TicksPerDay));
    }

    private static TimelinePreviewResult Block(TimelineStatus status, string explanation) =>
        new(status, explanation, Legal, TariffReference,
            Array.Empty<DateOnly>(), Array.Empty<DateOnly>(),
            Array.Empty<DateOnly>(), Array.Empty<DateOnly>(),
            null, null, null, null, null);
}
