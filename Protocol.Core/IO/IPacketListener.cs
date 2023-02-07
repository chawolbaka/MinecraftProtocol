using MinecraftProtocol.Crypto;
using System;
using System.Net.Sockets;

namespace MinecraftProtocol.IO
{
    public interface IPacketListener : INetworkListener
    {
        event EventHandler<PacketReceivedEventArgs> PacketReceived;

        int CompressionThreshold { get; set; }
        CryptoHandler CryptoHandler { get; }
        int ProtocolVersion { get; set; }
        
    }
}