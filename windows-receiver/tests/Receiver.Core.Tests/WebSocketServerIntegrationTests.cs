using System.Net.WebSockets;
using System.Text;
using Receiver.Core.Dispatcher;
using Receiver.Core.Networking;
using Xunit;

namespace Receiver.Core.Tests;

public class WebSocketServerIntegrationTests
{
    private const int TestPort = 48899;

    [Fact]
    public async Task WebSocketServer_CompleteEndToEndLifecycleTest()
    {
        var dispatcher = new PrankDispatcher();
        var mockModule = new TestMockModule("glitch_screen", delayMs: 5000);
        dispatcher.RegisterModule(mockModule);

        var server = new WebSocketServer(TestPort, dispatcher, "1.0.0");
        server.Start();

        using var client = new ClientWebSocket();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            // 1. Connect to WebSocket server
            var serverUri = new Uri($"ws://127.0.0.1:{TestPort}/ws");
            await client.ConnectAsync(serverUri, cts.Token);
            Assert.Equal(WebSocketState.Open, client.State);

            // 2. Receive Welcome Message
            string welcomeJson = await ReceiveTextAsync(client, cts.Token);
            var welcome = MessageSerializer.Deserialize<WelcomeMessage>(welcomeJson);
            Assert.NotNull(welcome);
            Assert.Equal(MessageTypes.Welcome, welcome.Type);
            Assert.Equal("1.0.0", welcome.Version);
            Assert.Single(welcome.Modules);
            Assert.Equal("glitch_screen", welcome.Modules[0].Id);

            // 3. Receive Initial Status
            string initialStatusJson = await ReceiveTextAsync(client, cts.Token);
            var initialStatus = MessageSerializer.Deserialize<StatusMessage>(initialStatusJson);
            Assert.NotNull(initialStatus);
            Assert.Equal("IDLE", initialStatus.State);

            // 4. Send Execute Command
            var execCmd = new ExecuteCommand
            {
                ModuleId = "glitch_screen",
                Parameters = new Dictionary<string, object> { { "duration_seconds", 5 } }
            };
            await SendTextAsync(client, MessageSerializer.Serialize(execCmd), cts.Token);

            // 5. Receive RUNNING Status
            string runningStatusJson = await ReceiveTextAsync(client, cts.Token);
            var runningStatus = MessageSerializer.Deserialize<StatusMessage>(runningStatusJson);
            Assert.NotNull(runningStatus);
            Assert.Equal("RUNNING", runningStatus.State);
            Assert.Equal("glitch_screen", runningStatus.ActiveModule);
            Assert.True(dispatcher.IsRunning);

            // 6. Send Kill Command
            var killCmd = new KillCommand();
            await SendTextAsync(client, MessageSerializer.Serialize(killCmd), cts.Token);

            // 7. Receive STOPPED Status
            string stoppedStatusJson = await ReceiveTextAsync(client, cts.Token);
            var stoppedStatus = MessageSerializer.Deserialize<StatusMessage>(stoppedStatusJson);
            Assert.NotNull(stoppedStatus);
            Assert.Equal("STOPPED", stoppedStatus.State);
            Assert.False(dispatcher.IsRunning);
            Assert.True(mockModule.WasReverted);

            // 8. Test Nuke Command & Ack
            var nukeCmd = new NukeCommand { SleepSeconds = 120 };
            await SendTextAsync(client, MessageSerializer.Serialize(nukeCmd), cts.Token);

            string nukeAckJson = await ReceiveTextAsync(client, cts.Token);
            var nukeAck = MessageSerializer.Deserialize<NukeAckMessage>(nukeAckJson);
            Assert.NotNull(nukeAck);
            Assert.Equal(MessageTypes.NukeAck, nukeAck.Type);
            Assert.Equal(120, nukeAck.SleepSeconds);

            // 9. Close client cleanly
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", cts.Token);
        }
        finally
        {
            server.Stop();
        }
    }

    [Fact]
    public async Task WebSocketServer_ConcurrentSendsAndBroadcasts_DoNotThrow()
    {
        const int concurrentPort = 48897;
        var dispatcher = new PrankDispatcher();
        var server = new WebSocketServer(concurrentPort, dispatcher, "1.0.0");
        server.Start();

        using var client = new ClientWebSocket();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            var serverUri = new Uri($"ws://127.0.0.1:{concurrentPort}/ws");
            await client.ConnectAsync(serverUri, cts.Token);

            // Read welcome & initial status
            await ReceiveTextAsync(client, cts.Token);
            await ReceiveTextAsync(client, cts.Token);

            // Fire multiple concurrent broadcasts from server simultaneously
            var broadcastTasks = Enumerable.Range(0, 20).Select(i =>
                server.BroadcastMessageAsync(new StatusMessage { State = "IDLE", Message = $"Msg {i}" })
            ).ToArray();

            await Task.WhenAll(broadcastTasks);

            // Consume all 20 messages without socket crash
            for (int i = 0; i < 20; i++)
            {
                string msgJson = await ReceiveTextAsync(client, cts.Token);
                var status = MessageSerializer.Deserialize<StatusMessage>(msgJson);
                Assert.NotNull(status);
            }

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", cts.Token);
        }
        finally
        {
            server.Stop();
        }
    }

    private static async Task<string> ReceiveTextAsync(ClientWebSocket client, CancellationToken token)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static async Task SendTextAsync(ClientWebSocket client, string text, CancellationToken token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
    }
}
