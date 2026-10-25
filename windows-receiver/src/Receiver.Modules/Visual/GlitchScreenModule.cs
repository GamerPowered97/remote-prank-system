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
        { "duration_seconds", 15 },
        { "intensity", 5 },
        { "speed", 5 }
    };

    private GlitchOverlayWindow? _window;
    private readonly object _lock = new();
    private volatile int _intensity = 5;
    private volatile int _speed = 5;

    public void UpdateParameters(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("intensity", out var iVal))
        {
            if (iVal is int i) _intensity = Math.Clamp(i, 1, 10);
            else if (int.TryParse(iVal?.ToString(), out int parsed)) _intensity = Math.Clamp(parsed, 1, 10);
        }

        if (parameters.TryGetValue("speed", out var sVal))
        {
            if (sVal is int s) _speed = Math.Clamp(s, 1, 10);
            else if (int.TryParse(sVal?.ToString(), out int parsed)) _speed = Math.Clamp(parsed, 1, 10);
        }

        lock (_lock)
        {
            if (_window != null && Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _window.UpdateSettings(_intensity, _speed);
                });
            }
        }
    }

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        int durationSeconds = 15;
        if (context.Parameters.TryGetValue("duration_seconds", out var val))
        {
            if (val is int intVal) durationSeconds = intVal;
            else if (int.TryParse(val?.ToString(), out int parsed)) durationSeconds = parsed;
        }

        UpdateParameters(context.Parameters);

        context.LogInfo($"Displaying screen glitch for {durationSeconds} seconds (Intensity: {_intensity}, Speed: {_speed})...");

        if (Application.Current != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _window?.StopAndClose();
                    _window = new GlitchOverlayWindow(_intensity, _speed);
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
