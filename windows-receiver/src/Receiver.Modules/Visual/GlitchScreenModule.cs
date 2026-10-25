using System.Windows;
using Receiver.Core.Dispatcher;

namespace Receiver.Modules.Visual;

public class GlitchScreenModule : IPrankModule
{
    public string Id => "glitch_screen";
    public string DisplayName => "Screen Glitch Overlay";
    public string Description => "Renders animated retro CRT scanlines and glitch slices across the screen.";
    public string Category => "Visual";

    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "duration_seconds", 10 }
    };

    private GlitchOverlayWindow? _window;
    private readonly object _lock = new();

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        int durationSeconds = 10;
        if (context.Parameters.TryGetValue("duration_seconds", out var val))
        {
            if (val is int intVal) durationSeconds = intVal;
            else if (int.TryParse(val?.ToString(), out int parsed)) durationSeconds = parsed;
        }

        context.LogInfo($"Displaying screen glitch for {durationSeconds} seconds...");

        var tcs = new TaskCompletionSource();

        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _window?.StopAndClose();
                    _window = new GlitchOverlayWindow();
                    _window.Show();
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
            if (_window != null)
            {
                var win = _window;
                _window = null;
                if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
                {
                    win.StopAndClose();
                }
                else if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() => win.StopAndClose());
                }
            }
        }
        return Task.CompletedTask;
    }
}
