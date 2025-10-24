using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreTray.Contracts.Services;
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
/// ViewModel for RAM monitoring page.
/// Displays real-time RAM usage with LiveCharts.
/// </summary>
public partial class RAMViewModel : ObservableRecipient, IDisposable
{
    private readonly IHardwareMonitorService _hardwareMonitor;
    private readonly IAppSettingsService _settingsService;
    private readonly IDebugLoggingService _debugLoggingService;
    private PeriodicTimer? _timer;
    private readonly ObservableCollection<DateTimePoint> _usageValues = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private DateTimeAxis _customAxis = null!;
    private bool _disposed;
    private Task? _monitoringTask;
    private AppSettings _currentSettings = new();
    private int _maxDataPoints = 250;
    private int _updateIntervalMs = 1000;
    private LineSeries<DateTimePoint>? _lineSeries;

    [ObservableProperty]
    private double _totalRam;

    [ObservableProperty]
    private string _totalRamFormatted = "0.0";

    [ObservableProperty]
    private double _usedRam;

    [ObservableProperty]
    private string _usedRamFormatted = "0.0";

    [ObservableProperty]
    private double _ramUsagePercentage;

    [ObservableProperty]
    private string _ramUsagePercentageFormatted = "0.0";

    [ObservableProperty]
    private int _decimalPrecision = 1;

    [ObservableProperty]
    private bool _isMonitoringActive;

    [ObservableProperty]
    private ObservableCollection<ISeries> _usageSeries = new();

    [ObservableProperty]
    private ICartesianAxis[] _xAxes = Array.Empty<ICartesianAxis>();

    [ObservableProperty]
    private ICartesianAxis[] _yAxesUsage = Array.Empty<ICartesianAxis>();

    public object Sync { get; } = new();

    public RAMViewModel(IHardwareMonitorService hardwareMonitor, IAppSettingsService settingsService, IDebugLoggingService debugLoggingService)
    {
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _debugLoggingService = debugLoggingService ?? throw new ArgumentNullException(nameof(debugLoggingService));
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Subscribe to settings
        _settingsService.SettingsChanged += OnSettingsChanged;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _currentSettings = await _settingsService.GetSettingsAsync();
        _maxDataPoints = _currentSettings.MaxDataPoints;
        _updateIntervalMs = _currentSettings.UpdateIntervalMs;
        
        _debugLoggingService.Log("RAMViewModel initializing...");
        
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


        DecimalPrecision = _currentSettings.DecimalPrecision;

        // Chart Settings
        UpdateChartSettings();

        
        if (oldInterval != _updateIntervalMs && !_disposed)
        {
            RestartTimer();
        }

 
        if (oldMaxPoints != _maxDataPoints)
        {
            lock (Sync)
            {
                while (_usageValues.Count > _maxDataPoints)
                {
                    _usageValues.RemoveAt(0);
                }
            }
        }
    }

    private void RestartTimer()
    {
        try
        {
            _debugLoggingService.Log($"RAM: Restarting timer with interval {_updateIntervalMs}ms");
            _timer?.Dispose();
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateIntervalMs));
            _monitoringTask = StartMonitoringAsync();
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"RAM ERROR: Failed to restart timer - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error restarting timer: {ex.Message}");
        }
    }

    private void InitializeCharts()
    {
        _lineSeries = new LineSeries<DateTimePoint>
        {
            Values = _usageValues,
            GeometryFill = null,
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 },
            Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(50)),
            LineSmoothness = _currentSettings.EnableChartSmoothing ? _currentSettings.ChartSmoothness : 0
        };

        UsageSeries = new ObservableCollection<ISeries> { _lineSeries };

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

        var precision = _currentSettings.DecimalPrecision;
        
        YAxesUsage = new ICartesianAxis[]
        {
            new Axis
            {
                Labeler = value => $"{value.ToString($"F{precision}")}%",
                MinLimit = 0,
                MaxLimit = 100,
                CustomSeparators = new double[] { 0, 20, 40, 60, 80, 100 },
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

        
        var precision = _currentSettings.DecimalPrecision;
        
        YAxesUsage = new ICartesianAxis[]
        {
            new Axis
            {
                Labeler = value => $"{value.ToString($"F{precision}")}%",
                MinLimit = 0,
                MaxLimit = 100,
                CustomSeparators = new double[] { 0, 20, 40, 60, 80, 100 },
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50))
            }
        };
    }

    private void InitializeData()
    {
        try
        {
            _hardwareMonitor.Update();

            var total = _hardwareMonitor.GetTotalRam();
            TotalRam = total ?? 0;
            TotalRamFormatted = TotalRam.ToString($"F{_currentSettings.DecimalPrecision}");

            var used = _hardwareMonitor.GetUsedRam();
            UsedRam = used ?? 0;
            UsedRamFormatted = UsedRam.ToString($"F{_currentSettings.DecimalPrecision}");

            var usage = _hardwareMonitor.GetRamUsagePercentage();
            RamUsagePercentage = usage ?? 0;
            RamUsagePercentageFormatted = RamUsagePercentage.ToString($"F{_currentSettings.DecimalPrecision}");
            _debugLoggingService.Log($"RAM: Initial usage - {RamUsagePercentage:F1}%, Total: {TotalRam:F1}GB, Used: {UsedRam:F1}GB");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"RAM ERROR: Failed to initialize - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error initializing RAM data: {ex.Message}");
        }
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            _debugLoggingService.Log("RAM: Monitoring loop started");
            while (_timer != null && await _timer.WaitForNextTickAsync() && !_disposed)
            {
                if (IsMonitoringActive)
                {
                    // Run sensor update on background thread
                    await Task.Run(() => UpdateSensorData());
                }
            }
            _debugLoggingService.Log("RAM: Monitoring loop ended");
        }
        catch (OperationCanceledException)
        {
            _debugLoggingService.Log("RAM: Monitoring loop cancelled");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"RAM ERROR: Monitoring loop exception - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error in RAM monitoring loop: {ex.Message}");
        }
    }

    partial void OnIsMonitoringActiveChanged(bool value)
    {
        _debugLoggingService.Log($"RAM: Monitoring active state changed to {value}");
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
            var used = _hardwareMonitor.GetUsedRam();
            var usage = _hardwareMonitor.GetRamUsagePercentage();

            if (used.HasValue)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    UsedRam = used.Value;
                    UsedRamFormatted = used.Value.ToString($"F{_currentSettings.DecimalPrecision}");
                });
            }

            if (usage.HasValue)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    RamUsagePercentage = usage.Value;
                    RamUsagePercentageFormatted = usage.Value.ToString($"F{_currentSettings.DecimalPrecision}");
                });
            }

            var total = _hardwareMonitor.GetTotalRam();
            if (total.HasValue && Math.Abs(total.Value - TotalRam) > 0.01)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    TotalRam = total.Value;
                    TotalRamFormatted = total.Value.ToString($"F{_currentSettings.DecimalPrecision}");
                });
            }

            lock (Sync)
            {
                _usageValues.Add(new DateTimePoint(currentTime, usage ?? RamUsagePercentage));
                if (_usageValues.Count > _maxDataPoints)
                {
                    _usageValues.RemoveAt(0);
                }

                _customAxis.CustomSeparators = GetSeparators();
            }
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"RAM ERROR: Sensor update failed - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error updating RAM sensor data: {ex.Message}");
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
            _usageValues.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        
        // Unsubscribe from settings changes, duh!
        _settingsService.SettingsChanged -= OnSettingsChanged;
        
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
