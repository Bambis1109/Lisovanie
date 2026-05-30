using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Converters;

public class ProduktStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EnProduktLis status)
        {
            return status switch
            {
                EnProduktLis.Ok => Brushes.LimeGreen,
                EnProduktLis.Nok => Brushes.Red,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}