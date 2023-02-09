using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
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


        public static Packet BuildServerChatMessage(this ICompatible compatible, string message) => BuildServerChatMessage(message, compatible.ProtocolVersion);
        public static Packet BuildServerChatMessage(this ICompatible compatible, string message, ChatPosition position) => BuildServerChatMessage(message, position, compatible.ProtocolVersion);
        public static Packet BuildServerChatMessage(string message, int protocolVersion) => BuildServerChatMessage(message, ChatPosition.ChatMessage, protocolVersion);
        public static Packet BuildServerChatMessage(string message, ChatPosition position, int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_19) //这个最简单，其它的还要签名和搞翻译组件太麻烦了，所以暂时统一创建这个
                return new SystemChatMessagePacket(message, false, protocolVersion);
            else
                return new ServerChatMessagePacket(message, (byte)position, UUID.Empty, protocolVersion);
        }
    }
}
