using LibreHardwareMonitor.Hardware;

namespace CoreTray.Contracts.Services;

/// <summary>
/// Service interface for hardware monitoring operations.
/// Provides access to CPU, GPU, and RAM sensor data.
/// </summary>
public interface IHardwareMonitorService : IDisposable
{
    /// <summary>
    /// Updates all hardware sensors with latest values.
    /// </summary>
    void Update();

    /// <summary>
    /// Gets the CPU name/model.
    /// </summary>
    string GetCpuName();

    /// <summary>
    /// Gets the maximum CPU core temperature in Celsius.
    /// </summary>
    /// <returns>Temperature value or null if not available.</returns>
    float? GetCpuTemperature();

    /// <summary>
    /// Gets the CPU total usage percentage (0-100).
    /// </summary>
    /// <returns>Usage percentage or null if not available.</returns>
    float? GetCpuUsage();

    /// <summary>
    /// Gets the GPU name/model.
    /// </summary>
    string GetGpuName();

    /// <summary>
    /// Gets the GPU core temperature in Celsius.
    /// </summary>
    /// <returns>Temperature value or null if not available.</returns>
    float? GetGpuTemperature();

    /// <summary>
    /// Gets the GPU core usage percentage (0-100).
    /// </summary>
    /// <returns>Usage percentage or null if not available.</returns>
    float? GetGpuUsage();

    /// <summary>
    /// Gets the total physical RAM in GB.
    /// </summary>
    float? GetTotalRam();

    /// <summary>
    /// Gets the used RAM in GB.
    /// </summary>
    float? GetUsedRam();

    /// <summary>
    /// Gets the available RAM in GB.
    /// </summary>
    float? GetAvailableRam();

    /// <summary>
    /// Gets the RAM usage percentage (0-100).
    /// </summary>
    float? GetRamUsagePercentage();
}
