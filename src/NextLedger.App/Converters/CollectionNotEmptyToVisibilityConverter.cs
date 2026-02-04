using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace NextLedger.App.Converters;

public sealed class CollectionNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
            return Visibility.Collapsed;

        if (value is string s)
            return string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible;

        if (value is ICollection collection)
            return collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (value is IEnumerable enumerable)
        {
            foreach (var _ in enumerable)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
