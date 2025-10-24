using System.Diagnostics;
using System.IO;
using CoreTray.Contracts.Services;

namespace CoreTray.Services;

/// <summary>
/// Service for managing debug logging output to a Windows Terminal window.
/// Launches an external PowerShell window for displaying logs.
/// </summary>
public class DebugLoggingService : IDebugLoggingService, IDisposable
{
    private bool _isEnabled;
    private StreamWriter? _logWriter;
    private readonly string _logFilePath;
    private FileSystemWatcher? _fileWatcher;
    private Process? _viewerProcess;
    private bool _disposed;

    public DebugLoggingService()
    {
        // Create log file in temp directory
        _logFilePath = Path.Combine(Path.GetTempPath(), $"CoreTray_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    }

    public bool IsEnabled => _isEnabled;

    public void Enable()
    {
        if (_isEnabled)
        {
            return;
        }

        try
        {
            // Create log file stream
            var fileStream = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            _logWriter = new StreamWriter(fileStream, System.Text.Encoding.UTF8) 
            { 
                AutoFlush = true 
            };
            
            _logWriter.WriteLine("╔═══════════════════════════════════════════╗");
            _logWriter.WriteLine("║     CoreTray Debug Logging Enabled        ║");
            _logWriter.WriteLine("╚═══════════════════════════════════════════╝");
            _logWriter.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logWriter.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
            _logWriter.WriteLine($"Log File: {_logFilePath}");
            _logWriter.WriteLine();
            _logWriter.Flush();

            _isEnabled = true;

            if (_viewerProcess == null || _viewerProcess.HasExited)
            {
                LaunchLogViewer();
            }

            System.Diagnostics.Debug.WriteLine($"Debug logging enabled. Log file: {_logFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable debug logging: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void LaunchLogViewer()
    {
        try
        {
            if (!File.Exists(_logFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"Log file doesn't exist yet: {_logFilePath}");
                return;
            }

            // Escape the path for PowerShell
            var escapedPath = _logFilePath.Replace("'", "''");

            // PowerShell command to tail the log file
            var tailCommand = $"$Host.UI.RawUI.WindowTitle='CoreTray Debug Console'; Write-Host 'CoreTray Debug Console - Live Log' -ForegroundColor Cyan; Write-Host 'Watching: {escapedPath}' -ForegroundColor Gray; Write-Host ''; Get-Content -Path '{escapedPath}' -Wait -Tail 100 -Encoding UTF8";
            
            ProcessStartInfo psi;

            // 1. Try PowerShell Core (pwsh)
            try
            {
                psi = new ProcessStartInfo
                {
                    FileName = "pwsh.exe",
                    Arguments = $"-NoExit -Command \"{tailCommand}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                
                _viewerProcess = Process.Start(psi);
                System.Diagnostics.Debug.WriteLine($"Launched PowerShell viewer for log file");
                return;
            }
            catch
            {
                // 2. Fallback to Windows PowerShell
                try
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoExit -Command \"{tailCommand}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    
                    _viewerProcess = Process.Start(psi);
                    System.Diagnostics.Debug.WriteLine($"Launched Windows PowerShell viewer for log file");
                    return;
                }
                catch {}
            }

            // 3. Final fallback... just open in Notepad
            psi = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = _logFilePath,
                UseShellExecute = true
            };
            
            _viewerProcess = Process.Start(psi);
            System.Diagnostics.Debug.WriteLine($"Opened log file in Notepad");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to launch log viewer: {ex.Message}");
        }
    }

    public void Disable()
    {
        if (!_isEnabled)
        {
            return;
        }

        try
        {
            if (_logWriter != null)
            {
                _logWriter.WriteLine();
                _logWriter.WriteLine("===========================================");
                _logWriter.WriteLine("CoreTray Debug Logging Disabled");
                _logWriter.WriteLine($"Stopped: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logWriter.WriteLine("===========================================");
                _logWriter.Flush();
                _logWriter.Close();
                _logWriter.Dispose();
                _logWriter = null;
            }

            _fileWatcher?.Dispose();
            _fileWatcher = null;

            if (_viewerProcess != null && !_viewerProcess.HasExited)
            {
                try
                {
                    _viewerProcess.Kill();
                }
                catch {}
            }
            _viewerProcess = null;

            _isEnabled = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to disable debug logging: {ex.Message}");
        }
    }

    public void Log(string message)
    {
        if (!_isEnabled || _logWriter == null)
        {
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}";
            
            _logWriter.WriteLine(logLine);

            _logWriter.Flush();
            _logWriter.BaseStream.Flush();

            // Also output to VS Debug console
            System.Diagnostics.Debug.WriteLine(logLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to log message: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disable();
        _disposed = true;
    }
}
