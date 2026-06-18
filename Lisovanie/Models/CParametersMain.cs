using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.Models;

public partial class CParametersMain : ObservableObject
{
    // 4-miestne číselné heslo správcu (string kvôli zachovaniu úvodných núl)
    [ObservableProperty] private string _password = "1234";
}
