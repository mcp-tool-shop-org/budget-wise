using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace NextLedger.App.Converters;

public sealed class CollectionEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
            return Visibility.Visible;

        if (value is string)
            return Visibility.Collapsed;

        if (value is ICollection collection)
            return collection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (value is IEnumerable enumerable)
        {
            foreach (var _ in enumerable)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
