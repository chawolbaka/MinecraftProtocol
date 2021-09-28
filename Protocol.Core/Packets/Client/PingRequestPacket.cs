using System;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Request
    /// </summary>
    public class PingRequestPacket : DefinedPacket
    {
        private const int id = 0x00;
        public PingRequestPacket() : base(id,-1) { }

        public static int GetPacketId() => id;
        public static int GetPacketId(int protocolVersion) => id;

        protected override void CheckProperty()
        {

        }

        protected override void Read()
        {
            throw new NotImplementedException();
        }

        protected override void Write()
        {
            throw new NotImplementedException();
        }
    }
}
