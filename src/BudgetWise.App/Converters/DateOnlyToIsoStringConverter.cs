using Microsoft.UI.Xaml.Data;

namespace BudgetWise.App.Converters;

public sealed class DateOnlyToIsoStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateOnly d)
            return d.ToString("yyyy-MM-dd");

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
