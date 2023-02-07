using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Compatible
{
    public static class CompatibleBuilder
    {
        public static Packet BuildChatMessage(this ICompatible compatible, string message) => BuildChatMessage(message, compatible.ProtocolVersion);
        public static Packet BuildChatMessage(string message, int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_19_3 && message[0] == '/')
                return new ChatCommandPacket(message.Substring(1), protocolVersion);
            else
                return new ClientChatMessagePacket(message, protocolVersion);
        }
    }
}
