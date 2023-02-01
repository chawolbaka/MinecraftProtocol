using MinecraftProtocol.Chat;
using MinecraftProtocol.Compatible;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets.Server
{
    public partial class DisguisedChatMessagePacket : DefinedPacket
    {
        //public virtual ChatComponent Message => !string.IsNullOrWhiteSpace(_chatComponentJson) ? _message ??= ChatComponent.Deserialize(_chatComponentJson) : throw new ArgumentNullException(nameof(_chatComponentJson), "json is empty");
        //private ChatComponent _message;

        [PacketProperty]
        private string _message;

        [PacketProperty]
        private int _chatType;

        [PacketProperty]
        private string _chatTypeName;

        [PacketProperty]
        private string _targetName;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrEmpty(_message))
                throw new ArgumentNullException(nameof(Message));
            if (string.IsNullOrEmpty(_chatTypeName))
                throw new ArgumentNullException(nameof(ChatTypeName));
        }

        protected override void Read()
        {
            _message = Reader.ReadString();
            _chatType= Reader.ReadVarInt();
            _chatTypeName = Reader.ReadString();
            _targetName = Reader.ReadOptionalField(Reader.ReadString);
        }

        protected override void Write()
        {
            WriteString(_message);
            WriteVarInt(_chatType);
            WriteString(_chatTypeName);
            WriteOptionalString(_targetName);
        }

        public static int GetPacketId(int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_19_3)
                return 0x18;
            else
                return UnsupportPacketId;
        }
    }
}
