using Avalonia.Controls;
using Avalonia.Interactivity;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views
{
    public partial class ParametersWindow : Window
    {
        public ParametersWindow()
        {
            InitializeComponent();
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
