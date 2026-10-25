using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Receiver.Core.Dispatcher;

namespace Receiver.Core.Networking;

public class WebSocketServer
{
    private readonly int _port;
    private readonly PrankDispatcher _dispatcher;
    private readonly string _serverVersion;
    private readonly ConcurrentDictionary<Guid, ClientSession> _clients = new();

    private sealed class ClientSession : IDisposable
    {
        public WebSocket WebSocket { get; }
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public ClientSession(WebSocket webSocket)
        {
            WebSocket = webSocket;
        }

        public async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (WebSocket.State != WebSocketState.Open) return;

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    await WebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            try { WebSocket.Dispose(); } catch { }
            _sendLock.Dispose();
        }
    }
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _serverCts;

    public event Func<int, Task>? NukeRequested;
    public event Func<Task<UpdateStatusMessage>>? CheckUpdateRequested;
    public event Func<string, Task>? ApplyUpdateRequested;

    public WebSocketServer(int port, PrankDispatcher dispatcher, string version = "1.0.0")
    {
        _port = port;
        _dispatcher = dispatcher;
        _serverVersion = version;

        _dispatcher.StatusChanged += OnDispatcherStatusChanged;
    }

    public void Start()
    {
        _serverCts = new CancellationTokenSource();
        _tcpListener = new TcpListener(IPAddress.Any, _port);
        _tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _tcpListener.Start();

        _dispatcher.Log($"WebSocket server listening on ws://0.0.0.0:{_port}/ws");

        _ = AcceptClientsAsync(_serverCts.Token);
    }

    public void Stop()
    {
        _serverCts?.Cancel();
        _tcpListener?.Stop();

        foreach (var kvp in _clients)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        _clients.Clear();
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _tcpListener != null)
        {
            try
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync(token);
                _ = HandleClientConnectionAsync(tcpClient, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _dispatcher.Log($"Error accepting client: {ex.Message}");
            }
        }
    }

    private async Task HandleClientConnectionAsync(TcpClient tcpClient, CancellationToken token)
    {
        var clientId = Guid.NewGuid();
        NetworkStream? stream = null;
        WebSocket? webSocket = null;

        try
        {
            stream = tcpClient.GetStream();

            // Perform HTTP Upgrade handshake
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            string? line = await reader.ReadLineAsync(token);
            if (line == null || !line.StartsWith("GET"))
            {
                tcpClient.Close();
                return;
            }

            string? secWebSocketKey = null;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(token)))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var headerName = line.Substring(0, colonIndex).Trim();
                    var headerValue = line.Substring(colonIndex + 1).Trim();
                    if (string.Equals(headerName, "Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                    {
                        secWebSocketKey = headerValue;
                    }
                }
            }

            if (string.IsNullOrEmpty(secWebSocketKey))
            {
                var badResponse = Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\n\r\n");
                await stream.WriteAsync(badResponse, token);
                tcpClient.Close();
                return;
            }

            // Calculate Sec-WebSocket-Accept
            string acceptKey;
            string keyToHash = secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(keyToHash));
                acceptKey = Convert.ToBase64String(hash);
            }

            string responseHeaders = "HTTP/1.1 101 Switching Protocols\r\n" +
                                     "Upgrade: websocket\r\n" +
                                     "Connection: Upgrade\r\n" +
                                     $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseHeaders);
            await stream.WriteAsync(responseBytes, token);

            webSocket = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null, TimeSpan.FromSeconds(30));
            var session = new ClientSession(webSocket);
            _clients[clientId] = session;
            _dispatcher.Log($"Client connected: {tcpClient.Client.RemoteEndPoint}");

            // Send Welcome message
            var welcome = new WelcomeMessage
            {
                ServerName = Environment.MachineName,
                Version = _serverVersion,
                Modules = _dispatcher.Modules.Select(m => new ModuleInfo
                {
                    Id = m.Id,
                    Name = m.DisplayName,
                    Description = m.Description,
                    Category = m.Category,
                    DefaultParameters = m.DefaultParameters.ToDictionary(k => k.Key, v => v.Value)
                }).ToList()
            };

            await SendMessageAsync(session, welcome, token);

            // Send initial status
            var initialStatus = new StatusMessage
            {
                ActiveModule = _dispatcher.ActiveModuleId,
                State = _dispatcher.IsRunning ? "RUNNING" : "IDLE",
                Message = _dispatcher.IsRunning ? "Active prank running" : "System ready"
            };
            await SendMessageAsync(session, initialStatus, token);

            // Message receive loop
            var buffer = new byte[8192];
            var messageAccumulator = new MemoryStream();

            while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token);
                        return;
                    }
                    messageAccumulator.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(messageAccumulator.ToArray());
                    messageAccumulator.SetLength(0);

                    await ProcessIncomingMessageAsync(session, json, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _dispatcher.Log($"Client handler exception: {ex.Message}");
        }
        finally
        {
            if (_clients.TryRemove(clientId, out var s))
            {
                s.Dispose();
            }
            else if (webSocket != null)
            {
                try { webSocket.Dispose(); } catch { }
            }
            tcpClient.Close();
            _dispatcher.Log($"Client disconnected ({clientId})");
        }
    }

    private async Task ProcessIncomingMessageAsync(ClientSession client, string json, CancellationToken token)
    {
        var baseMsg = MessageSerializer.DeserializeBase(json);
        if (baseMsg == null)
        {
            await SendMessageAsync(client, new ErrorMessage("Invalid JSON format"), token);
            return;
        }

        switch (baseMsg.Type)
        {
            case MessageTypes.Execute:
                var execCmd = MessageSerializer.Deserialize<ExecuteCommand>(json);
                if (execCmd != null && !string.IsNullOrEmpty(execCmd.ModuleId))
                {
                    bool started = await _dispatcher.ExecuteAsync(execCmd.ModuleId, execCmd.Parameters);
                    if (!started)
                    {
                        await SendMessageAsync(client, new StatusMessage
                        {
                            ActiveModule = _dispatcher.ActiveModuleId,
                            State = _dispatcher.IsRunning ? "BUSY" : "ERROR",
                            Message = "Could not start requested module (already running or invalid module ID)."
                        }, token);
                    }
                }
                break;

            case MessageTypes.UpdateParams:
                var updateParamsCmd = MessageSerializer.Deserialize<UpdateParamsCommand>(json);
                if (updateParamsCmd != null && !string.IsNullOrEmpty(updateParamsCmd.ModuleId) && updateParamsCmd.Parameters != null)
                {
                    _dispatcher.UpdateActiveParameters(updateParamsCmd.ModuleId, updateParamsCmd.Parameters);
                }
                break;

            case MessageTypes.Kill:
                await _dispatcher.StopActiveAsync("Stopped by remote controller");
                break;

            case MessageTypes.Nuke:
                var nukeCmd = MessageSerializer.Deserialize<NukeCommand>(json);
                int sleepSeconds = nukeCmd?.SleepSeconds ?? 120;
                await SendMessageAsync(client, new NukeAckMessage
                {
                    SleepSeconds = sleepSeconds,
                    Message = $"Receiver is shutting down for {sleepSeconds} seconds."
                }, token);

                if (NukeRequested != null)
                {
                    _ = NukeRequested.Invoke(sleepSeconds);
                }
                break;

            case MessageTypes.CheckUpdate:
                try
                {
                    if (CheckUpdateRequested != null)
                    {
                        var updateStatus = await CheckUpdateRequested.Invoke();
                        await SendMessageAsync(client, updateStatus, token);
                    }
                    else
                    {
                        await SendMessageAsync(client, new UpdateStatusMessage
                        {
                            HasUpdate = false,
                            CurrentVersion = _serverVersion,
                            LatestVersion = _serverVersion,
                            ReleaseNotes = "No update checker configured."
                        }, token);
                    }
                }
                catch (Exception ex)
                {
                    _dispatcher.Log($"Error during update check: {ex.Message}");
                    await SendMessageAsync(client, new UpdateStatusMessage
                    {
                        HasUpdate = false,
                        CurrentVersion = _serverVersion,
                        Error = $"Update check failed: {ex.Message}"
                    }, token);
                }
                break;

            case MessageTypes.ApplyUpdate:
                var applyCmd = MessageSerializer.Deserialize<ApplyUpdateCommand>(json);
                if (applyCmd != null && !string.IsNullOrEmpty(applyCmd.DownloadUrl))
                {
                    if (ApplyUpdateRequested != null)
                    {
                        _ = ApplyUpdateRequested.Invoke(applyCmd.DownloadUrl);
                    }
                }
                break;

            case MessageTypes.QueryStatus:
                await SendMessageAsync(client, new StatusMessage
                {
                    ActiveModule = _dispatcher.ActiveModuleId,
                    State = _dispatcher.IsRunning ? "RUNNING" : "IDLE",
                    Message = _dispatcher.IsRunning ? $"Running {_dispatcher.ActiveModuleId}" : "Ready"
                }, token);
                break;

            default:
                await SendMessageAsync(client, new ErrorMessage($"Unknown message type: {baseMsg.Type}"), token);
                break;
        }
    }

    private void OnDispatcherStatusChanged(string? moduleId, string state, string message)
    {
        var statusMsg = new StatusMessage
        {
            ActiveModule = moduleId,
            State = state,
            Message = message
        };

        _ = BroadcastMessageAsync(statusMsg);
    }

    public async Task BroadcastMessageAsync<T>(T message)
    {
        string json = MessageSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var kvp in _clients)
        {
            var session = kvp.Value;
            try
            {
                await session.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static async Task SendMessageAsync<T>(ClientSession session, T message, CancellationToken token = default)
    {
        string json = MessageSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await session.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
    }

    public static async Task SendMessageAsync<T>(WebSocket client, T message, CancellationToken token = default)
    {
        if (client.State == WebSocketState.Open)
        {
            string json = MessageSerializer.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }
    }
}
