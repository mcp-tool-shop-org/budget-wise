using Microsoft.UI.Xaml.Data;

namespace NextLedger.App.Converters;

/// <summary>
/// Converts boolean validation result to an icon glyph.
/// true = checkmark, false = warning.
/// </summary>
public sealed class BoolToValidationIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isValid = value is bool b && b;
        return isValid ? "\uE8FB" : "\uE7BA"; // Checkmark vs Warning
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
