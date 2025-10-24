using Microsoft.UI.Xaml.Data;

namespace CoreTray.Helpers;

/// <summary>
/// Value converter for string formatting in XAML bindings.
/// </summary>
public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var format = parameter as string;
        if (string.IsNullOrEmpty(format))
        {
            return value.ToString() ?? string.Empty;
        }

        try
        {
            return string.Format(format, value);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
