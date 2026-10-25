using System.Net;
using System.Text;
using Receiver.Core.Networking;
using Xunit;

namespace Receiver.Core.Tests;

public class DiscoveryServiceTests
{
    [Fact]
    public void BuildDnsResponsePacket_GeneratesValidMdnsStructure()
    {
        var discovery = new DiscoveryService(48888, "TestHost");
        var testIp = IPAddress.Parse("192.168.1.100");

        byte[] packet = discovery.BuildDnsResponsePacket(testIp);

        Assert.NotNull(packet);
        Assert.True(packet.Length > 30);

        // Header check:
        // Transaction ID: 0x0000
        Assert.Equal(0, packet[0]);
        Assert.Equal(0, packet[1]);

        // Flags: 0x8400 (Response, Authoritative)
        Assert.Equal(0x84, packet[2]);
        Assert.Equal(0x00, packet[3]);

        // Answer count: 4 (PTR, SRV, TXT, A)
        Assert.Equal(0, packet[6]);
        Assert.Equal(4, packet[7]);

        // Check if string contains service type & hostname
        string ascii = Encoding.ASCII.GetString(packet);
        Assert.Contains("prankctl", ascii);
        Assert.Contains("TestHost", ascii);

        // Check IP bytes at the end of the packet (192, 168, 1, 100)
        Assert.Equal(192, packet[^4]);
        Assert.Equal(168, packet[^3]);
        Assert.Equal(1, packet[^2]);
        Assert.Equal(100, packet[^1]);
    }

    [Fact]
    public void BuildDnsResponsePacket_TxtRecordHasValidDnsSdAttributes()
    {
        var discovery = new DiscoveryService(48888, "TestHost");
        var testIp = IPAddress.Parse("192.168.1.100");

        byte[] packet = discovery.BuildDnsResponsePacket(testIp);

        // Verify DNS-SD TXT record: [0x09] "txtvers=1" followed by [0x0d] "version=1.0.0"
        byte[] expectedTxtSeq = new byte[] { 0x09 }
            .Concat(Encoding.UTF8.GetBytes("txtvers=1"))
            .Concat(new byte[] { 0x0D })
            .Concat(Encoding.UTF8.GetBytes("version=1.0.0"))
            .ToArray();

        bool foundSequence = false;
        for (int i = 0; i <= packet.Length - expectedTxtSeq.Length; i++)
        {
            if (packet.Skip(i).Take(expectedTxtSeq.Length).SequenceEqual(expectedTxtSeq))
            {
                foundSequence = true;
                break;
            }
        }

        Assert.True(foundSequence, "DNS packet should contain standard DNS-SD length-prefixed TXT strings");
    }
}
