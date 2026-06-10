using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Lisovanie.Converters;

public class LogColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string logText)
        {
            if (logText.Contains("[WRN]")) return Brushes.Yellow;
            if (logText.Contains("[ERR]") || logText.Contains("[FTL]")) return Brushes.Red;
            if (logText.Contains("[INF]")) return Brushes.LimeGreen;
        }
        // Predvolená farba pre ostatné správy
        return Brushes.LightGray; 
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}