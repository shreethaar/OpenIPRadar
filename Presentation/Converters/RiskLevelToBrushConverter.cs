using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using OpenIPRadar.Core.Enums;

namespace OpenIPRadar.Presentation.Converters;

/// <summary>
/// Converts a <see cref="RiskLevel"/> into a brush for risk-colored badges and rows.
/// </summary>
public sealed class RiskLevelToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Malicious = new((Color)ColorConverter.ConvertFromString("#F8D7DA"));
    private static readonly SolidColorBrush Suspicious = new((Color)ColorConverter.ConvertFromString("#FFF3CD"));
    private static readonly SolidColorBrush Clean = new((Color)ColorConverter.ConvertFromString("#D1E7DD"));
    private static readonly SolidColorBrush Unknown = new((Color)ColorConverter.ConvertFromString("#E2E3E5"));

    static RiskLevelToBrushConverter()
    {
        Malicious.Freeze();
        Suspicious.Freeze();
        Clean.Freeze();
        Unknown.Freeze();
    }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        RiskLevel.Malicious => Malicious,
        RiskLevel.Suspicious => Suspicious,
        RiskLevel.Clean => Clean,
        _ => Unknown
    };

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
