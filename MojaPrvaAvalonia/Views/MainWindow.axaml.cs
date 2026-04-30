using Avalonia.Controls;
using System;
using System.Collections.Specialized;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Táto metóda sa zavolá, keď sa okno prepojí s ViewModelom
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
            
        if (DataContext is MainWindowViewModel vm)
        {
            // Prihlásime sa na odber udalostí "kolekcia logov sa zmenila"
            vm.VypisLogov.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null && args.NewItems.Count > 0)
                {
                    // Posunie ListBox na najnovšie pridaný riadok
                    LogListBox.ScrollIntoView(args.NewItems[0]); 
                }
            };
        }
    }
}