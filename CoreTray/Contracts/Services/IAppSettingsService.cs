using CoreTray.Models;

namespace CoreTray.Contracts.Services;

/// <summary>
/// Service interface for managing application settings.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    Task<AppSettings> GetSettingsAsync();

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to default values.
    /// </summary>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Event raised when settings are changed.
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;
}
