using System;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Pong
    /// </summary>
    public class PongPacket : Packet
    {
        private const int id = 0x01;
        public long Code { get; }
        private PongPacket(Packet packet, long code) : base(packet.ID, packet.Data) { Code = Code; }
        public PongPacket(long code) : base(id)
        {
            this.Code = code; ;
            WriteLong(code);
        }
        public static int GetPacketID() => id;

        public static bool Verify(Packet packet) => Verify(packet, out long? _);
        public static bool Verify(Packet packet, out PongPacket pp)
        {
            pp = null;
            if (Verify(packet, out long? code))
                pp = new PongPacket(packet, code.Value);
            return !(pp is null);
        }
        public static bool Verify(Packet packet, out long? code)
        {
            code = null;
            if (packet.ID == id && packet.Data.Count == 8)
                code = ProtocolHandler.ReadLong(packet.Data, true);
            return !(code is null);
        }
    }
}
