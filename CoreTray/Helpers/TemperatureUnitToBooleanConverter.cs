using CoreTray.Models;
using Microsoft.UI.Xaml.Data;

namespace CoreTray.Helpers;

/// <summary>
/// Converter for binding TemperatureUnit enum to radio buttons.
/// </summary>
public class TemperatureUnitToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TemperatureUnit currentUnit && parameter is string parameterString)
        {
            if (Enum.TryParse<TemperatureUnit>(parameterString, out var targetUnit))
            {
                return currentUnit == targetUnit;
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isChecked && isChecked && parameter is string parameterString)
        {
            if (Enum.TryParse<TemperatureUnit>(parameterString, out var targetUnit))
            {
                return targetUnit;
            }
        }
        return TemperatureUnit.Celsius;
    }
}
