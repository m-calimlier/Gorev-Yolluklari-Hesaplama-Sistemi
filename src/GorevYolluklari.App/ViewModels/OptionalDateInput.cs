using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace GorevYolluklari.App.ViewModels;

public sealed class OptionalDateInput : IValueConverter
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    public static bool TryRead(string text, out DateTime date) => DateTime.TryParseExact(text.Trim(),
        new[] { "dd.MM.yyyy HH:mm", "d.M.yyyy H:mm" }, Tr, DateTimeStyles.None, out date);
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime date ? date.ToString("dd.MM.yyyy HH:mm", Tr) : "";
    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(text)) return null;
        return TryRead(text, out var date) ? DateTime.SpecifyKind(date, DateTimeKind.Unspecified) : DependencyProperty.UnsetValue;
    }
}
public sealed class OptionalDateRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var text = value?.ToString() ?? "";
        return string.IsNullOrWhiteSpace(text) || OptionalDateInput.TryRead(text, out _) ? ValidationResult.ValidResult
            : new ValidationResult(false, "Tarih-saat gg.AA.yyyy SS:dd biçiminde olmalı veya boş bırakılmalı.");
    }
}
