using CoreTray.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace CoreTray.Views;

public sealed partial class CPUPage : Page
{
    public CPUViewModel ViewModel
    {
        get;
    }

    public CPUPage()
    {
        ViewModel = App.GetService<CPUViewModel>();
        InitializeComponent();
    }
}
