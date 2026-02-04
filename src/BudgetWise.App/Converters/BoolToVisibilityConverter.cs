using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BudgetWise.App.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isVisible = value is bool b && b;
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;

        return false;
    }
}
