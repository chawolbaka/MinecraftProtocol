using System;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Ping
    /// </summary>
    public class Ping:Packet
    {
        public const int PacketID = 0x01;
        public long Code { get; }
        public Ping(long code)
        {
            this.Code = code;
            this.ID = Ping.PacketID;
            WriteLong(code);
        }
        public static bool Verify(Packet packet)
        {
            return packet.ID == PacketID && packet.Data.Count == 8;
        }
    }
}
