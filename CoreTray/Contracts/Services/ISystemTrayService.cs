namespace CoreTray.Contracts.Services;

/// <summary>
/// Service for managing system tray icon and notifications.
/// </summary>
public interface ISystemTrayService : IDisposable
{
    /// <summary>
    /// Initializes the system tray icon.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Updates the system tray icon with current CPU temperature.
    /// </summary>
    /// <param name="temperature">Current CPU temperature</param>
    /// <param name="unitSymbol">Temperature unit symbol (°C, °F, K)</param>
    void UpdateTemperature(double temperature, string unitSymbol);

    /// <summary>
    /// Shows the main window.
    /// </summary>
    void ShowMainWindow();

    /// <summary>
    /// Hides the main window to system tray.
    /// </summary>
    void HideMainWindow();
}
