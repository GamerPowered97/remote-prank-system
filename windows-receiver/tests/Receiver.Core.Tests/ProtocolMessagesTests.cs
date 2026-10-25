using Receiver.Core.Networking;
using Xunit;

namespace Receiver.Core.Tests;

public class ProtocolMessagesTests
{
    [Fact]
    public void ExecuteCommand_SerializesAndDeserializesCorrectly()
    {
        var cmd = new ExecuteCommand
        {
            ModuleId = "glitch_screen",
            Parameters = new Dictionary<string, object>
            {
                { "duration_seconds", 12 }
            }
        };

        string json = MessageSerializer.Serialize(cmd);
        Assert.Contains("\"type\":\"execute\"", json);
        Assert.Contains("\"module_id\":\"glitch_screen\"", json);

        var deserialized = MessageSerializer.Deserialize<ExecuteCommand>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(MessageTypes.Execute, deserialized.Type);
        Assert.Equal("glitch_screen", deserialized.ModuleId);
        Assert.NotNull(deserialized.Parameters);
        Assert.True(deserialized.Parameters.ContainsKey("duration_seconds"));
    }

    [Fact]
    public void KillCommand_SerializesAndDeserializesCorrectly()
    {
        var cmd = new KillCommand();
        string json = MessageSerializer.Serialize(cmd);
        Assert.Contains("\"type\":\"kill\"", json);

        var deserialized = MessageSerializer.Deserialize<KillCommand>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(MessageTypes.Kill, deserialized.Type);
    }

    [Fact]
    public void UpdateParamsCommand_SerializesAndDeserializesCorrectly()
    {
        var cmd = new UpdateParamsCommand
        {
            ModuleId = "glitch_screen",
            Parameters = new Dictionary<string, object>
            {
                { "intensity", 8 },
                { "speed", 9 }
            }
        };

        string json = MessageSerializer.Serialize(cmd);
        Assert.Contains("\"type\":\"update_params\"", json);
        Assert.Contains("\"module_id\":\"glitch_screen\"", json);

        var deserialized = MessageSerializer.Deserialize<UpdateParamsCommand>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(MessageTypes.UpdateParams, deserialized.Type);
        Assert.Equal("glitch_screen", deserialized.ModuleId);
        Assert.NotNull(deserialized.Parameters);
        Assert.True(deserialized.Parameters.ContainsKey("intensity"));
    }

    [Fact]
    public void NukeCommand_SerializesAndDeserializesCorrectly()
    {
        var cmd = new NukeCommand { SleepSeconds = 120 };
        string json = MessageSerializer.Serialize(cmd);
        Assert.Contains("\"sleep_seconds\":120", json);

        var deserialized = MessageSerializer.Deserialize<NukeCommand>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(120, deserialized.SleepSeconds);
    }

    [Fact]
    public void WelcomeMessage_ContainsModulesAndServerInfo()
    {
        var welcome = new WelcomeMessage
        {
            ServerName = "Test-Rig",
            Version = "1.0.0",
            Modules = new List<ModuleInfo>
            {
                new() { Id = "test_mod", Name = "Test Module", Description = "A test" }
            }
        };

        string json = MessageSerializer.Serialize(welcome);
        Assert.Contains("Test-Rig", json);
        Assert.Contains("test_mod", json);

        var deserialized = MessageSerializer.Deserialize<WelcomeMessage>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("Test-Rig", deserialized.ServerName);
        Assert.Single(deserialized.Modules);
        Assert.Equal("test_mod", deserialized.Modules[0].Id);
    }

    [Fact]
    public void StatusMessage_SerializesStateProperly()
    {
        var status = new StatusMessage
        {
            ActiveModule = "mouse_drift",
            State = "RUNNING",
            Message = "Drifting..."
        };

        string json = MessageSerializer.Serialize(status);
        var deserialized = MessageSerializer.Deserialize<StatusMessage>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("mouse_drift", deserialized.ActiveModule);
        Assert.Equal("RUNNING", deserialized.State);
        Assert.Equal("Drifting...", deserialized.Message);
    }

    [Fact]
    public void MalformedJson_ReturnsNullGracefully()
    {
        var result = MessageSerializer.DeserializeBase("{invalid json");
        Assert.Null(result);

        var cmd = MessageSerializer.Deserialize<ExecuteCommand>("not json");
        Assert.Null(cmd);
    }
}
