using Microsoft.UI.Xaml.Data;

namespace CoreTray.Helpers;

/// <summary>
/// Value converter that formats numbers with dynamic decimal precision.
/// The parameter should specify the number of decimal places.
/// </summary>
public class DecimalPrecisionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (!double.TryParse(value.ToString(), out var doubleValue))
        {
            return value.ToString() ?? string.Empty;
        }

        if (parameter is int precision)
        {
            return doubleValue.ToString($"F{precision}");
        }

        // Default to 1 decimal place if parameter is not provided
        return doubleValue.ToString("F1");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
