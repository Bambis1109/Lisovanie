using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MojaPrvaAvalonia.Converters;

public class EqualityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        
        string valStr = value.ToString() ?? "";
        string paramStr = parameter.ToString() ?? "";

        // Porovnanie ako double pre presnosť s 0.1
        if (double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d1) &&
            double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d2))
        {
            return Math.Abs(d1 - d2) < 0.001;
        }

        return valStr == paramStr;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            if (double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
            {
                return d;
            }
            return parameter;
        }
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}