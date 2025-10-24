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
/// ViewModel for GPU monitoring page.
/// Displays real-time GPU temperature with LiveCharts.
/// </summary>
public partial class GPUViewModel : ObservableRecipient, IDisposable
{
    private readonly IHardwareMonitorService _hardwareMonitor;
    private readonly IAppSettingsService _settingsService;
    private readonly IDebugLoggingService _debugLoggingService;
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
    private string _gpuName = "Loading...";

    [ObservableProperty]
    private double _gpuTemp;

    [ObservableProperty]
    private string _gpuTempFormatted = "0.0"; 

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

    public GPUViewModel(IHardwareMonitorService hardwareMonitor, IAppSettingsService settingsService, IDebugLoggingService debugLoggingService)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _debugLoggingService = debugLoggingService ?? throw new ArgumentNullException(nameof(debugLoggingService));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _settingsService.SettingsChanged += OnSettingsChanged;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _currentSettings = await _settingsService.GetSettingsAsync();
        _maxDataPoints = _currentSettings.MaxDataPoints;
        _updateIntervalMs = _currentSettings.UpdateIntervalMs;
        
        _debugLoggingService.Log("GPUViewModel initializing...");
        
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

        TemperatureUnitSymbol = TemperatureConverter.GetUnitSymbol(_currentSettings.TemperatureUnit);
        DecimalPrecision = _currentSettings.DecimalPrecision;

        UpdateChartSettings();

        if (oldInterval != _updateIntervalMs && !_disposed)
        {
            RestartTimer();
        }

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
            _debugLoggingService.Log($"GPU: Restarting timer with interval {_updateIntervalMs}ms");
            _timer?.Dispose();
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateIntervalMs));
            _monitoringTask = StartMonitoringAsync();
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"GPU ERROR: Failed to restart timer - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error restarting timer: {ex.Message}");
        }
    }

    private void InitializeCharts()
    {
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

        // X Axis
        _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
        {
            CustomSeparators = GetSeparators(),
            AnimationsSpeed = _currentSettings.EnableChartAnimations 
                ? TimeSpan.FromMilliseconds(500) 
                : TimeSpan.FromMilliseconds(0),
            SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(5))
        };

        XAxes = new ICartesianAxis[] { _customAxis };

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
            GpuName = _hardwareMonitor.GetGpuName();
            _hardwareMonitor.Update();

            var temp = _hardwareMonitor.GetGpuTemperature();
            if (temp.HasValue)
            {
                GpuTemp = TemperatureConverter.FromCelsius(temp.Value, _currentSettings.TemperatureUnit);
                _debugLoggingService.Log($"GPU: Initial temperature - {temp.Value:F1}°C");
            }
            _debugLoggingService.Log($"GPU: Detected hardware - {GpuName}");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"GPU ERROR: Failed to initialize - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error initializing GPU data: {ex.Message}");
            GpuName = "GPU Not Available";
        }
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            _debugLoggingService.Log("GPU: Monitoring loop started");
            while (_timer != null && await _timer.WaitForNextTickAsync() && !_disposed)
            {
                if (IsMonitoringActive)
                {
                    await Task.Run(() => UpdateSensorData());
                }
            }
            _debugLoggingService.Log("GPU: Monitoring loop ended");
        }
        catch (OperationCanceledException)
        {
            _debugLoggingService.Log("GPU: Monitoring loop cancelled");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"GPU ERROR: Monitoring loop exception - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error in GPU monitoring loop: {ex.Message}");
        }
    }

    partial void OnIsMonitoringActiveChanged(bool value)
    {
        _debugLoggingService.Log($"GPU: Monitoring active state changed to {value}");
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
            var tempCelsius = _hardwareMonitor.GetGpuTemperature();

            if (tempCelsius.HasValue)
            {
                var convertedTemp = TemperatureConverter.FromCelsius(tempCelsius.Value, _currentSettings.TemperatureUnit);
                
                _dispatcherQueue.TryEnqueue(() =>
                {
                    GpuTemp = convertedTemp;
                    GpuTempFormatted = convertedTemp.ToString($"F{_currentSettings.DecimalPrecision}");
                });
            }

            lock (Sync)
            {
                var tempForChart = tempCelsius.HasValue 
                    ? TemperatureConverter.FromCelsius(tempCelsius.Value, _currentSettings.TemperatureUnit)
                    : GpuTemp;
                    
                _temperatureValues.Add(new DateTimePoint(currentTime, tempForChart));
                
                if (_temperatureValues.Count > _maxDataPoints)
                {
                    _temperatureValues.RemoveAt(0);
                }

                _customAxis.CustomSeparators = GetSeparators();
            }
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"GPU ERROR: Sensor update failed - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error updating GPU sensor data: {ex.Message}");
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
        
        _settingsService.SettingsChanged -= OnSettingsChanged;
        
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
