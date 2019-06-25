using System;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Request
    /// </summary>
    public class PingRequest : Packet
    {
        public const int PacketID = 0x00;
        public PingRequest()
        {
            this.ID = PingRequest.PacketID;
        }
        public PingRequest(Packet pingRequestPacket)
        {
            if (Verify(pingRequestPacket))
                this.ID = PingRequest.PacketID;
            else
                throw new ArgumentException("Invalid packet");
        }
        public static bool Verify(Packet packet)
        {
            return packet.ID == PingRequest.PacketID && packet.Data.Count == 0;
        }
    }
}
