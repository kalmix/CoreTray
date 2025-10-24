using CoreTray.Models;

namespace CoreTray.Helpers;

/// <summary>
/// Converts temperatures between different units.
/// </summary>
public static class TemperatureConverter
{
    /// <summary>
    /// Converts a temperature from Celsius to the specified unit.
    /// </summary>
    /// <param name="celsius">Temperature in Celsius</param>
    /// <param name="targetUnit">Target temperature unit</param>
    /// <returns>Temperature in the target unit</returns>
    public static double FromCelsius(double celsius, TemperatureUnit targetUnit)
    {
        return targetUnit switch
        {
            TemperatureUnit.Celsius => celsius,
            TemperatureUnit.Fahrenheit => CelsiusToFahrenheit(celsius),
            TemperatureUnit.Kelvin => CelsiusToKelvin(celsius),
            _ => celsius
        };
    }

    /// <summary>
    /// Gets the unit symbol for display purposes.
    /// </summary>
    /// <param name="unit">Temperature unit</param>
    /// <returns>Unit symbol string</returns>
    public static string GetUnitSymbol(TemperatureUnit unit)
    {
        return unit switch
        {
            TemperatureUnit.Celsius => "°C",
            TemperatureUnit.Fahrenheit => "°F",
            TemperatureUnit.Kelvin => "K",
            _ => "°C"
        };
    }

    /// <summary>
    /// Formats a temperature value with the specified precision and unit.
    /// </summary>
    /// <param name="celsius">Temperature in Celsius</param>
    /// <param name="unit">Target temperature unit</param>
    /// <param name="precision">Number of decimal places</param>
    /// <returns>Formatted temperature string</returns>
    public static string Format(double celsius, TemperatureUnit unit, int precision)
    {
        var converted = FromCelsius(celsius, unit);
        var formatString = $"F{precision}";
        return $"{converted.ToString(formatString)}{GetUnitSymbol(unit)}";
    }

    private static double CelsiusToFahrenheit(double celsius)
    {
        return (celsius * 9.0 / 5.0) + 32.0;
    }

    private static double CelsiusToKelvin(double celsius)
    {
        return celsius + 273.15;
    }
}
