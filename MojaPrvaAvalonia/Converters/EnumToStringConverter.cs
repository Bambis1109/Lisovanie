using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using MojaPrvaAvalonia.Models;

namespace MojaPrvaAvalonia.Converters;

public class EnumStringMap
{
    public EnMovementMode Key { get; set; }
    public string Value { get; set; } = "";
}

public class EnumToStringConverter : IValueConverter
{
    public List<EnumStringMap> Mapping { get; set; } = new List<EnumStringMap>();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EnMovementMode mode)
        {
            var map = Mapping.Find(m => m.Key == mode);
            if (map != null)
            {
                return map.Value;
            }
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}