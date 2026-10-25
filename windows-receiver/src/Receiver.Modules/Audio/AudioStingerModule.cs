using System.Media;
using Receiver.Core.Dispatcher;

namespace Receiver.Modules.Audio;

public class AudioStingerModule : IPrankModule
{
    public string Id => "audio_stinger";
    public string DisplayName => "Playful Audio Chime";
    public string Description => "Plays a cheerful short musical chime sequence.";
    public string Category => "Audio";

    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "repeat_count", 2 }
    };

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        int repeats = 2;
        if (context.Parameters.TryGetValue("repeat_count", out var val))
        {
            if (val is int intVal) repeats = intVal;
            else if (int.TryParse(val?.ToString(), out int parsed)) repeats = parsed;
        }

        context.LogInfo($"Audio chime active ({repeats} repeats)...");

        // Frequencies for a friendly chime: C5, E5, G5, C6
        int[] notes = { 523, 659, 784, 1046 };

        for (int r = 0; r < repeats; r++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            foreach (var freq in notes)
            {
                if (cancellationToken.IsCancellationRequested) break;
                try
                {
                    Console.Beep(freq, 120);
                }
                catch
                {
                    // Fallback to system sounds if Console.Beep is unavailable
                    SystemSounds.Asterisk.Play();
                    await Task.Delay(150, cancellationToken);
                }
                await Task.Delay(40, cancellationToken);
            }

            await Task.Delay(400, cancellationToken);
        }
    }

    public Task RevertAsync()
    {
        return Task.CompletedTask;
    }
}
