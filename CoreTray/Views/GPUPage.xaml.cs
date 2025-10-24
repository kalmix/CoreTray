using CoreTray.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace CoreTray.Views;

public sealed partial class GPUPage : Page
{
    public GPUViewModel ViewModel
    {
        get;
    }

    public GPUPage()
    {
        ViewModel = App.GetService<GPUViewModel>();
        InitializeComponent();
    }
}
