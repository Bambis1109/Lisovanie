using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcStatusPlc : UserControl
{
    public static readonly StyledProperty<bool> ShowSetupButtonProperty =
        AvaloniaProperty.Register<UcStatusPlc, bool>(nameof(ShowSetupButton), defaultValue: false);

    public bool ShowSetupButton
    {
        get => GetValue(ShowSetupButtonProperty);
        set => SetValue(ShowSetupButtonProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? SetupClicked;

    public UcStatusPlc()
    {
        InitializeComponent();
    }

    private void OnSetupClicked(object? sender, RoutedEventArgs e)
    {
        SetupClicked?.Invoke(this, e);
    }
}