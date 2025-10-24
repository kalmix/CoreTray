namespace CoreTray.Contracts.Services;

/// <summary>
/// Service for managing debug logging output to a console window.
/// </summary>
public interface IDebugLoggingService
{
    /// <summary>
    /// Gets whether debug logging is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables debug logging by allocating a console window.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disables debug logging by freeing the console window.
    /// </summary>
    void Disable();

    /// <summary>
    /// Logs a debug message to the console if enabled.
    /// </summary>
    void Log(string message);
}
