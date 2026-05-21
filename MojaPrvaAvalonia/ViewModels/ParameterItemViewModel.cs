using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.ViewModels;

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

    public int Value
    {
        get => (int)(_property.GetValue(_owner) ?? 0);
        set
        {
            _property.SetValue(_owner, value);
            OnPropertyChanged();
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Value));
    }
}
