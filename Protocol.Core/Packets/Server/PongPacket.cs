using MinecraftProtocol.Compatible;
using System;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Pong
    /// </summary>
    public partial class PongPacket : DefinedPacket
    {
        private const int _id = 0x01;

        [PacketProperty]
        internal long _code;

        public PongPacket(long code) : this(code, -1) { }

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
