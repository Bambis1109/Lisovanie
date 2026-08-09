using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Lisovanie.Models;

namespace Lisovanie.Views;

/// <summary>Položka zoznamu receptov v úvodnom dialógu.</summary>
public class CRecipeItem
{
    public string Nazov { get; init; } = string.Empty;
    public string Popis { get; init; } = string.Empty;
    public bool IsActive { get; init; }

    /// <summary>Naposledy použitý recept je zvýraznený, aby ho obsluha našla na prvý pohľad.</summary>
    public IBrush Pozadie => IsActive
        ? new SolidColorBrush(Color.Parse("#00695C"))
        : new SolidColorBrush(Color.Parse("#2D2D30"));
}

/// <summary>
/// Úvodný dialóg pre voľbu receptu. Zobrazí sa pred hlavným oknom;
/// zavrie sa názvom zvoleného receptu alebo null pri ukončení.
/// </summary>
public partial class frmRecipeSelect : Window
{
    public frmRecipeSelect()
    {
        InitializeComponent();
    }

    public frmRecipeSelect(CRecipeManager manager, string activeRecipe)
    {
        InitializeComponent();

        var items = new List<CRecipeItem>();

        foreach (var name in manager.GetRecipeNames())
        {
            var recipe = manager.PeekRecipe(name);

            items.Add(new CRecipeItem
            {
                Nazov = name,
                Popis = recipe == null
                    ? "Súbor sa nepodarilo načítať"
                    : $"Režim {recipe.Mode} · forma {recipe.Form} · výrobok {recipe.Vyrobok.Name}",
                IsActive = string.Equals(name, activeRecipe, System.StringComparison.OrdinalIgnoreCase)
            });
        }

        LstRecipes.ItemsSource = items;
        TxtEmpty.IsVisible = items.Count == 0;
    }

    private void BtnRecipe_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: CRecipeItem item })
        {
            Close(item.Nazov);
        }
    }

    private void BtnCancel_OnClick(object? sender, RoutedEventArgs e) => Close(null);
}
