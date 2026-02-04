using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace NextLedger.App.Converters;

/// <summary>
/// Converts boolean validation result to a color brush.
/// true = green, false = orange/warning.
/// </summary>
public sealed class BoolToValidationColorConverter : IValueConverter
{
    private static readonly SolidColorBrush ValidBrush = new(Windows.UI.Color.FromArgb(255, 34, 139, 34)); // ForestGreen
    private static readonly SolidColorBrush InvalidBrush = new(Windows.UI.Color.FromArgb(255, 255, 140, 0)); // DarkOrange

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isValid = value is bool b && b;
        return isValid ? ValidBrush : InvalidBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
