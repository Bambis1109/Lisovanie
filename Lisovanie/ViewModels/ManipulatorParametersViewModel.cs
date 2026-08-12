using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Lisovanie.Models;

namespace Lisovanie.ViewModels;

/// <summary>
/// Záložka "Manipulátor" v okne nastavení - dráhy manipulátora z receptu.
/// Zoznam parametrov sa stavia reflexiou z CParManipulator; do UI idú len
/// properties s atribútom DisplayName (VyrobokName ho nemá, preto sa vynechá).
/// </summary>
public partial class ManipulatorParametersViewModel : ViewModelBase
{
    public ObservableCollection<ParameterItemViewModel> Parameters { get; } = new();

    [ObservableProperty] private ParameterItemViewModel? _selectedParameter;

    public ManipulatorParametersViewModel(CParManipulator parameters)
    {
        foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var dispAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            if (dispAttr == null) continue;

            var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
            Parameters.Add(new ParameterItemViewModel(
                parameters, prop, dispAttr.DisplayName, descAttr?.Description ?? "", ""));
        }
    }

    /// <summary>Po Load Parameters - Reload receptu prepíše hodnoty v tej istej inštancii.</summary>
    public void RefreshAll()
    {
        foreach (var param in Parameters) param.Refresh();
    }
}
