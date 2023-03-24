using System;
using System.Collections.Generic;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;

namespace MinecraftProtocol.Packets
{
    public static class PacketCache
    {
        private static Dictionary<int, byte[]> DisconnectLogin = new ();
        private static Dictionary<int, byte[]> Disconnect = new();
        
        public static byte[] GetDisconnect(string message, int protocolVersion, int compressionThreshold)
        {
            int hashCode = HashCode.Combine(message, (ushort)protocolVersion, compressionThreshold);
            if (!Disconnect.ContainsKey(hashCode))
            {
                using Packet packet = new DisconnectPacket(ChatComponent.Parse(message), protocolVersion);
                Disconnect.Add(hashCode, packet.Pack(compressionThreshold));
            }

            return Disconnect[hashCode];
        }

        public static byte[] GetDisconnectLogin(string message)
        {
            int hashCode = message.GetHashCode();
            if (!DisconnectLogin.ContainsKey(hashCode))
            {
                using Packet packet = new DisconnectLoginPacket(ChatComponent.Parse(message), -1);
                DisconnectLogin.Add(hashCode, packet.Pack(-1));
            }
         
            return DisconnectLogin[hashCode];
        }
    }
}
