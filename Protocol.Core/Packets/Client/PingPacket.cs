using System;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Ping
    /// </summary>
    public partial class PingPacket : DefinedPacket
    {
        private const int Id = 0x01;

        [PacketProperty]
        private long _code;

        public PingPacket(long code):this(code,-1) { }

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
