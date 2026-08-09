using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.Models;

public partial class CParametersMain : ObservableObject
{
    // 4-miestne číselné heslo správcu (string kvôli zachovaniu úvodných núl)
    [ObservableProperty] private string _password = "1234";

    // Recept zvolený pri poslednom spustení - predvolí sa v dialógu výberu.
    [ObservableProperty] private string _activeRecipe = string.Empty;
}
