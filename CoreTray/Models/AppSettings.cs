namespace CoreTray.Models;

/// <summary>
/// Application settings model for CoreTray hardware monitoring application.
/// Contains all user-configurable settings for monitoring, display, and performance.
/// </summary>
public class AppSettings
{
    // Monitoring Settings
    public int UpdateIntervalMs { get; set; } = 1000;
    public bool AutoStartMonitoring { get; set; } = true;
    public bool IsMonitoringRunning { get; set; } = true;
    public int MaxDataPoints { get; set; } = 250;
    
    // Display Settings
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;
    public int DecimalPrecision { get; set; } = 1;
    
    // Chart Settings
    public bool EnableChartAnimations { get; set; } = true;
    public bool EnableChartSmoothing { get; set; } = true;
    public double ChartSmoothness { get; set; } = 0.5;
    public int ChartTimeWindowSeconds { get; set; } = 60;
    
    // Advanced Settings
    public bool EnableDebugLogging { get; set; } = false;
    public bool EnableSystemTray { get; set; } = false;
    
    // Welcome Experience
    public bool HasSeenWelcomeDialog { get; set; } = false;
}

public enum TemperatureUnit
{
    Celsius,
    Fahrenheit,
    Kelvin
}
