using System;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Request
    /// </summary>
    public class PingRequestPacket : Packet
    {
        public const int PacketID = 0x00;
        public PingRequestPacket()
        {
            this.ID = PingRequestPacket.PacketID;
        }
        public PingRequestPacket(Packet pingRequestPacket)
        {
            if (Verify(pingRequestPacket))
                this.ID = PingRequestPacket.PacketID;
            else
                throw new ArgumentException("Invalid packet");
        }
        public static bool Verify(Packet packet)
        {
            return packet.ID == PingRequestPacket.PacketID && packet.Data.Count == 0;
        }
    }
}
