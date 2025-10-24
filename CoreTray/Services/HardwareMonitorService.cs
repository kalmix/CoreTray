using System.Diagnostics;
using CoreTray.Contracts.Services;
using LibreHardwareMonitor.Hardware;

namespace CoreTray.Services;

/// <summary>
/// Hardware monitoring service implementation using LibreHardwareMonitor.
/// Implements IDisposable pattern for proper resource cleanup.
/// </summary>
public sealed class HardwareMonitorService : IHardwareMonitorService
{
    private readonly Computer _computer;
    private readonly object _updateLock = new();
    private bool _disposed;

    public HardwareMonitorService()
    {
        try
        {
            _computer = new Computer()
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = false,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsStorageEnabled = false
            };

            _computer.Open();
            Update(); // INIT UPDATE
            
            LogAvailableSensors(); // list all (debug)
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize hardware monitor: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Update()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HardwareMonitorService));
        }

        lock (_updateLock)
        {
            foreach (var hardware in _computer.Hardware)
            {
                try
                {
                    hardware.Update();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.WriteLine($"Error updating {hardware.Name}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error updating {hardware.Name}: {ex.Message}");
                }

                foreach (var subHardware in hardware.SubHardware)
                {
                    try
                    {
                        subHardware.Update();
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        Debug.WriteLine($"Error updating {subHardware.Name}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating {subHardware.Name}: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public string GetCpuName()
    {
        var cpu = GetCpuHardware();
        return cpu?.Name ?? "CPU Not Found";
    }

    /// <inheritdoc/>
    public float? GetCpuTemperature()
    {
        try
        {
            var cpu = GetCpuHardware();
            if (cpu == null) return null;

            // Lookup the maximum core temperature sensor
            var tempSensor = cpu.Sensors
                .Where(s => s.SensorType == SensorType.Temperature)
                .FirstOrDefault(s => s.Name.Contains("Core Max", StringComparison.OrdinalIgnoreCase) ||
                                   s.Name.Contains("CPU Package", StringComparison.OrdinalIgnoreCase) ||
                                   s.Name.Contains("Core (Tctl/Tdie)", StringComparison.OrdinalIgnoreCase));

            // If not found, fallback to any temperature sensor
            tempSensor ??= cpu.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature);

            return tempSensor?.Value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading CPU temperature: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public float? GetCpuUsage() // I'll use this later :)
    {
        try
        {
            var cpu = GetCpuHardware();
            if (cpu == null) return null;

            var loadSensor = cpu.Sensors
                .Where(s => s.SensorType == SensorType.Load)
                .FirstOrDefault(s => s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase));

            return loadSensor?.Value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading CPU usage: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public string GetGpuName()
    {
        var gpu = GetGpuHardware();
        return gpu?.Name ?? "GPU Not Found";
    }

    /// <inheritdoc/>
    public float? GetGpuTemperature()
    {
        try
        {
            var gpu = GetGpuHardware();
            if (gpu == null) return null;

            var tempSensor = gpu.Sensors
                .Where(s => s.SensorType == SensorType.Temperature)
                .FirstOrDefault(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                                   s.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase));

            return tempSensor?.Value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading GPU temperature: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public float? GetGpuUsage()
    {
        try
        {
            var gpu = GetGpuHardware();
            if (gpu == null) return null;

            var loadSensor = gpu.Sensors
                .Where(s => s.SensorType == SensorType.Load)
                .FirstOrDefault(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                                   s.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase));

            return loadSensor?.Value;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading GPU usage: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc/>
    public float? GetTotalRam()
    {
        var memory = GetMemoryHardware();
        if (memory == null) return null;

        var used = GetUsedRam();
        var available = GetAvailableRam();

        if (used.HasValue && available.HasValue)
        {
            return used.Value + available.Value;
        }

        return null;
    }

    /// <inheritdoc/>
    public float? GetUsedRam()
    {
        var memory = GetMemoryHardware();
        if (memory == null) return null;

        var usedSensor = memory.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Data && 
                               s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase));

        return usedSensor?.Value;
    }

    /// <inheritdoc/>
    public float? GetAvailableRam()
    {
        var memory = GetMemoryHardware();
        if (memory == null) return null;

        var availableSensor = memory.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Data && 
                               s.Name.Contains("Available", StringComparison.OrdinalIgnoreCase));

        return availableSensor?.Value;
    }

    /// <inheritdoc/>
    public float? GetRamUsagePercentage()
    {
        var memory = GetMemoryHardware();
        if (memory == null) return null;

        var loadSensor = memory.Sensors
            .FirstOrDefault(s => s.SensorType == SensorType.Load && 
                               s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase));

        return loadSensor?.Value;
    }

    #region Private Helper Methods

    private IHardware? GetCpuHardware()
    {
        return _computer.Hardware
            .FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
    }

    private IHardware? GetGpuHardware()
    {
        return _computer.Hardware
            .FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia || 
                               h.HardwareType == HardwareType.GpuAmd ||
                               h.HardwareType == HardwareType.GpuIntel);
    }

    private IHardware? GetMemoryHardware()
    {
        return _computer.Hardware
            .FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
    }

    private void LogAvailableSensors()
    {
        Debug.WriteLine("=== Available Hardware and Sensors ===");
        foreach (var hardware in _computer.Hardware)
        {
            Debug.WriteLine($"\nHardware: {hardware.Name} ({hardware.HardwareType})");
            
            try
            {
                foreach (var sensor in hardware.Sensors)
                {
                    Debug.WriteLine($"  Sensor: {sensor.Name} | Type: {sensor.SensorType} | Value: {sensor.Value}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"  Error reading sensors: {ex.Message}");
            }

            foreach (var subHardware in hardware.SubHardware)
            {
                Debug.WriteLine($"  SubHardware: {subHardware.Name} ({subHardware.HardwareType})");
                try
                {
                    foreach (var sensor in subHardware.Sensors)
                    {
                        Debug.WriteLine($"    Sensor: {sensor.Name} | Type: {sensor.SensorType} | Value: {sensor.Value}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"    Error reading sensors: {ex.Message}");
                }
            }
        }
        Debug.WriteLine("=== End of Available Sensors ===\n");
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_updateLock)
        {
            try
            {
                _computer.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing hardware monitor: {ex.Message}");
            }

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}
