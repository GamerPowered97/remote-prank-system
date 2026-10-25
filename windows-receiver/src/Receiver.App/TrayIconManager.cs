using System.Drawing;
using System.Windows.Forms;
using Receiver.App.Views;
using Receiver.Core.Dispatcher;

namespace Receiver.App;

public class TrayIconManager : IDisposable
{
    private readonly PrankDispatcher _dispatcher;
    private readonly LogViewerWindow _logViewerWindow;
    private readonly Action _exitCallback;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly ToolStripMenuItem _startupMenuItem;

    public TrayIconManager(PrankDispatcher dispatcher, LogViewerWindow logViewerWindow, Action exitCallback)
    {
        _dispatcher = dispatcher;
        _logViewerWindow = logViewerWindow;
        _exitCallback = exitCallback;

        _notifyIcon = new NotifyIcon
        {
            Text = "Remote Companion Receiver",
            Visible = true,
            Icon = CreateTrayIcon()
        };

        // Context menu
        var contextMenu = new ContextMenuStrip();

        _statusMenuItem = new ToolStripMenuItem("Status: Ready (Port 48888)") { Enabled = false };
        contextMenu.Items.Add(_statusMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());

        var showLogsItem = new ToolStripMenuItem("Show Status & Logs...", null, (s, e) =>
        {
            _logViewerWindow.Show();
            _logViewerWindow.Activate();
        });
        contextMenu.Items.Add(showLogsItem);

        _startupMenuItem = new ToolStripMenuItem("Start with Windows", null, OnToggleStartup)
        {
            Checked = StartupManager.IsStartupEnabled(),
            CheckOnClick = true
        };
        contextMenu.Items.Add(_startupMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var cancelItem = new ToolStripMenuItem("Cancel Active Effect", null, async (s, e) =>
        {
            await _dispatcher.StopActiveAsync("Cancelled by local user (Tray Menu)");
        });
        contextMenu.Items.Add(cancelItem);

        var exitItem = new ToolStripMenuItem("Exit Receiver", null, (s, e) =>
        {
            _exitCallback();
        });
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Double click: IMMEDIATE KILL SWITCH
        _notifyIcon.DoubleClick += async (s, e) =>
        {
            if (_dispatcher.IsRunning)
            {
                await _dispatcher.StopActiveAsync("Cancelled by local user (Tray Double-Click)");
                _notifyIcon.ShowBalloonTip(2000, "Remote Receiver", "Active effect aborted.", ToolTipIcon.Info);
            }
            else
            {
                _logViewerWindow.Show();
                _logViewerWindow.Activate();
            }
        };

        _dispatcher.StatusChanged += OnDispatcherStatusChanged;
    }

    private void OnDispatcherStatusChanged(string? moduleId, string state, string message)
    {
        string statusText = state switch
        {
            "RUNNING" => $"Running: {moduleId}",
            "STOPPED" => "Stopped",
            "COMPLETED" => "Ready (Last: Completed)",
            "ERROR" => "Error in effect",
            _ => "Status: Ready (Port 48888)"
        };

        if (_statusMenuItem != null)
        {
            _statusMenuItem.Text = statusText;
        }

        if (state == "RUNNING")
        {
            _notifyIcon.Text = $"Remote Receiver - Running {moduleId}";
        }
        else
        {
            _notifyIcon.Text = "Remote Companion Receiver";
        }
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        bool enable = _startupMenuItem.Checked;
        StartupManager.SetStartup(enable);
        _dispatcher.Log($"Start with Windows set to: {enable}");
    }

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static Icon CreateTrayIcon()
    {
        try
        {
            // Create a high-res 32x32 icon with anti-aliasing for modern High-DPI Windows taskbars
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Outer vibrant electric blue circle
                using var brush = new SolidBrush(Color.FromArgb(14, 165, 233));
                g.FillEllipse(brush, 1, 1, 30, 30);

                // Dark contrast ring
                using var darkBrush = new SolidBrush(Color.FromArgb(15, 23, 42));
                g.FillEllipse(darkBrush, 5, 5, 22, 22);

                // Center bright cyan target
                using var innerBrush = new SolidBrush(Color.FromArgb(56, 189, 248));
                g.FillEllipse(innerBrush, 10, 10, 12, 12);
            }

            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);
            return icon;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
