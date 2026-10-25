namespace Receiver.Core.Dispatcher;

public record PrankContext(
    IReadOnlyDictionary<string, object> Parameters,
    Action<string> LogInfo
);

public interface IPrankModule
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    string Category => "General";
    IReadOnlyDictionary<string, object> DefaultParameters { get; }

    Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken);
    Task RevertAsync();
    void UpdateParameters(IReadOnlyDictionary<string, object> parameters) { }
}
