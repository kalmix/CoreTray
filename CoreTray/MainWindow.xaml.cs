using CoreTray.Helpers;
using CoreTray.Contracts.Services;
using CoreTray.Views;

using Windows.UI.ViewManagement;

namespace CoreTray;

public sealed partial class MainWindow : WindowEx
{
    private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;

    private UISettings settings;

    public MainWindow()
    {
        InitializeComponent();

        // Set window icon for taskbar
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "WindowIcon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }
        
        this.CenterOnScreen(); // Bro's centering a div LOL
        
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
        
        // Ensure debug console closes when window closes
        Closed += MainWindow_Closed;
        
        // Initialize system tray
        InitializeSystemTray();
        
        // Show welcome dialog after window is activated
        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        Activated -= MainWindow_Activated;
        
        _ = ShowWelcomeDialogIfNeededAsync();
    }

    private void InitializeSystemTray()
    {
        try
        {
            _ = InitializeSystemTrayAsync(); // fire-and-forget
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing system tray: {ex.Message}");
        }
    }
    
    private async Task InitializeSystemTrayAsync()
    {
        try
        {
            var settingsService = App.GetService<IAppSettingsService>();
            var settings = await settingsService.GetSettingsAsync();
            
            if (settings.EnableSystemTray)
            {
                var systemTrayService = App.GetService<ISystemTrayService>();
                systemTrayService.Initialize();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in InitializeSystemTrayAsync: {ex.Message}");
        }
    }

    private async Task ShowWelcomeDialogIfNeededAsync()
    {
        try
        {
            var settingsService = App.GetService<IAppSettingsService>();
            var settings = await settingsService.GetSettingsAsync();
            
            System.Diagnostics.Debug.WriteLine($"HasSeenWelcomeDialog: {settings.HasSeenWelcomeDialog}");

            // Show only if not seen before
            if (!settings.HasSeenWelcomeDialog)
            {
                System.Diagnostics.Debug.WriteLine("Showing welcome dialog...");
                
                await Task.Delay(500); // 500ms

                var welcomeDialog = new WelcomeDialog();
                var result = await welcomeDialog.ShowAsync();
                
                System.Diagnostics.Debug.WriteLine($"Welcome dialog result: {result}");
                
                settings.HasSeenWelcomeDialog = true;
                await settingsService.SaveSettingsAsync(settings);
                
                System.Diagnostics.Debug.WriteLine("Welcome dialog flag saved");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Welcome dialog already seen, skipping");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing welcome dialog: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        try
        {
            var systemTrayService = App.GetService<ISystemTrayService>();
            if (systemTrayService is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            var debugService = App.GetService<IDebugLoggingService>();
            if (debugService is IDisposable debugDisposable)
            {
                debugDisposable.Dispose();
            }
        }
        catch {}
    }

    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(() =>
        {
            TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
}
