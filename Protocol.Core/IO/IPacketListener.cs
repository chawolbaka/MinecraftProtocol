using MinecraftProtocol.Crypto;
using System;
using System.Net.Sockets;

namespace MinecraftProtocol.IO
{
    public interface IPacketListener : INetworkListener
    {
        event EventHandler<PacketListener.PacketReceivedEventArgs> PacketReceived;

        int CompressionThreshold { get; set; }
        CryptoHandler Crypto { get; }
        int ProtocolVersion { get; set; }
        
    }
}