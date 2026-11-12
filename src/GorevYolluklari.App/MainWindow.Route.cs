using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using GorevYolluklari.Application;
using GorevYolluklari.Core.Documents;
using GorevYolluklari.Core.Records;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private static readonly Lazy<Province[]> ProvinceCatalog = new(LoadProvinces);
    private CancellationTokenSource? _distanceRequest;
    private readonly Func<KgmPlace, KgmPlace, CancellationToken, Task<int>> _fetchDistance;

    private static Province[] LoadProvinces()
    {
        using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream("KgmIlIlce.json")
            ?? throw new InvalidDataException("KGM il ve ilçe kataloğu bulunamadı.");
        var catalog = JsonSerializer.Deserialize<PlaceCatalog>(stream);
        return catalog?.Provinces is { Length: 81 } provinces
            ? provinces : throw new InvalidDataException("KGM il ve ilçe kataloğu eksik.");
    }

    private void AddRoute(Panel left, Panel right, DutyRecord duty)
    {
        AddPlace(left, "Çıkış", duty.Origin, place => RouteChanged(duty, true, place));
        AddPlace(right, "Varış", duty.Destination, place => RouteChanged(duty, false, place));
    }

    private void AddPlace(Panel parent, string label, string stored, Action<string> changed)
    {
        var selected = ParsePlace(stored);
        var province = new ComboBox { ItemsSource = ProvinceCatalog.Value, DisplayMemberPath = nameof(Province.Name), SelectedValuePath = nameof(Province.Name),
            SelectedItem = ProvinceCatalog.Value.FirstOrDefault(p => p.Name == selected?.Province) };
        var district = new ComboBox { ItemsSource = (province.SelectedItem as Province)?.Districts,
            SelectedItem = selected?.District, IsEnabled = province.SelectedItem is not null };
        AutomationProperties.SetName(province, label + " ili");
        AutomationProperties.SetName(district, label + " ilçesi");
        parent.Children.Add(new Label { Content = Heading(label + " ili"), Target = province });
        parent.Children.Add(province);
        parent.Children.Add(new Label { Content = Heading(label + " ilçesi"), Target = district });
        parent.Children.Add(district);
        RegisterIssue(label == "Çıkış" ? "Origin" : "Destination", district, parent);
        var previous = Text(selected is null && !string.IsNullOrWhiteSpace(stored) ? "Kayıtlı yer: " + stored : "", 12);
        if (previous.Text.Length > 0) parent.Children.Add(previous);
        var changingProvince = false;
        province.SelectionChanged += (_, _) =>
        {
            changingProvince = true;
            district.SelectedItem = null;
            district.ItemsSource = (province.SelectedItem as Province)?.Districts;
            district.IsEnabled = province.SelectedItem is not null;
            changingProvince = false;
            previous.Visibility = Visibility.Collapsed;
            changed("");
        };
        district.SelectionChanged += (_, _) =>
        {
            if (!changingProvince && province.SelectedItem is Province city)
                changed(district.SelectedItem is string name ? new KgmPlace(city.Name, name).ToString() : "");
        };
    }

    private void RouteChanged(DutyRecord duty, bool origin, string place)
    {
        var previous = origin ? duty.Origin : duty.Destination;
        if (previous == place) return;
        if (origin) duty.Origin = place;
        else duty.Destination = place;
        Invalidate(duty, origin ? nameof(duty.Origin) : nameof(duty.Destination));
        CancelDistance();
        if (duty.Kind != DraftKind.PermanentDuty) return;
        duty.Kilometers = 0;
        duty.DistanceEvidence = "";
        UpdateDistanceFields();
        if (ParsePlace(duty.Origin) is not null && ParsePlace(duty.Destination) is not null)
            _ = FillDistanceAsync(duty);
    }

    private void CancelDistance()
    {
        var request = _distanceRequest;
        _distanceRequest = null;
        request?.Cancel();
    }

    private async Task FillDistanceAsync(DutyRecord duty)
    {
        var from = ParsePlace(duty.Origin);
        var to = ParsePlace(duty.Destination);
        if (from is null || to is null)
        {
            _dialog("Çıkış ve varış için il ve ilçe seçin.", "KGM mesafe sorgusu", MessageBoxButton.OK);
            return;
        }

        CancelDistance();
        using var request = new CancellationTokenSource();
        _distanceRequest = request;
        var initialDistance = duty.Kilometers;
        var initialEvidence = duty.DistanceEvidence;
        Status.Text = "KGM mesafesi sorgulanıyor…";
        try
        {
            var km = await _fetchDistance(from, to, request.Token);
            if (!ReferenceEquals(request, _distanceRequest) || duty.Kilometers != initialDistance || duty.DistanceEvidence != initialEvidence)
                return;

            var uncommittedDistance = DistanceField("Kaynaklı kilometre");
            var uncommittedEvidence = DistanceField("KGM / mesafe kanıtı");
            if ((uncommittedDistance?.IsKeyboardFocusWithin == true &&
                    uncommittedDistance.Text != initialDistance.ToString(Tr)) ||
                (uncommittedEvidence?.IsKeyboardFocusWithin == true && uncommittedEvidence.Text != initialEvidence))
                return;

            duty.Kilometers = km;
            duty.DistanceEvidence = $"KGM İlçeler Arası Mesafe · {from} → {to} · {DateTime.Today:dd.MM.yyyy}";
            Invalidate(duty, nameof(duty.Kilometers));
            UpdateDistanceFields();
            Status.Text = $"KGM mesafesi: {km} km. Mesafe ve güzergâhı inceleyin.";
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or
                                    InvalidDataException or JsonException or System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            if (!ReferenceEquals(request, _distanceRequest)) return;
            var reason = !NetworkInterface.GetIsNetworkAvailable() ? "İnternet bağlantısı yok."
                : ex is InvalidDataException or JsonException ? "KGM bu güzergâh için doğrulanabilir mesafe döndürmedi."
                : "KGM'ye ulaşılamadı. İnternet bağlantınızı kontrol edin.";
            Status.Text = reason + " Mesafeyi ve dayanağını elle girin.";
            _dialog(Status.Text, "KGM mesafe sorgusu", MessageBoxButton.OK);
        }
        finally
        {
            if (ReferenceEquals(request, _distanceRequest)) _distanceRequest = null;
        }
    }

    private TextBox? DistanceField(string label) => _editors.OfType<TextBox>()
        .FirstOrDefault(box => AutomationProperties.GetName(box) == label);

    private void UpdateDistanceFields()
    {
        DistanceField("Kaynaklı kilometre")?.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
        DistanceField("KGM / mesafe kanıtı")?.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
    }

    private static KgmPlace? ParsePlace(string value)
    {
        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return null;
        var province = ProvinceCatalog.Value.FirstOrDefault(p => Tr.CompareInfo.Compare(p.Name, parts[0],
            System.Globalization.CompareOptions.IgnoreCase) == 0);
        var district = province?.Districts.FirstOrDefault(d => Tr.CompareInfo.Compare(d, parts[1],
            System.Globalization.CompareOptions.IgnoreCase) == 0);
        return province is not null && district is not null ? new(province.Name, district) : null;
    }

    private sealed record PlaceCatalog(Province[] Provinces);
    private sealed record Province(string Name, string[] Districts);
}
