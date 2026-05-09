using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcMotorReduced : UserControl
{
    public UcMotorReduced()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}