using CoreTray.Contracts.Services;
using CoreTray.Models;

namespace CoreTray.Services;

/// <summary>
/// Service for managing application settings.
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private const string SettingsKey = "AppSettings";
    private readonly ILocalSettingsService _localSettingsService;
    private AppSettings? _cachedSettings;

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettingsService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        try
        {
            _cachedSettings = await _localSettingsService.ReadSettingAsync<AppSettings>(SettingsKey);
            
            if (_cachedSettings == null)
            {
                _cachedSettings = new AppSettings();
                await SaveSettingsAsync(_cachedSettings);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            _cachedSettings = new AppSettings();
        }

        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            await _localSettingsService.SaveSettingAsync(SettingsKey, settings);
            _cachedSettings = settings;
            SettingsChanged?.Invoke(this, settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            throw;
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        var defaultSettings = new AppSettings();
        await SaveSettingsAsync(defaultSettings);
    }
}
