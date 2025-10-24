using CoreTray.Contracts.Services;
using Microsoft.UI.Xaml;
using System.Drawing;
using System.Runtime.InteropServices;
using WinRT;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace CoreTray.Services;

/// <summary>
/// Service for managing system tray icon with CPU temperature display.
/// </summary>
public class SystemTrayService : ISystemTrayService
{
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private bool _disposed;
    private bool _isInitialized;
    private string _currentTemperature = "--";
    private string _currentUnit = "°C";
    private readonly IDebugLoggingService _debugLoggingService;

    public SystemTrayService(IDebugLoggingService debugLoggingService)
    {
        _debugLoggingService = debugLoggingService ?? throw new ArgumentNullException(nameof(debugLoggingService));
    }

    public void Initialize()
    {
        if (_notifyIcon != null || _isInitialized)
        {
            return;
        }

        _debugLoggingService.Log("SystemTray: Initializing system tray icon");

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show CoreTray", null, (s, e) => ShowMainWindow());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "CoreTray - CPU Monitor",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        UpdateIcon(); // Init

        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        _isInitialized = true;
        _debugLoggingService.Log("SystemTray: Initialization complete");
    }

    public void UpdateTemperature(double temperature, string unitSymbol)
    {
        _currentTemperature = temperature.ToString("F0");
        _currentUnit = unitSymbol;
        
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = $"CoreTray - CPU: {_currentTemperature}{_currentUnit}";
            UpdateIcon();
        }
    }

    private void UpdateIcon()
    {
        try
        {
            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            
            graphics.Clear(Color.Transparent);
            
            using var font = new Font("Segoe UI", 7, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            
            var text = _currentTemperature;
            var size = graphics.MeasureString(text, font);
            var x = (16 - size.Width) / 2;
            var y = (16 - size.Height) / 2;
            
            using var bgBrush = new SolidBrush(Color.FromArgb(180, 33, 147, 237));
            graphics.FillRectangle(bgBrush, 0, 0, 16, 16);
            
            graphics.DrawString(text, font, brush, x, y);
            
            var hIcon = bitmap.GetHicon();
            using var icon = System.Drawing.Icon.FromHandle(hIcon);
            
            if (_notifyIcon != null)
            {
                _notifyIcon.Icon = (System.Drawing.Icon)icon.Clone();
            }
            
            DestroyIcon(hIcon);
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"SystemTray ERROR: Failed to update icon - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error updating tray icon: {ex.Message}");
        }
    }

    public void ShowMainWindow()
    {
        try
        {
            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            appWindow.Show();
            
            SetForegroundWindow(hwnd);
            
            _debugLoggingService.Log("SystemTray: Main window shown");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"SystemTray ERROR: Failed to show window - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error showing main window: {ex.Message}");
        }
    }

    public void HideMainWindow()
    {
        try
        {
            var window = App.MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            appWindow.Hide();
            
            _debugLoggingService.Log("SystemTray: Main window hidden");
        }
        catch (Exception ex)
        {
            _debugLoggingService.Log($"SystemTray ERROR: Failed to hide window - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error hiding main window: {ex.Message}");
        }
    }

    private void ExitApplication()
    {
        _debugLoggingService.Log("SystemTray: Exit requested");
        Application.Current.Exit();
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _isInitialized = false;
        _debugLoggingService.Log("SystemTray: Disposed");
        
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    // ... if you know you knowwwwwww :)
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
