using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace NextLedger.App.Converters;

public sealed class OverspentToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isOverspent = value is bool b && b;
        if (isOverspent)
            return new SolidColorBrush(Colors.Tomato);

        if (Microsoft.UI.Xaml.Application.Current?.Resources.TryGetValue("TextFillColorPrimaryBrush", out var brush) == true && brush is Brush b2)
            return b2;

        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
