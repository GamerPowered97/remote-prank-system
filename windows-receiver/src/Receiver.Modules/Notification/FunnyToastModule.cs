using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Receiver.Core.Dispatcher;

namespace Receiver.Modules.Notification;

public class FunnyToastModule : IPrankModule
{
    public string Id => "funny_toast";
    public string DisplayName => "Harmless System Alert";
    public string Description => "Displays an amusing system notification toast in the corner of the screen.";
    public string Category => "Notification";

    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "title", "System Diagnostic Alert" },
        { "message", "Warning: Keyboard velocity exceeded standard limits! Please hydrate the user." },
        { "duration_seconds", 6 }
    };

    private Window? _toastWindow;
    private readonly object _lock = new();

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        string title = "System Diagnostic Alert";
        if (context.Parameters.TryGetValue("title", out var tVal) && tVal is string sTitle)
        {
            title = sTitle;
        }

        string message = "Warning: Keyboard velocity exceeded standard limits! Please hydrate the user.";
        if (context.Parameters.TryGetValue("message", out var mVal) && mVal is string sMsg)
        {
            message = sMsg;
        }

        int durationSeconds = 6;
        if (context.Parameters.TryGetValue("duration_seconds", out var dVal))
        {
            if (dVal is int intVal) durationSeconds = intVal;
            else if (int.TryParse(dVal?.ToString(), out int parsed)) durationSeconds = parsed;
        }

        context.LogInfo($"Displaying toast: '{title}' - '{message}' for {durationSeconds}s");

        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _toastWindow?.Close();
                    _toastWindow = CreateToastWindow(title, message);
                    _toastWindow.Show();
                }
            });
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken);
        }
        finally
        {
            await RevertAsync();
        }
    }

    public Task RevertAsync()
    {
        lock (_lock)
        {
            if (_toastWindow != null)
            {
                var win = _toastWindow;
                _toastWindow = null;
                if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
                {
                    win.Close();
                }
                else if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() => win.Close());
                }
            }
        }
        return Task.CompletedTask;
    }

    private static Window CreateToastWindow(string title, string message)
    {
        var win = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            ShowActivated = false,
            Focusable = false,
            Width = 360,
            Height = 90,
            Left = SystemParameters.WorkArea.Right - 380,
            Top = SystemParameters.WorkArea.Bottom - 110
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 36)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(65, 105, 225)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10)
        };

        var stack = new StackPanel();

        var titleBlock = new TextBlock
        {
            Text = $"🔔 {title}",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var msgBlock = new TextBlock
        {
            Text = message,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(210, 210, 210)),
            TextWrapping = TextWrapping.Wrap
        };

        stack.Children.Add(titleBlock);
        stack.Children.Add(msgBlock);
        border.Child = stack;
        win.Content = border;

        return win;
    }
}
