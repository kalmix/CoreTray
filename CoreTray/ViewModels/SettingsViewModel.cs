using System.Reflection;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CoreTray.Contracts.Services;
using CoreTray.Helpers;
using CoreTray.Models;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;

namespace CoreTray.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private const string SettingsKey = "AppSettings";
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IDebugLoggingService _debugLoggingService;
    private readonly ISystemTrayService _systemTrayService;
    private readonly CPUViewModel _cpuViewModel;
    private readonly GPUViewModel _gpuViewModel;
    private readonly RAMViewModel _ramViewModel;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    // Theme Index
    [ObservableProperty]
    private int _themeIndex;

    // Monitoring Settings
    [ObservableProperty]
    private int _updateInterval = 1000;

    [ObservableProperty]
    private bool _autoStartMonitoring = true;

    [ObservableProperty]
    private int _maxDataPoints = 250;

    // Display Settings
    [ObservableProperty]
    private TemperatureUnit _temperatureUnit = TemperatureUnit.Celsius;

    // Temperature Unit 
    [ObservableProperty]
    private int _temperatureUnitIndex;

    [ObservableProperty]
    private int _decimalPrecision = 1;

    [ObservableProperty]
    private bool _showCpuMonitoring = true;

    [ObservableProperty]
    private bool _showGpuMonitoring = true;

    [ObservableProperty]
    private bool _showRamMonitoring = true;

    // Chart Settings
    [ObservableProperty]
    private bool _enableChartAnimations = true;

    [ObservableProperty]
    private bool _enableChartSmoothing = true;

    [ObservableProperty]
    private double _chartSmoothness = 0.5;

    [ObservableProperty]
    private int _chartTimeWindow = 60;

    // Alert Settings
    [ObservableProperty]
    private bool _enableTemperatureAlerts = false;

    [ObservableProperty]
    private double _cpuWarningThreshold = 80.0;

    [ObservableProperty]
    private double _cpuCriticalThreshold = 90.0;

    [ObservableProperty]
    private double _gpuWarningThreshold = 80.0;

    [ObservableProperty]
    private double _gpuCriticalThreshold = 90.0;

    [ObservableProperty]
    private bool _showNotifications = true;

    // Startup Settings
    [ObservableProperty]
    private bool _launchAtStartup = false;

    [ObservableProperty]
    private bool _startMinimized = false;

    [ObservableProperty]
    private bool _minimizeToTray = false;

    // System Tray Settings
    [ObservableProperty]
    private bool _enableSystemTray = true;

    // Advanced Settings
    [ObservableProperty]
    private bool _enableDebugLogging = false;

    // Monitoring State
    [ObservableProperty]
    private bool _isMonitoringActive = true;

    public ICommand SwitchThemeCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand ResetAllGraphsCommand { get; }

    public SettingsViewModel(
        IThemeSelectorService themeSelectorService, 
        ILocalSettingsService localSettingsService, 
        IAppSettingsService appSettingsService,
        IDebugLoggingService debugLoggingService,
        ISystemTrayService systemTrayService,
        CPUViewModel cpuViewModel,
        GPUViewModel gpuViewModel,
        RAMViewModel ramViewModel)
    {
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _appSettingsService = appSettingsService;
        _debugLoggingService = debugLoggingService;
        _systemTrayService = systemTrayService;
        _cpuViewModel = cpuViewModel;
        _gpuViewModel = gpuViewModel;
        _ramViewModel = ramViewModel;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        // Set initial theme index
        _themeIndex = _elementTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            ElementTheme.Default => 2,
            _ => 2
        };

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        ResetSettingsCommand = new RelayCommand(async () => await ResetToDefaultsAsync());
        ResetAllGraphsCommand = new RelayCommand(ResetAllGraphs);

        _ = LoadSettingsAsync();
    }

    private void ResetAllGraphs()
    {
        _debugLoggingService.Log("Resetting all graph data...");
        _cpuViewModel.ClearGraphData();
        _gpuViewModel.ClearGraphData();
        _ramViewModel.ClearGraphData();
        _debugLoggingService.Log("All graph data cleared successfully");
    }

    partial void OnIsMonitoringActiveChanged(bool value)
    {
        _debugLoggingService.Log($"Monitoring state changed to: {(value ? "Active" : "Paused")}");
        // Sync monitoring state to all ViewModels
        _cpuViewModel.IsMonitoringActive = value;
        _gpuViewModel.IsMonitoringActive = value;
        _ramViewModel.IsMonitoringActive = value;
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _appSettingsService.GetSettingsAsync();
            if (settings != null)
            {
                // Monitoring Settings
                UpdateInterval = settings.UpdateIntervalMs;
                AutoStartMonitoring = settings.AutoStartMonitoring;
                MaxDataPoints = settings.MaxDataPoints;
                
                // Set initial monitoring state based on AutoStartMonitoring
                IsMonitoringActive = settings.AutoStartMonitoring;

                // Display Settings
                TemperatureUnit = settings.TemperatureUnit;
                TemperatureUnitIndex = (int)settings.TemperatureUnit;
                DecimalPrecision = settings.DecimalPrecision;

                // Chart Settings
                EnableChartAnimations = settings.EnableChartAnimations;
                EnableChartSmoothing = settings.EnableChartSmoothing;
                ChartSmoothness = settings.ChartSmoothness;
                ChartTimeWindow = settings.ChartTimeWindowSeconds;


                // System Tray Settings
                EnableSystemTray = settings.EnableSystemTray;

                // Advanced Settings
                EnableDebugLogging = settings.EnableDebugLogging;
                
                // Sync debug logging state
                if (EnableDebugLogging && !_debugLoggingService.IsEnabled)
                {
                    _debugLoggingService.Enable();
                }
            }
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"ERROR: Failed to load settings - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                // Monitoring Settings
                UpdateIntervalMs = UpdateInterval,
                AutoStartMonitoring = AutoStartMonitoring,

                MaxDataPoints = MaxDataPoints,

                // Display Settings
                TemperatureUnit = TemperatureUnit,
                DecimalPrecision = DecimalPrecision,

                // Chart Settings
                EnableChartAnimations = EnableChartAnimations,
                EnableChartSmoothing = EnableChartSmoothing,
                ChartSmoothness = ChartSmoothness,
                ChartTimeWindowSeconds = ChartTimeWindow,


                // System Tray Settings
                EnableSystemTray = EnableSystemTray,

                // Advanced Settings
                EnableDebugLogging = EnableDebugLogging
            };

            // Use AppSettingsService which will fire SettingsChanged event
            await _appSettingsService.SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"ERROR: Failed to save settings - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    private async Task ResetToDefaultsAsync()
    {
        var defaultSettings = new AppSettings();

        // Monitoring Settings
        UpdateInterval = defaultSettings.UpdateIntervalMs;
        AutoStartMonitoring = defaultSettings.AutoStartMonitoring;
        MaxDataPoints = defaultSettings.MaxDataPoints;

        // Display Settings
        TemperatureUnit = defaultSettings.TemperatureUnit;
        DecimalPrecision = defaultSettings.DecimalPrecision;

        // Chart Settings
        EnableChartAnimations = defaultSettings.EnableChartAnimations;
        EnableChartSmoothing = defaultSettings.EnableChartSmoothing;
        ChartSmoothness = defaultSettings.ChartSmoothness;
        ChartTimeWindow = defaultSettings.ChartTimeWindowSeconds;

        // System Tray Settings
        EnableSystemTray = defaultSettings.EnableSystemTray;

        // Advanced Settings
        EnableDebugLogging = defaultSettings.EnableDebugLogging;

        await SaveSettingsAsync();
    }

    partial void OnUpdateIntervalChanged(int value)
    {
        _debugLoggingService.Log($"Update Interval changed to: {value}ms");
        _ = SaveSettingsAsync();
    }
    
    partial void OnAutoStartMonitoringChanged(bool value)
    {
        _debugLoggingService.Log($"Auto-start Monitoring changed to: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnMaxDataPointsChanged(int value)
    {
        _debugLoggingService.Log($"Max Data Points changed to: {value}");
        _ = SaveSettingsAsync();
    }
    partial void OnTemperatureUnitChanged(TemperatureUnit value)
    {
        _debugLoggingService.Log($"Temperature Unit changed to: {value}");
        TemperatureUnitIndex = (int)value;
        _ = SaveSettingsAsync();
    }
    partial void OnTemperatureUnitIndexChanged(int value)
    {
        if (value >= 0 && value <= 2)
        {
            TemperatureUnit = (TemperatureUnit)value;
        }
    }
    partial void OnThemeIndexChanged(int value)
    {
        var newTheme = value switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            2 => ElementTheme.Default,
            _ => ElementTheme.Default
        };
        
        if (ElementTheme != newTheme)
        {
            _debugLoggingService.Log($"Theme changed to: {newTheme}");
            _ = _themeSelectorService.SetThemeAsync(newTheme);
            ElementTheme = newTheme;
        }
    }
    partial void OnDecimalPrecisionChanged(int value)
    {
        _debugLoggingService.Log($"Decimal Precision changed to: {value}");
        _ = SaveSettingsAsync();
    }
    partial void OnShowCpuMonitoringChanged(bool value)
    {
        _debugLoggingService.Log($"Show CPU Monitoring: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnShowGpuMonitoringChanged(bool value)
    {
        _debugLoggingService.Log($"Show GPU Monitoring: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnShowRamMonitoringChanged(bool value)
    {
        _debugLoggingService.Log($"Show RAM Monitoring: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnEnableChartAnimationsChanged(bool value)
    {
        _debugLoggingService.Log($"Chart Animations: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnEnableChartSmoothingChanged(bool value)
    {
        _debugLoggingService.Log($"Chart Smoothing: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnChartSmoothnessChanged(double value)
    {
        _debugLoggingService.Log($"Chart Smoothness: {value:F2}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnChartTimeWindowChanged(int value)
    {
        _debugLoggingService.Log($"Chart Time Window: {value}s");
        _ = SaveSettingsAsync();
    }
    
    partial void OnEnableTemperatureAlertsChanged(bool value)
    {
        _debugLoggingService.Log($"Temperature Alerts: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnCpuWarningThresholdChanged(double value)
    {
        _debugLoggingService.Log($"CPU Warning Threshold: {value}°C");
        _ = SaveSettingsAsync();
    }
    
    partial void OnCpuCriticalThresholdChanged(double value)
    {
        _debugLoggingService.Log($"CPU Critical Threshold: {value}°C");
        _ = SaveSettingsAsync();
    }
    
    partial void OnGpuWarningThresholdChanged(double value)
    {
        _debugLoggingService.Log($"GPU Warning Threshold: {value}°C");
        _ = SaveSettingsAsync();
    }
    
    partial void OnGpuCriticalThresholdChanged(double value)
    {
        _debugLoggingService.Log($"GPU Critical Threshold: {value}°C");
        _ = SaveSettingsAsync();
    }
    
    partial void OnShowNotificationsChanged(bool value)
    {
        _debugLoggingService.Log($"Show Notifications: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnLaunchAtStartupChanged(bool value)
    {
        _debugLoggingService.Log($"Launch at Startup: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnStartMinimizedChanged(bool value)
    {
        _debugLoggingService.Log($"Start Minimized: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnMinimizeToTrayChanged(bool value)
    {
        _debugLoggingService.Log($"Minimize to Tray: {value}");
        _ = SaveSettingsAsync();
    }
    
    partial void OnEnableSystemTrayChanged(bool value)
    {
        _debugLoggingService.Log($"System Tray Enabled: {value}");
        
        if (value)
        {
            _systemTrayService.Initialize();
        }
        else
        {
            _systemTrayService.Dispose();
        }
        
        _ = SaveSettingsAsync();
    }
    
    partial void OnEnableDebugLoggingChanged(bool value)
    {
        if (value)
        {
            _debugLoggingService.Enable();
        }
        else
        {
            _debugLoggingService.Disable();
        }
        _ = SaveSettingsAsync();
    }
    
    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
