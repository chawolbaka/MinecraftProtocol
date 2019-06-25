using System;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Pong
    /// </summary>
    public class Pong:Packet
    {
        public const int PacketID = 0x01;
        public long Code { get; }
        public Pong(long code)
        {
            this.Code = code;
            this.ID = Pong.PacketID;
            WriteLong(code);
        }
        public static bool Verify(Packet packet)
        {
            return packet.ID == PacketID && packet.Data.Count == 8;
        }
    }
}
