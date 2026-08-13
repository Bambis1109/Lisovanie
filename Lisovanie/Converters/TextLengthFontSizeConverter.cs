using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Lisovanie.Converters;

/// <summary>
/// Zmenší písmo podľa dĺžky textu, aby sa dlhý názov (napr. receptu v hlavičke)
/// zmestil do vyhradeného miesta. Spolu s TextWrapping sa dlhý text zalomí
/// a zároveň zmenší, namiesto toho, aby prekryl susedné ovládacie prvky.
///
/// Parameter = východisková veľkosť písma pre krátky text; bez neho sa použije 22.
/// </summary>
public class TextLengthFontSizeConverter : IValueConverter
{
    /// <summary>Koľko znakov sa pri východiskovej veľkosti ešte zmestí do jedného riadku.</summary>
    private const int PlnaDlzka = 12;

    /// <summary>Pod túto veľkosť sa už nejde - text by bol na dotykovom paneli nečitateľný.</summary>
    private const double MinFontSize = 11.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double zaklad = 22.0;
        if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
            zaklad = p;

        int dlzka = (value as string)?.Length ?? 0;
        if (dlzka <= PlnaDlzka) return zaklad;

        // Písmo klesá úmerne k dĺžke - dvojnásobne dlhý text dostane polovičnú veľkosť.
        double zmensene = zaklad * PlnaDlzka / dlzka;
        return Math.Max(MinFontSize, Math.Round(zmensene));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
