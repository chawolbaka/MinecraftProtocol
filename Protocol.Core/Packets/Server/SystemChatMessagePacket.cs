using MinecraftProtocol.Chat;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets.Server
{
    public partial class SystemChatMessagePacket : DefinedPacket
    {
        public virtual ChatComponent Message => !string.IsNullOrWhiteSpace(_content) ? _message ??= ChatComponent.Deserialize(_content) : throw new ArgumentNullException(nameof(_content), "json is empty");
        private ChatComponent _message;

        [PacketProperty]
        private string _content;

        [PacketProperty]
        private bool _overlay;

        public SystemChatMessagePacket(ChatComponent chatComponent, bool overlay, int protocolVersion) : this(chatComponent.Serialize(), overlay, protocolVersion)
        {
            _message = chatComponent;
        }

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrEmpty(_content))
                throw new ArgumentNullException(nameof(Content));
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _content = reader.ReadString();
            _overlay = reader.ReadBoolean();
        }

        protected override void Write()
        {
            WriteString(_content);
            WriteBoolean(_overlay);
        }

        public static int GetPacketId(int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_19_3) return 0x60;
            if (protocolVersion >= ProtocolVersions.V1_19_1) return 0x62;
            if (protocolVersion >= ProtocolVersions.V1_19)   return 0x5F;
            else return UnsupportPacketId;

        }
    }
}
