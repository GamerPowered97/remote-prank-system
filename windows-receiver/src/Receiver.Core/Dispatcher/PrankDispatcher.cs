using System.Collections.Concurrent;

namespace Receiver.Core.Dispatcher;

public class PrankDispatcher
{
    private readonly ConcurrentDictionary<string, IPrankModule> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private CancellationTokenSource? _currentCts;
    private IPrankModule? _currentModule;
    private Task? _currentTask;

    public event Action<string? /*moduleId*/, string /*state*/, string /*message*/>? StatusChanged;
    public event Action<string /*message*/>? LogEmitted;

    public IReadOnlyCollection<IPrankModule> Modules => _modules.Values.ToList();

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _currentTask != null && !_currentTask.IsCompleted;
            }
        }
    }

    public string? ActiveModuleId
    {
        get
        {
            lock (_lock)
            {
                return IsRunning ? _currentModule?.Id : null;
            }
        }
    }

    public void RegisterModule(IPrankModule module)
    {
        _modules[module.Id] = module;
    }

    public void Log(string message)
    {
        LogEmitted?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public Task<bool> ExecuteAsync(string moduleId, Dictionary<string, object>? parameters = null)
    {
        IPrankModule module;
        CancellationTokenSource cts;

        lock (_lock)
        {
            if (IsRunning)
            {
                Log($"Cannot execute '{moduleId}': module '{_currentModule?.Id}' is already running.");
                return Task.FromResult(false);
            }

            if (!_modules.TryGetValue(moduleId, out var foundModule))
            {
                Log($"Module '{moduleId}' not found.");
                return Task.FromResult(false);
            }

            module = foundModule;
            _currentCts = new CancellationTokenSource();
            _currentModule = module;
            cts = _currentCts;
        }

        StatusChanged?.Invoke(module.Id, "RUNNING", $"Module '{module.DisplayName}' started.");
        Log($"Started module: {module.DisplayName} ({module.Id})");

        var paramDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in module.DefaultParameters)
        {
            paramDict[kv.Key] = kv.Value;
        }
        if (parameters != null)
        {
            foreach (var kv in parameters)
            {
                paramDict[kv.Key] = kv.Value;
            }
        }

        var context = new PrankContext(paramDict, Log);

        _currentTask = Task.Run(async () =>
        {
            try
            {
                await module.ExecuteAsync(context, cts.Token);
                StatusChanged?.Invoke(module.Id, "COMPLETED", $"Module '{module.DisplayName}' completed.");
                Log($"Module '{module.DisplayName}' completed naturally.");
            }
            catch (OperationCanceledException)
            {
                Log($"Module '{module.DisplayName}' was cancelled.");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(module.Id, "ERROR", $"Error in '{module.DisplayName}': {ex.Message}");
                Log($"Module '{module.DisplayName}' threw exception: {ex}");
            }
            finally
            {
                try
                {
                    await module.RevertAsync();
                }
                catch (Exception ex)
                {
                    Log($"Error reverting '{module.DisplayName}': {ex.Message}");
                }

                lock (_lock)
                {
                    if (_currentModule == module)
                    {
                        _currentModule = null;
                        _currentCts?.Dispose();
                        _currentCts = null;
                    }
                }
            }
        });

        return Task.FromResult(true);
    }

    public async Task StopActiveAsync(string reason = "Stopped by user")
    {
        CancellationTokenSource? cts;
        IPrankModule? module;
        Task? task;

        lock (_lock)
        {
            cts = _currentCts;
            module = _currentModule;
            task = _currentTask;
        }

        if (cts != null && !cts.IsCancellationRequested)
        {
            Log($"Stopping active module '{module?.DisplayName}' ({reason})...");
            cts.Cancel();
        }

        if (task != null)
        {
            try
            {
                await Task.WhenAny(task, Task.Delay(2000));
            }
            catch
            {
                // ignored
            }
        }

        StatusChanged?.Invoke(module?.Id, "STOPPED", reason);
    }
}
