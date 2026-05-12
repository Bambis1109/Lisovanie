using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MojaPrvaAvalonia.Converters;

public class BargraphColorConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        try
        {
            if (values != null && values.Count >= 3)
            {
                double val = System.Convert.ToDouble(values[0]);
                double limit1 = System.Convert.ToDouble(values[1]);
                double limit2 = System.Convert.ToDouble(values[2]);

                double min = Math.Min(limit1, limit2);
                double max = Math.Max(limit1, limit2);
                double range = max - min;
                
                if (range <= 0) return Brushes.LimeGreen;
                
                double threshold = range * 0.05;

                // Ak je hodnota v dolných alebo horných 5% rozsahu, vrátime červenú
                if (val <= min + threshold || val >= max - threshold)
                {
                    return Brushes.Red;
                }
            }
        }
        catch
        {
            // V prípade chyby konverzie ignorujeme a vrátime predvolenú farbu
        }
        
        return Brushes.LimeGreen;
    }
}
