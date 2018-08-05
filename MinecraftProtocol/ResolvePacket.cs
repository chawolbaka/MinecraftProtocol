using System;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Chat;

namespace MinecraftProtocol
{
    public static class ResolvePacket
    {
        public static (Chat Chat,byte Position) ChatMessage(List<byte> data)
        {
            throw new NotImplementedException("改烂了");
            int length =  ProtocolHandler.ReadNextVarInt(data);
            Chat chat = new Chat("");
            return (chat,data[0]);
        }
        public static (string PlayerName,string UUID) LoginSuccess(List<Byte> data)
        {
            string uuid = ProtocolHandler.ReadNextString(data);
            string name = ProtocolHandler.ReadNextString(data);
            return (name, uuid);
        }
    }
}
