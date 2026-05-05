using System.Globalization;
using System.Windows.Data;

namespace PdfManager.App.Converters;

public sealed class MoonSunConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "🌙" : "☀️";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
