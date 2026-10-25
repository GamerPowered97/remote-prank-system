using System.ComponentModel;
using System.Text;
using System.Windows;
using Receiver.Core.Dispatcher;

namespace Receiver.App.Views;

public partial class LogViewerWindow : Window
{
    private readonly PrankDispatcher _dispatcher;
    private readonly StringBuilder _logBuffer = new();

    public LogViewerWindow(PrankDispatcher dispatcher)
    {
        InitializeComponent();
        _dispatcher = dispatcher;
        _dispatcher.LogEmitted += OnLogEmitted;
        _dispatcher.StatusChanged += OnStatusChanged;
    }

    private void OnStatusChanged(string? moduleId, string state, string message)
    {
        Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = $"Status: [{state}] {message} (Port 48888)";
        });
    }

    private void OnLogEmitted(string message)
    {
        Dispatcher.Invoke(() =>
        {
            _logBuffer.AppendLine(message);
            if (_logBuffer.Length > 20000)
            {
                _logBuffer.Remove(0, 5000);
            }
            TxtLogs.Text = _logBuffer.ToString();
            TxtLogs.ScrollToEnd();
        });
    }

    public void BringToForeground()
    {
        Show();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        ShowInTaskbar = true;
        Topmost = true;
        Activate();
        Topmost = false;
        Focus();
    }

    private async void BtnTestGlitch_Click(object sender, RoutedEventArgs e)
    {
        await _dispatcher.ExecuteAsync("glitch_screen", new Dictionary<string, object> { { "duration_seconds", 3 } });
    }

    private async void BtnTestDrift_Click(object sender, RoutedEventArgs e)
    {
        await _dispatcher.ExecuteAsync("mouse_drift", new Dictionary<string, object> { { "duration_seconds", 3 } });
    }

    private async void BtnTestFan_Click(object sender, RoutedEventArgs e)
    {
        await _dispatcher.ExecuteAsync("fan_speed", new Dictionary<string, object> { { "duration_seconds", 5 }, { "intensity", 8 } });
    }

    private async void BtnTestAlert_Click(object sender, RoutedEventArgs e)
    {
        await _dispatcher.ExecuteAsync("system_alert", new Dictionary<string, object> { { "title", "Test Alert" }, { "message", "System dialog test from local control panel." } });
    }

    private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
    {
        _logBuffer.Clear();
        TxtLogs.Text = string.Empty;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        ShowInTaskbar = false;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Don't close the process when log window is closed, just hide it
        e.Cancel = true;
        Hide();
        ShowInTaskbar = false;
    }
}
