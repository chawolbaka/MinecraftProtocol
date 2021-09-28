using System;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Disconnect_.28login.29
    /// </summary>
    public partial class DisconnectLoginPacket : DefinedPacket
    {
        [PacketProperty]
        public string _json;

        public ChatMessage Reason => !string.IsNullOrWhiteSpace(_json) ? ChatMessage.Deserialize(Json) : throw new ArgumentNullException(nameof(Json), "json is empty");

        public DisconnectLoginPacket(ChatMessage message, int protocolVersion) : this(message.Serialize(), protocolVersion)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
        }

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrWhiteSpace(_json))
                throw new ArgumentNullException(nameof(Json));
        }

        protected override void Write()
        {
            WriteString(_json);
        }

        protected override void Read()
        {
            _json = Reader.ReadString();
        }


        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Disconnect (login) is again 0x00
             * 1.13-pre3(385)
             * Changed the ID of Disconnect (login) from 0x00 to 0x01
             */
#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersions.V1_13_pre9) return 0x00;
            if (protocolVersion >= ProtocolVersions.V1_13_pre3) return 0x01;
            else return 0x00;
#else
            return 0x00;
#endif
        }

    }
}
