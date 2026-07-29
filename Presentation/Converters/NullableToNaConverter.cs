using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace OpenIPRadar.Presentation.Converters;

/// <summary>
/// Renders <c>null</c>, empty strings, and empty collections as "N/A"; collections are joined
/// with commas. Used so the grid shows a consistent placeholder for missing provider data.
/// </summary>
public sealed class NullableToNaConverter : IValueConverter
{
    private const string NotAvailable = "N/A";

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case null:
                return NotAvailable;
            case string s:
                return string.IsNullOrWhiteSpace(s) ? NotAvailable : s;
            case IEnumerable enumerable and not string:
                var items = enumerable.Cast<object?>().Where(o => o is not null).Select(o => o!.ToString()).ToList();
                return items.Count == 0 ? NotAvailable : string.Join(", ", items);
            default:
                return value.ToString() ?? NotAvailable;
        }
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
