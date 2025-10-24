using CoreTray.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace CoreTray.Views;

public sealed partial class RAMPage : Page
{
    public RAMViewModel ViewModel
    {
        get;
    }

    public RAMPage()
    {
        ViewModel = App.GetService<RAMViewModel>();
        InitializeComponent();
    }
}
