using MinecraftProtocol.Compatible;
using System;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Ping
    /// </summary>
    public partial class PingPacket : DefinedPacket
    {
        private const int _id = 0x01;

        [PacketProperty]
        internal long _code;

        public PingPacket(long code):this(code,-1) { }

        protected override void CheckProperty() { }

        protected override void Write()
        {
            WriteLong(_code);
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _code = reader.ReadLong();
        }

        public static int GetPacketId(int protocolVersion) => _id;
        public static int GetPacketId() => _id;
    }
}
