using System;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Chat;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Disconnect_.28play.29
    /// </summary>
    public partial class DisconnectPacket : DefinedPacket
    {
        [PacketProperty]
        internal string _json;

        internal ChatComponent Reason => !string.IsNullOrWhiteSpace(_json) ? ChatComponent.Deserialize(Json) : throw new ArgumentNullException(nameof(Json), "json is empty");

        public DisconnectPacket(ChatComponent message, int protocolVersion) : this(message.Serialize(), protocolVersion)
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
             * 17w46a(345)
             * Changed ID of Disconnect (play) from 0x1A to 0x1B
             * 1.12-pre5(332)
             * Changed ID of Disconnect (play) from 0x1B to 0x1A
             * 17w13a(318)
             * Changed ID of Disconnect (play) from 0x1A to 0x1B
             * 15w43a?(80)
             * Changed ID of Disconnect (play) from 0x19 to 0x1A
             * 15w36a(67)
             * Changed ID of Disconnect (play) from 0x40 to 0x19
             */
            if (protocolVersion >= ProtocolVersions.V1_17)          return 0x1A;
            if (protocolVersion >= ProtocolVersions.V20w28a)        return 0x19;
            if (protocolVersion >= ProtocolVersions.V1_16)          return 0x1A;
            if (protocolVersion >= ProtocolVersions.V1_15)          return 0x1B;
            if (protocolVersion >= ProtocolVersions.V1_14)          return 0x1A;
            if (protocolVersion >= ProtocolVersions.V17w46a)        return 0x1B;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5)     return 0x1A;
            if (protocolVersion >= ProtocolVersions.V17w13a)        return 0x1B;
            if (protocolVersion >= ProtocolVersions.V15w43a)        return 0x1A;
            if (protocolVersion >= ProtocolVersions.V15w36a)        return 0x19;
            else                                                    return 0x40;

        }

    }
}
