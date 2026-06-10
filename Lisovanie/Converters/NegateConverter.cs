using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Lisovanie.Converters;

public class NegateConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (value != null)
            {
                double d = System.Convert.ToDouble(value);
                return -d;
            }
        }
        catch
        {
            // ignorujeme chyby pri konverzii a vrátime 0
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
