using CoreTray.Activation;
using CoreTray.Contracts.Services;
using CoreTray.Core.Contracts.Services;
using CoreTray.Core.Services;
using CoreTray.Helpers;
using CoreTray.Models;
using CoreTray.Services;
using CoreTray.ViewModels;
using CoreTray.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace CoreTray;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<IDebugLoggingService, DebugLoggingService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Hardware Monitoring Service
            services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
            
            // System Tray Service
            services.AddSingleton<ISystemTrayService, SystemTrayService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddSingleton<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddSingleton<RAMViewModel>();
            services.AddTransient<RAMPage>();
            services.AddSingleton<GPUViewModel>();
            services.AddTransient<GPUPage>();
            services.AddSingleton<CPUViewModel>();
            services.AddTransient<CPUPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).
        Build();

        UnhandledException += App_UnhandledException;
        
        // Ensure debug logging service is disposed when app exits
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        try
        {
            var debugService = GetService<IDebugLoggingService>();
            if (debugService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch { }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Load settings
        var settingsService = GetService<IAppSettingsService>();
        var settings = await settingsService.GetSettingsAsync();
        
        // Apply debug logging if enabled
        var debugService = GetService<IDebugLoggingService>();
        if (settings.EnableDebugLogging)
        {
            debugService.Enable();
            
            debugService.Log("=== Application Started ===");
            debugService.Log($"Update Interval: {settings.UpdateIntervalMs}ms");
            debugService.Log($"Max Data Points: {settings.MaxDataPoints}");
            debugService.Log($"Temperature Unit: {settings.TemperatureUnit}");
        }

        await App.GetService<IActivationService>().ActivateAsync(args);
    }
}
