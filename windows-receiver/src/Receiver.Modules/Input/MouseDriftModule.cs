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
        { "duration_seconds", 8 }
    };

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
        int durationSeconds = 8;
        if (context.Parameters.TryGetValue("duration_seconds", out var val))
        {
            if (val is int intVal) durationSeconds = intVal;
            else if (int.TryParse(val?.ToString(), out int parsed)) durationSeconds = parsed;
        }

        context.LogInfo($"Mouse drift module active for {durationSeconds} seconds...");

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(durationSeconds);
        double angle = 0;

        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            if (GetCursorPos(out POINT currentPos))
            {
                int deltaX = (int)Math.Round(Math.Sin(angle) * 2.5);
                int deltaY = (int)Math.Round(Math.Cos(angle * 1.5) * 2.0);

                SetCursorPos(currentPos.X + deltaX, currentPos.Y + deltaY);
            }

            angle += 0.25;
            await Task.Delay(40, cancellationToken);
        }
    }

    public Task RevertAsync()
    {
        return Task.CompletedTask;
    }
}
