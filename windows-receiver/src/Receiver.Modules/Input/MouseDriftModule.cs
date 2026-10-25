using System.Runtime.InteropServices;
using Receiver.Core.Dispatcher;

namespace Receiver.Modules.Input;

public class MouseDriftModule : IPrankModule
{
    public string Id => "mouse_drift";
    public string DisplayName => "Subtle Mouse Drift";
    public string Description => "Gently nudges mouse cursor in subtle waves for testing cursor dynamics.";
    public string Category => "Input";

    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "duration_seconds", 15 },
        { "speed", 5 },
        { "strength", 5 }
    };

    private volatile int _speed = 5;
    private volatile int _strength = 5;

    public void UpdateParameters(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("speed", out var sVal))
        {
            if (sVal is int s) _speed = Math.Clamp(s, 1, 10);
            else if (int.TryParse(sVal?.ToString(), out int parsed)) _speed = Math.Clamp(parsed, 1, 10);
        }

        if (parameters.TryGetValue("strength", out var strVal))
        {
            if (strVal is int str) _strength = Math.Clamp(str, 1, 10);
            else if (int.TryParse(strVal?.ToString(), out int parsed)) _strength = Math.Clamp(parsed, 1, 10);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        int durationSeconds = 15;
        if (context.Parameters.TryGetValue("duration_seconds", out var val))
        {
            if (val is int intVal) durationSeconds = intVal;
            else if (int.TryParse(val?.ToString(), out int parsed)) durationSeconds = parsed;
        }

        UpdateParameters(context.Parameters);

        context.LogInfo($"Mouse drift module active for {durationSeconds} seconds (Speed: {_speed}, Strength: {_strength})...");

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(durationSeconds);
        double angle = 0;

        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (GetCursorPos(out POINT currentPos))
            {
                double amp = 1.0 + (_strength * 1.2);
                int deltaX = (int)Math.Round(Math.Sin(angle) * amp);
                int deltaY = (int)Math.Round(Math.Cos(angle * 1.5) * (amp * 0.8));

                SetCursorPos(currentPos.X + deltaX, currentPos.Y + deltaY);
            }

            double angleStep = 0.05 + (_speed * 0.05);
            angle += angleStep;
            await Task.Delay(30, cancellationToken);
        }
    }

    public Task RevertAsync()
    {
        return Task.CompletedTask;
    }
}
