using Avalonia;
using Avalonia.Controls;
using MojaPrvaAvalonia.ViewModels;

namespace MojaPrvaAvalonia.Views.UserControls;

public partial class UcDeviceEpos4 : UserControl
{
    public UcDeviceEpos4()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is UcDeviceEpos4ViewModel viewModel)
        {
            viewModel.StartRefresh();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is UcDeviceEpos4ViewModel viewModel)
        {
            viewModel.StopRefresh();
        }
    }
    
    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (VisualRoot != null && DataContext is UcDeviceEpos4ViewModel viewModel)
        {
             viewModel.StartRefresh();
        }
    }
}
