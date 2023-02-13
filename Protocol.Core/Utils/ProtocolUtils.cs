using MinecraftProtocol.Compression;
using MinecraftProtocol.Packets;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftProtocol.Utils
{
    public static class ProtocolUtils
    {
        public static int ReceivePacketLength(Socket tcp) => VarInt.Read(tcp);
        public static async Task<Packet> ReceivePacketAsync(Socket tcp, int compressionThreshold = -1, CancellationToken cancellationToken = default)
        {
            int PacketLength = ReceivePacketLength(tcp);
            if (PacketLength <= 0)
                throw new PacketException($"Packet length too small");
            else
                return await Packet.DepackAsync(await NetworkUtils.ReceiveDataAsync(tcp, PacketLength, cancellationToken), compressionThreshold);
        }
    }
}
