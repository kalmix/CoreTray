using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreTray.Contracts.Services;
using CoreTray.Helpers;
using CoreTray.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Dispatching;
using SkiaSharp;

namespace CoreTray.ViewModels;

/// <summary>
/// ViewModel for CPU monitoring page.
/// Displays real-time CPU temperature with LiveCharts.
/// </summary>
public partial class CPUViewModel : ObservableRecipient, IDisposable
{
    private readonly IHardwareMonitorService _hardwareMonitor;
    private readonly IAppSettingsService _settingsService;
    private readonly IDebugLoggingService _debugLoggingService;
    private readonly ISystemTrayService _systemTrayService;
    private PeriodicTimer? _timer;
    private readonly ObservableCollection<DateTimePoint> _temperatureValues = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private DateTimeAxis _customAxis = null!;
    private bool _disposed;
    private Task? _monitoringTask;
    private AppSettings _currentSettings = new();
    private int _maxDataPoints = 250;
    private int _updateIntervalMs = 1000;
    private LineSeries<DateTimePoint>? _lineSeries;

    [ObservableProperty]
    private string _cpuName = "Loading...";

    [ObservableProperty]
    private double _cpuTemp;

    [ObservableProperty]
    private string _cpuTempFormatted = "0.0";

    [ObservableProperty]
    private string _temperatureUnitSymbol = "°C";

    [ObservableProperty]
    private int _decimalPrecision = 1;

    [ObservableProperty]
    private bool _isMonitoringActive;

    [ObservableProperty]
    private ObservableCollection<ISeries> _temperatureSeries = new();

    [ObservableProperty]
    private ICartesianAxis[] _xAxes = Array.Empty<ICartesianAxis>();

    [ObservableProperty]
    private ICartesianAxis[] _yAxesTemperature = Array.Empty<ICartesianAxis>();

    public object Sync { get; } = new();

    public CPUViewModel(IHardwareMonitorService hardwareMonitor, IAppSettingsService settingsService, IDebugLoggingService debugLoggingService, ISystemTrayService systemTrayService)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _debugLoggingService = debugLoggingService ?? throw new ArgumentNullException(nameof(debugLoggingService));
        _systemTrayService = systemTrayService ?? throw new ArgumentNullException(nameof(systemTrayService));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Subscribe to settings changes
        _settingsService.SettingsChanged += OnSettingsChanged;

        // Load settings and initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _currentSettings = await _settingsService.GetSettingsAsync();
        _maxDataPoints = _currentSettings.MaxDataPoints;
        _updateIntervalMs = _currentSettings.UpdateIntervalMs;
        
        _debugLoggingService.Log("CPUViewModel initializing...");
        
        // Update temperature unit symbol
        TemperatureUnitSymbol = TemperatureConverter.GetUnitSymbol(_currentSettings.TemperatureUnit);
        DecimalPrecision = _currentSettings.DecimalPrecision;
        
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateIntervalMs));

        InitializeCharts();
        InitializeData();
        
        if (_currentSettings.AutoStartMonitoring)
        {
            IsMonitoringActive = true;
            _monitoringTask = StartMonitoringAsync();
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        var oldInterval = _updateIntervalMs;
        var oldMaxPoints = _maxDataPoints;
        
        _currentSettings = settings;
        _maxDataPoints = settings.MaxDataPoints;
        _updateIntervalMs = settings.UpdateIntervalMs;

        // Update temperature unit symbol and decimal precision
        TemperatureUnitSymbol = TemperatureConverter.GetUnitSymbol(_currentSettings.TemperatureUnit);
        DecimalPrecision = _currentSettings.DecimalPrecision;

        // Update chart settings
        UpdateChartSettings();

        // Restart timer if interval changed
        if (oldInterval != _updateIntervalMs && !_disposed)
        {
            RestartTimer();
        }

        // Trim data points if max changed
        if (oldMaxPoints != _maxDataPoints)
        {
            lock (Sync)
            {
                while (_temperatureValues.Count > _maxDataPoints)
                {
                    _temperatureValues.RemoveAt(0);
                }
            }
        }
    }

    private void RestartTimer()
    {
        try
        {
            _debugLoggingService.Log($"CPU: Restarting timer with interval {_updateIntervalMs}ms");
            _timer?.Dispose();
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateIntervalMs));
            _monitoringTask = StartMonitoringAsync();
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"CPU ERROR: Failed to restart timer - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error restarting timer: {ex.Message}");
        }
    }

    private void InitializeCharts()
    {
        // Temperature Chart Series
        _lineSeries = new LineSeries<DateTimePoint>
        {
            Values = _temperatureValues,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 },
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(50)),
            LineSmoothness = _currentSettings.EnableChartSmoothing ? _currentSettings.ChartSmoothness : 0
        };

        TemperatureSeries = new ObservableCollection<ISeries> { _lineSeries };

        // X Axis Configuration
        _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
        {
            CustomSeparators = GetSeparators(),
            AnimationsSpeed = _currentSettings.EnableChartAnimations 
                ? TimeSpan.FromMilliseconds(500) 
                : TimeSpan.FromMilliseconds(0),
            SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(5))
        };

        XAxes = new ICartesianAxis[] { _customAxis };

        // Y Axes Configuration - Temperature unit aware
        var unitSymbol = TemperatureConverter.GetUnitSymbol(_currentSettings.TemperatureUnit);
        var precision = _currentSettings.DecimalPrecision;
        
        YAxesTemperature = new ICartesianAxis[]
        {
            new Axis
            {
                Labeler = value => $"{value.ToString($"F{precision}")}{unitSymbol}",
                MinLimit = 0,
                MaxLimit = _currentSettings.TemperatureUnit == Models.TemperatureUnit.Fahrenheit ? 212 : 100,
                CustomSeparators = _currentSettings.TemperatureUnit == Models.TemperatureUnit.Fahrenheit 
                    ? new double[] { 0, 50, 100, 150, 200 }
                    : new double[] { 0, 20, 40, 60, 80, 100 },
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50))
            }
        };
    }

    private void UpdateChartSettings()
    {
        if (_lineSeries != null)
        {
            _lineSeries.LineSmoothness = _currentSettings.EnableChartSmoothing ? _currentSettings.ChartSmoothness : 0;
        }

        if (_customAxis != null)
        {
            _customAxis.AnimationsSpeed = _currentSettings.EnableChartAnimations 
                ? TimeSpan.FromMilliseconds(500) 
                : TimeSpan.FromMilliseconds(0);
        }

        // Update Y axis for temperature unit and precision
        var unitSymbol = TemperatureConverter.GetUnitSymbol(_currentSettings.TemperatureUnit);
        var precision = _currentSettings.DecimalPrecision;
        
        YAxesTemperature = new ICartesianAxis[]
        {
            new Axis
            {
                Labeler = value => $"{value.ToString($"F{precision}")}{unitSymbol}",
                MinLimit = 0,
                MaxLimit = _currentSettings.TemperatureUnit == Models.TemperatureUnit.Fahrenheit ? 212 : 100,
                CustomSeparators = _currentSettings.TemperatureUnit == Models.TemperatureUnit.Fahrenheit 
                    ? new double[] { 0, 50, 100, 150, 200 }
                    : new double[] { 0, 20, 40, 60, 80, 100 },
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50))
            }
        };
    }

    private void InitializeData()
    {
        try
        {
            CpuName = _hardwareMonitor.GetCpuName();
            _debugLoggingService.Log($"CPU: Detected hardware - {CpuName}");
            _hardwareMonitor.Update();

            var temp = _hardwareMonitor.GetCpuTemperature();
            CpuTemp = temp ?? 0;
            _debugLoggingService.Log($"CPU: Initial temperature - {CpuTemp:F1}°C");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"CPU ERROR: Failed to initialize - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error initializing CPU data: {ex.Message}");
            CpuName = "CPU Not Available";
        }
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            _debugLoggingService.Log("CPU: Monitoring loop started");
            while (_timer != null && await _timer.WaitForNextTickAsync() && !_disposed)
            {
                // Check if monitoring is active before updating
                if (IsMonitoringActive)
                {
                    // Run sensor update on background thread to prevent blocking UI
                    await Task.Run(() => UpdateSensorData());
                }
            }
            _debugLoggingService.Log("CPU: Monitoring loop ended");
        }
        catch (OperationCanceledException)
        {
            // Timer cancelled during disposal
            _debugLoggingService.Log("CPU: Monitoring loop cancelled");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"CPU ERROR: Monitoring loop exception - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error in CPU monitoring loop: {ex.Message}");
        }
    }

    partial void OnIsMonitoringActiveChanged(bool value)
    {
        _debugLoggingService.Log($"CPU: Monitoring active state changed to {value}");
        if (value && _monitoringTask == null)
        {
            _monitoringTask = StartMonitoringAsync();
        }
    }

    private void UpdateSensorData()
    {
        try
        {
            _hardwareMonitor.Update();

            var currentTime = DateTime.Now;
            var tempCelsius = _hardwareMonitor.GetCpuTemperature();

            if (tempCelsius.HasValue)
            {
                // Convert temperature to selected unit
                var convertedTemp = TemperatureConverter.FromCelsius(tempCelsius.Value, _currentSettings.TemperatureUnit);
                
                // Update UI property on UI thread
                _dispatcherQueue.TryEnqueue(() =>
                {
                    CpuTemp = convertedTemp;
                    CpuTempFormatted = convertedTemp.ToString($"F{_currentSettings.DecimalPrecision}");
                    
                    // Update system tray
                    _systemTrayService.UpdateTemperature(convertedTemp, TemperatureUnitSymbol);
                });
            }

            lock (Sync)
            {
                // Store in Celsius for chart (will be converted by axis labeler)
                var tempForChart = tempCelsius.HasValue 
                    ? TemperatureConverter.FromCelsius(tempCelsius.Value, _currentSettings.TemperatureUnit)
                    : CpuTemp;
                    
                _temperatureValues.Add(new DateTimePoint(currentTime, tempForChart));
                
                // Use settings-based max data points
                if (_temperatureValues.Count > _maxDataPoints)
                {
                    _temperatureValues.RemoveAt(0);
                }

                _customAxis.CustomSeparators = GetSeparators();
            }
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"CPU ERROR: Sensor update failed - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error updating CPU sensor data: {ex.Message}");
        }
    }

    private double[] GetSeparators()
    {
        var now = DateTime.Now;
        return new double[] { now.AddSeconds(-60).Ticks, now.AddSeconds(-30).Ticks, now.Ticks };
    }

    private static string Formatter(DateTime date)
    {
        var secondsAgo = (DateTime.Now - date).TotalSeconds;
        return secondsAgo switch
        {
            < 1 => "now",
            < 60 => $"{secondsAgo:F0}s",
            _ => $"{secondsAgo / 60:F0}m"
        };
    }

    /// <summary>
    /// Clears all historical temperature data from the chart.
    /// </summary>
    public void ClearGraphData()
    {
        lock (Sync)
        {
            _temperatureValues.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        
        // Unsubscribe from settings changes
        _settingsService.SettingsChanged -= OnSettingsChanged;
        
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
