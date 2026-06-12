using System;
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

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (DataContext is ParametersViewModel vm)
                vm.LoadFromDeviceCommand.Execute(null);
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
