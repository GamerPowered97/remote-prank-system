using System.Text.Json;
using System.Text.Json.Serialization;

namespace Receiver.Core.Networking;

public static class MessageTypes
{
    public const string Execute = "execute";
    public const string Kill = "kill";
    public const string Nuke = "nuke";
    public const string CheckUpdate = "check_update";
    public const string ApplyUpdate = "apply_update";
    public const string QueryStatus = "query_status";

    public const string Welcome = "welcome";
    public const string Status = "status";
    public const string NukeAck = "nuke_ack";
    public const string UpdateStatus = "update_status";
    public const string Error = "error";
}

public class BaseMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class ExecuteCommand : BaseMessage
{
    [JsonPropertyName("module_id")]
    public string ModuleId { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    public ExecuteCommand()
    {
        Type = MessageTypes.Execute;
    }
}

public class KillCommand : BaseMessage
{
    public KillCommand()
    {
        Type = MessageTypes.Kill;
    }
}

public class NukeCommand : BaseMessage
{
    [JsonPropertyName("sleep_seconds")]
    public int SleepSeconds { get; set; } = 120;

    public NukeCommand()
    {
        Type = MessageTypes.Nuke;
    }
}

public class CheckUpdateCommand : BaseMessage
{
    public CheckUpdateCommand()
    {
        Type = MessageTypes.CheckUpdate;
    }
}

public class ApplyUpdateCommand : BaseMessage
{
    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    public ApplyUpdateCommand()
    {
        Type = MessageTypes.ApplyUpdate;
    }
}

public class QueryStatusCommand : BaseMessage
{
    public QueryStatusCommand()
    {
        Type = MessageTypes.QueryStatus;
    }
}

public class ModuleInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("default_parameters")]
    public Dictionary<string, object>? DefaultParameters { get; set; }
}

public class WelcomeMessage : BaseMessage
{
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = Environment.MachineName;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("modules")]
    public List<ModuleInfo> Modules { get; set; } = new();

    public WelcomeMessage()
    {
        Type = MessageTypes.Welcome;
    }
}

public class StatusMessage : BaseMessage
{
    [JsonPropertyName("active_module")]
    public string? ActiveModule { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "IDLE"; // IDLE, RUNNING, STOPPED, COMPLETED, ERROR

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public StatusMessage()
    {
        Type = MessageTypes.Status;
    }
}

public class NukeAckMessage : BaseMessage
{
    [JsonPropertyName("sleep_seconds")]
    public int SleepSeconds { get; set; } = 120;

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Receiver shutting down for 120 seconds...";

    public NukeAckMessage()
    {
        Type = MessageTypes.NukeAck;
    }
}

public class UpdateStatusMessage : BaseMessage
{
    [JsonPropertyName("has_update")]
    public bool HasUpdate { get; set; }

    [JsonPropertyName("current_version")]
    public string CurrentVersion { get; set; } = "1.0.0";

    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = string.Empty;

    [JsonPropertyName("release_notes")]
    public string ReleaseNotes { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    public UpdateStatusMessage()
    {
        Type = MessageTypes.UpdateStatus;
    }
}

public class ErrorMessage : BaseMessage
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public ErrorMessage()
    {
        Type = MessageTypes.Error;
    }

    public ErrorMessage(string msg)
    {
        Type = MessageTypes.Error;
        Message = msg;
    }
}

public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, Options);

    public static BaseMessage? DeserializeBase(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BaseMessage>(json, Options);
        }
        catch
        {
            return null;
        }
    }

    public static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch
        {
            return default;
        }
    }
}
