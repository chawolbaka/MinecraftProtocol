using System;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol
{
    public static class ResolvePacket
    {
        public static (Chat Chat,byte Position) ChatMessage(List<byte> data)
        {
            int length =  ProtocolHandler.ReadNextVarInt(data);
            Chat chat = new Chat(new List<byte>(ProtocolHandler.ReadData(length, data)));
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
