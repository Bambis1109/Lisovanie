using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.ViewModels;

public partial class ParameterItemViewModel : ObservableObject
{
    private readonly object _owner;
    private readonly PropertyInfo _property;

    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string _category;

    /// <summary>
    /// False znamená, že hodnota je len zástupná nula - čítanie zo zariadenia zlyhalo
    /// a nikto ju odvtedy nenastavil. Taká hodnota sa nesmie zapísať späť do zariadenia.
    /// </summary>
    [ObservableProperty] private bool _isValueKnown = true;

    public ParameterItemViewModel(object owner, PropertyInfo property, string displayName, string description, string category)
    {
        _owner = owner;
        _property = property;
        _displayName = displayName;
        _description = description;
        _category = category;
    }

    // Double pokrýva aj int properties (DeviceParameters) - typ sa pri zápise
    // konvertuje späť na skutočný typ property.
    public double Value
    {
        get => Convert.ToDouble(_property.GetValue(_owner) ?? 0);
        set
        {
            _property.SetValue(_owner, Convert.ChangeType(value, _property.PropertyType));
            IsValueKnown = true;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Hodnota pripravená na zápis do 4-bajtového registra (dvojkový doplnok).
    /// Konverzia MUSÍ ísť cez int: priame pretypovanie double -> uint v .NET saturuje,
    /// takže záporná hodnota (napr. va_offset) by sa do zariadenia zapísala ako 0.
    /// </summary>
    public uint RegisterValue => unchecked((uint)(int)Math.Round(Value, MidpointRounding.AwayFromZero));

    public void Refresh()
    {
        OnPropertyChanged(nameof(Value));
    }
}
