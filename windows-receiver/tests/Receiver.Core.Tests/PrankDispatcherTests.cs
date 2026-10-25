using Receiver.Core.Dispatcher;
using Xunit;

namespace Receiver.Core.Tests;

public class TestMockModule : IPrankModule
{
    public string Id { get; }
    public string DisplayName => $"Mock {Id}";
    public string Description => "Mock module for tests";
    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "delay_ms", 100 }
    };

    public bool WasExecuted { get; private set; }
    public bool WasReverted { get; private set; }
    public bool ShouldThrow { get; set; }
    private readonly int _delayMs;

    public TestMockModule(string id, int delayMs = 100)
    {
        Id = id;
        _delayMs = delayMs;
    }

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        WasExecuted = true;
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated failure in module");
        }

        int delay = _delayMs;
        if (context.Parameters.TryGetValue("delay_ms", out var val) && val is int d)
        {
            delay = d;
        }

        await Task.Delay(delay, cancellationToken);
    }

    public Task RevertAsync()
    {
        WasReverted = true;
        return Task.CompletedTask;
    }
}

public class PrankDispatcherTests
{
    [Fact]
    public void RegisterModule_AddsModuleToCollection()
    {
        var dispatcher = new PrankDispatcher();
        var module = new TestMockModule("mod1");

        dispatcher.RegisterModule(module);

        Assert.Single(dispatcher.Modules);
        Assert.Equal("mod1", dispatcher.Modules.First().Id);
    }

    [Fact]
    public async Task ExecuteAsync_RunsModuleAndTriggersCompleted()
    {
        var dispatcher = new PrankDispatcher();
        var module = new TestMockModule("mod1", delayMs: 50);
        dispatcher.RegisterModule(module);

        string lastState = "";
        dispatcher.StatusChanged += (modId, state, msg) =>
        {
            lastState = state;
        };

        bool started = await dispatcher.ExecuteAsync("mod1");
        Assert.True(started);

        // Wait for completion
        await Task.Delay(200);

        Assert.True(module.WasExecuted);
        Assert.True(module.WasReverted);
        Assert.Equal("COMPLETED", lastState);
        Assert.False(dispatcher.IsRunning);
    }

    [Fact]
    public async Task ExecuteAsync_PreventsConcurrentExecution()
    {
        var dispatcher = new PrankDispatcher();
        var module1 = new TestMockModule("mod1", delayMs: 1000);
        var module2 = new TestMockModule("mod2", delayMs: 1000);
        dispatcher.RegisterModule(module1);
        dispatcher.RegisterModule(module2);

        bool started1 = await dispatcher.ExecuteAsync("mod1");
        Assert.True(started1);
        Assert.True(dispatcher.IsRunning);

        // Trying to start module 2 while module 1 is running should return false
        bool started2 = await dispatcher.ExecuteAsync("mod2");
        Assert.False(started2);

        // Stop module 1
        await dispatcher.StopActiveAsync();
        Assert.False(dispatcher.IsRunning);
    }

    [Fact]
    public async Task StopActiveAsync_CancelsRunningModuleAndReverts()
    {
        var dispatcher = new PrankDispatcher();
        var module = new TestMockModule("mod1", delayMs: 5000);
        dispatcher.RegisterModule(module);

        string lastState = "";
        dispatcher.StatusChanged += (modId, state, msg) =>
        {
            lastState = state;
        };

        await dispatcher.ExecuteAsync("mod1");
        Assert.True(dispatcher.IsRunning);

        await dispatcher.StopActiveAsync("User kill test");

        Assert.Equal("STOPPED", lastState);
        Assert.True(module.WasReverted);
        Assert.False(dispatcher.IsRunning);
    }

    [Fact]
    public async Task ModuleException_IsCaughtAndHandledGracefully()
    {
        var dispatcher = new PrankDispatcher();
        var module = new TestMockModule("faulty") { ShouldThrow = true };
        dispatcher.RegisterModule(module);

        string lastState = "";
        dispatcher.StatusChanged += (modId, state, msg) =>
        {
            lastState = state;
        };

        await dispatcher.ExecuteAsync("faulty");

        await Task.Delay(100);

        Assert.Equal("ERROR", lastState);
        Assert.True(module.WasReverted);
        Assert.False(dispatcher.IsRunning);
    }
}
