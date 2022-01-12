using System;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Response
    /// </summary>
    public partial class PingResponsePacket : DefinedPacket
    {
        [PacketProperty]
        internal string _content;

        public PingResponsePacket(ReadOnlyPacket packet) : this(packet,-1) { }
        public PingResponsePacket(string content) : this(content, -1) { }

        protected override void CheckProperty()
        {
            if (string.IsNullOrWhiteSpace(_content))
                throw new ArgumentNullException(nameof(Content));
        }

        protected override void Write()
        {
            WriteString(_content);
        }

        protected override void Read()
        {
            _content = Reader.ReadString();
        }

        private const int id = 0x00;
        public static int GetPacketId(int protocol) => id;
        public static int GetPacketID() => id;

    }
}
