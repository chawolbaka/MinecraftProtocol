using System;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Request
    /// </summary>
    public class PingRequestPacket : Packet
    {
        private const int id= 0x00;
        public PingRequestPacket() : base(id) { }
        public static int GetPacketID() => id;
        public static bool Verify(Packet packet,out PingRequestPacket prp)
        {
            prp = packet.ID == id && packet.Data.Count == 0 ? new PingRequestPacket() : null;
            return !(prp is null);
        }
    }
}
