using GorevYolluklari.Core.Tariffs;
namespace GorevYolluklari.Core.Integration;
/// <summary>Eski çağrılar için uyumluluk katmanı. Hesaplama iki bağımsız çekirdektedir.</summary>
public sealed class HarcirahPreviewEngine(DailyRateTariff2026 tariff)
{
 private readonly TemporaryDutyEngine _temporary = new(tariff);
 private readonly PermanentDutyEngine _permanent = new(tariff);
 public IntegratedPreview PreviewSameDay(SameDayDutyRequest request) => _temporary.PreviewSameDay(request);
 public IntegratedPreview PreviewRelocation(RelocationDutyRequest request) => _permanent.PreviewRelocation(request);
}
