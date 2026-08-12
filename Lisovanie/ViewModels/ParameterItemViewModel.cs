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
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Value));
    }
}
