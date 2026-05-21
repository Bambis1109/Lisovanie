using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MojaPrvaAvalonia.ViewModels;

public partial class CategoryViewModel : ObservableObject
{
    [ObservableProperty] private string _header;
    public ObservableCollection<ParameterItemViewModel> Parameters { get; } = new();

    public CategoryViewModel(string header) => Header = header;
}
