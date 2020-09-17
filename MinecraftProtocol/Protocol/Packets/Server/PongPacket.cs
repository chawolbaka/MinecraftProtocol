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
        private PongPacket(ReadOnlyPacket packet, long code) : base(packet) { Code = code; }
        public PongPacket(long code) : base(id)
        {
            this.Code = code; ;
            WriteLong(code);
        }
        public static int GetPacketID() => id;

        public static bool Verify(ReadOnlyPacket packet) => Verify(packet, out long? _);
        public static bool Verify(ReadOnlyPacket packet, out PongPacket pp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));

            pp = null;
            if (Verify(packet, out long? code))
                pp = new PongPacket(packet, code.Value);
            return !(pp is null);
        }
        public static bool Verify(ReadOnlyPacket packet, out long? code)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));

            code = null;
            if (packet.ID == id && packet.Count == 8)
                code = packet.ReadLong();
            return !(code is null);
        }
    }
}
