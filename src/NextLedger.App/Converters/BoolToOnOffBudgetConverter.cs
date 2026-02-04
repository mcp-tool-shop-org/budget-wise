using Microsoft.UI.Xaml.Data;

namespace NextLedger.App.Converters;

/// <summary>
/// Converts a boolean to "On Budget" or "Off Budget" text.
/// </summary>
public sealed class BoolToOnOffBudgetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOnBudget)
            return isOnBudget ? "On Budget" : "Off Budget";

        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
