using MinecraftProtocol.Compatible;
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

        protected override void Read(ref CompatibleByteReader reader)
        {
            _content = reader.ReadString();
        }

        private const int id = 0x00;
        public static int GetPacketId(int protocol) => id;
        public static int GetPacketId() => id;

    }
}
