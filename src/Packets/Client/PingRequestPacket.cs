using System;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Request
    /// </summary>
    public class PingRequestPacket : Packet
    {
        private const int id= 0x00;
        public PingRequestPacket() : base(id) { }
        public static int GetPacketID() => id;
        public static bool Verify(ReadOnlyPacket packet, out PingRequestPacket prp)
        {
            prp = packet.ID == id && packet.Count == 0 ? new PingRequestPacket() : null;
            return !(prp is null);
        }
    }
}
