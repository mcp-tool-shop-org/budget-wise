using NextLedger.Domain.ValueObjects;
using Microsoft.UI.Xaml.Data;

namespace NextLedger.App.Converters;

public sealed class MoneyToFormattedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not Money money)
            return string.Empty;

        var text = money.ToFormattedString();

        // Prefer -$12.34 over $-12.34
        if (money.Currency == "USD" && money.Amount < 0m && text.StartsWith("$-", StringComparison.Ordinal))
            return "-" + text.Replace("$-", "$", StringComparison.Ordinal);

        return text;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
