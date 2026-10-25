using System.Diagnostics;
using Receiver.Core.Dispatcher;

namespace Receiver.Modules.Hardware;

public class FanSpeedModule : IPrankModule
{
    public string Id => "fan_speed";
    public string DisplayName => "Vent Turbo (Computer Fans)";
    public string Description => "Spins up computer fans and air vents to maximum speed via controlled thermal load.";
    public string Category => "Hardware";

    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "duration_seconds", 15 },
        { "intensity", 10 }
    };

    private volatile int _intensity = 10;
    private volatile bool _isRunning;
    private CancellationTokenSource? _linkedCts;

    public void UpdateParameters(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("intensity", out var intVal))
        {
            if (intVal is int i) _intensity = Math.Clamp(i, 1, 10);
            else if (int.TryParse(intVal?.ToString(), out int parsed)) _intensity = Math.Clamp(parsed, 1, 10);
        }
    }

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        int durationSeconds = 15;
        if (context.Parameters.TryGetValue("duration_seconds", out var dVal))
        {
            if (dVal is int i) durationSeconds = i;
            else if (int.TryParse(dVal?.ToString(), out int parsed)) durationSeconds = parsed;
        }

        UpdateParameters(context.Parameters);

        context.LogInfo($"Vent accelerator running for {durationSeconds}s at intensity {_intensity}/10 on {Environment.ProcessorCount} cores...");

        _isRunning = true;
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _linkedCts.Token;

        int coreCount = Math.Max(1, Environment.ProcessorCount);
        var tasks = new Task[coreCount];

        for (int i = 0; i < coreCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                // Multi-threaded thermal generator loop
                while (_isRunning && !token.IsCancellationRequested)
                {
                    int currentIntensity = _intensity;
                    if (currentIntensity >= 10)
                    {
                        // 100% duty cycle: full compute to quickly ramp CPU thermal curve and spin fans
                        var sw = Stopwatch.StartNew();
                        while (sw.ElapsedMilliseconds < 50 && _isRunning && !token.IsCancellationRequested)
                        {
                            _ = Math.Sqrt(98765.4321) * Math.Sin(12345.6789);
                        }
                    }
                    else
                    {
                        // Fractional duty cycle
                        int workMs = currentIntensity * 5;
                        int sleepMs = (10 - currentIntensity) * 5;

                        var sw = Stopwatch.StartNew();
                        while (sw.ElapsedMilliseconds < workMs && _isRunning && !token.IsCancellationRequested)
                        {
                            _ = Math.Sqrt(98765.4321) * Math.Sin(12345.6789);
                        }

                        if (sleepMs > 0 && _isRunning && !token.IsCancellationRequested)
                        {
                            Thread.Sleep(sleepMs);
                        }
                    }
                }
            }, token);
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), token);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        finally
        {
            await RevertAsync();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // Ignore task cancellations
            }
        }
    }

    public Task RevertAsync()
    {
        _isRunning = false;
        try
        {
            _linkedCts?.Cancel();
        }
        catch { }
        return Task.CompletedTask;
    }
}
