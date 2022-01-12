using System;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Pong
    /// </summary>
    public partial class PongPacket : DefinedPacket
    {
        private const int Id = 0x01;

        [PacketProperty]
        internal long _code;

        public PongPacket(ReadOnlyPacket packet) : this(packet, -1) { }
        public PongPacket(long code) : this(code, -1) { }

        protected override void CheckProperty() { }

        protected override void Write()
        {
            WriteLong(_code);
        }

        protected override void Read()
        {
            _code = Reader.ReadLong();
        }

        public static int GetPacketId(int protocolVersion) => Id;
        public static int GetPacketId() => Id;
    }
}
