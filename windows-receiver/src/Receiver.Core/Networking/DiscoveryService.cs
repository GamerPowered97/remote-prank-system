using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Receiver.Core.Networking;

public class DiscoveryService : IDisposable
{
    private const int MdnsPort = 5353;
    private static readonly IPAddress MdnsMulticastAddress = IPAddress.Parse("224.0.0.251");
    private readonly int _servicePort;
    private readonly string _serviceName;
    private readonly string _serviceType = "_prankctl._tcp.local";
    private readonly Action<string>? _logger;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private Task? _announceTask;

    public DiscoveryService(int servicePort, string? serviceName = null, Action<string>? logger = null)
    {
        _servicePort = servicePort;
        _serviceName = string.IsNullOrWhiteSpace(serviceName) ? Environment.MachineName : serviceName;
        _logger = logger;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));

            try
            {
                _udpClient.JoinMulticastGroup(MdnsMulticastAddress);
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"Warning joining mDNS multicast group: {ex.Message}");
            }

            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            _logger?.Invoke($"mDNS discovery responder listening for {_serviceType} on port {MdnsPort}");
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Could not bind mDNS listener on port {MdnsPort}: {ex.Message}. Using announcer fallback.");
            _udpClient?.Dispose();
            _udpClient = new UdpClient();
        }

        // Periodic announcement task
        _announceTask = Task.Run(() => AnnounceLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        if (_udpClient == null) return;

        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(token);
                if (IsPrankctlQuery(result.Buffer))
                {
                    await SendResponseAsync(result.RemoteEndPoint);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    _logger?.Invoke($"mDNS receive error: {ex.Message}");
                }
            }
        }
    }

    private async Task AnnounceLoopAsync(CancellationToken token)
    {
        var multicastEndPoint = new IPEndPoint(MdnsMulticastAddress, MdnsPort);
        while (!token.IsCancellationRequested)
        {
            try
            {
                await SendResponseAsync(multicastEndPoint);
                await Task.Delay(5000, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"mDNS announce error: {ex.Message}");
                await Task.Delay(10000, token);
            }
        }
    }

    private bool IsPrankctlQuery(byte[] buffer)
    {
        // Simple heuristic: check if incoming DNS query contains "prankctl"
        string ascii = Encoding.ASCII.GetString(buffer);
        return ascii.Contains("prankctl", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SendResponseAsync(IPEndPoint targetEndPoint)
    {
        if (_udpClient == null) return;

        var localIps = GetLocalIPv4Addresses();
        foreach (var ip in localIps)
        {
            byte[] packet = BuildDnsResponsePacket(ip);
            try
            {
                await _udpClient.SendAsync(packet, packet.Length, targetEndPoint);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static List<IPAddress> GetLocalIPv4Addresses()
    {
        var primaryList = new List<IPAddress>();
        var secondaryList = new List<IPAddress>();

        try
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up ||
                    iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                bool hasGateway = iface.GetIPProperties().GatewayAddresses
                    .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.Any.Equals(g.Address));

                foreach (var addr in iface.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipStr = addr.Address.ToString();
                        // Exclude APIPA / link-local addresses (169.254.x.x) and loopback
                        if (ipStr.StartsWith("169.254.") || IPAddress.IsLoopback(addr.Address))
                            continue;

                        if (hasGateway)
                        {
                            primaryList.Add(addr.Address);
                        }
                        else
                        {
                            secondaryList.Add(addr.Address);
                        }
                    }
                }
            }
        }
        catch
        {
            // fallback
        }

        if (primaryList.Count > 0)
        {
            return primaryList;
        }

        if (secondaryList.Count > 0)
        {
            return secondaryList;
        }

        return new List<IPAddress> { IPAddress.Loopback };
    }

    public byte[] BuildDnsResponsePacket(IPAddress localIp)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Header: Transaction ID (0 for mDNS response)
        bw.Write((ushort)0);
        // Flags: 0x8400 (Standard query response, Authoritative)
        bw.Write((byte)0x84);
        bw.Write((byte)0x00);
        // Questions: 0
        bw.Write((ushort)0);
        // Answers: 4 (PTR, SRV, TXT, A)
        bw.Write((byte)0x00);
        bw.Write((byte)0x04);
        // Authority RRs: 0
        bw.Write((ushort)0);
        // Additional RRs: 0
        bw.Write((ushort)0);

        string instanceFullName = $"{_serviceName}.{_serviceType}";
        string hostFullName = $"{Environment.MachineName}.local";

        // 1. Answer: PTR Record (_prankctl._tcp.local -> <instance>._prankctl._tcp.local)
        WriteDnsName(bw, _serviceType);
        bw.Write((byte)0x00); bw.Write((byte)0x0C); // Type PTR (12)
        bw.Write((byte)0x00); bw.Write((byte)0x01); // Class IN (1)
        bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x78); // TTL 120s
        byte[] ptrData = EncodeDnsName(instanceFullName);
        bw.Write((byte)(ptrData.Length >> 8));
        bw.Write((byte)(ptrData.Length & 0xFF));
        bw.Write(ptrData);

        // 2. Answer: SRV Record (<instance>._prankctl._tcp.local -> host, port)
        WriteDnsName(bw, instanceFullName);
        bw.Write((byte)0x00); bw.Write((byte)0x21); // Type SRV (33)
        bw.Write((byte)0x80); bw.Write((byte)0x01); // Class IN with Cache Flush (0x8001)
        bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x78); // TTL 120s
        byte[] hostNameBytes = EncodeDnsName(hostFullName);
        ushort srvDataLen = (ushort)(6 + hostNameBytes.Length);
        bw.Write((byte)(srvDataLen >> 8));
        bw.Write((byte)(srvDataLen & 0xFF));
        bw.Write((ushort)0); // Priority 0
        bw.Write((ushort)0); // Weight 0
        bw.Write((byte)(_servicePort >> 8)); // Port high byte
        bw.Write((byte)(_servicePort & 0xFF)); // Port low byte
        bw.Write(hostNameBytes);

        // 3. Answer: TXT Record (<instance>._prankctl._tcp.local -> txtvers=1, version=1.0.0)
        WriteDnsName(bw, instanceFullName);
        bw.Write((byte)0x00); bw.Write((byte)0x10); // Type TXT (16)
        bw.Write((byte)0x80); bw.Write((byte)0x01); // Class IN with Cache Flush
        bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x78); // TTL 120s
        byte[] txt1 = Encoding.UTF8.GetBytes("txtvers=1");
        byte[] txt2 = Encoding.UTF8.GetBytes("version=1.0.0");
        ushort totalTxtLen = (ushort)(1 + txt1.Length + 1 + txt2.Length);
        bw.Write((byte)(totalTxtLen >> 8));
        bw.Write((byte)(totalTxtLen & 0xFF));
        bw.Write((byte)txt1.Length);
        bw.Write(txt1);
        bw.Write((byte)txt2.Length);
        bw.Write(txt2);

        // 4. Answer: A Record (host.local -> local IP)
        WriteDnsName(bw, hostFullName);
        bw.Write((byte)0x00); bw.Write((byte)0x01); // Type A (1)
        bw.Write((byte)0x80); bw.Write((byte)0x01); // Class IN with Cache Flush
        bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x00); bw.Write((byte)0x78); // TTL 120s
        bw.Write((byte)0x00); bw.Write((byte)0x04); // Data length 4
        bw.Write(localIp.GetAddressBytes());

        return ms.ToArray();
    }

    private static void WriteDnsName(BinaryWriter bw, string fqdn)
    {
        byte[] encoded = EncodeDnsName(fqdn);
        bw.Write(encoded);
    }

    private static byte[] EncodeDnsName(string fqdn)
    {
        using var ms = new MemoryStream();
        string[] labels = fqdn.TrimEnd('.').Split('.');
        foreach (var label in labels)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(label);
            ms.WriteByte((byte)bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
        }
        ms.WriteByte(0); // Root label null terminator
        return ms.ToArray();
    }
}
