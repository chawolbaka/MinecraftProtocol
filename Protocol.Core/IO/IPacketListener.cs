using MinecraftProtocol.Crypto;
using MinecraftProtocol.Utils;
using System;
using System.Net.Sockets;

namespace MinecraftProtocol.IO
{
    public interface IPacketListener : INetworkListener
    {
        event CommonEventHandler<object, PacketReceivedEventArgs> PacketReceived;

        int CompressionThreshold { get; set; }
        CryptoHandler CryptoHandler { get; init; }
        int ProtocolVersion { get; set; }
        
    }
}