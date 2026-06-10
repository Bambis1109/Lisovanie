using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.ViewModels;

namespace Lisovanie.Views
{
    public partial class ParametersWindow : Window
    {
        public ParametersWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Control_GotFocus(object? sender, RoutedEventArgs e)
        {
            if (sender is Control control && control.DataContext is ParameterItemViewModel vm)
            {
                if (DataContext is ParametersViewModel mainVm)
                {
                    mainVm.SelectedParameter = vm;
                }
            }
        }
    }
}
