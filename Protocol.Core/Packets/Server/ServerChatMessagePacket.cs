using System;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Chat_Message_.28clientbound.29
    /// </summary>
    public partial class ServerChatMessagePacket : DefinedPacket
    {
        public ChatMessage Message => !string.IsNullOrWhiteSpace(_json) ? _message ??= ChatMessage.Deserialize(Json) : throw new ArgumentNullException(nameof(Json),"json is empty");
        private ChatMessage _message;

        [PacketProperty]
        public string _json;
        
        [PacketProperty]
        public byte? _position; // 0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar).

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrWhiteSpace(_json))
                throw new ArgumentNullException(nameof(Json));
        }

        protected override void Write()
        {
            WriteString(Json);
            //14w02a:Added 'Position' to Chat Message Clientbound
            if (ProtocolVersion >= ProtocolVersions.V14w02a)
                WriteUnsignedByte(_position ?? 0);

            if (Count > 32767)
                throw new ArgumentOutOfRangeException(nameof(Json));
        }

        protected override void Read()
        {
            _json = Reader.ReadString();
            if (ProtocolVersion >= ProtocolVersions.V14w02a && !Reader.IsReadToEnd)
                _position = Reader.ReadUnsignedByte();
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 17w45a(343)
             * Changed ID of Chat Message (clientbound) from 0x0F to 0x0E
             * 1.12-pre5(332)
             * Changed ID of Chat Message (clientbound) from 0x10 to 0x0F
             * 17w13a(318)
             * Changed ID of Chat Message (clientbound) changed from 0x0F to 0x10
             * 15w36a(67)
             * Changed ID of Chat Message (clientbound) changed from 0x02 to 0x0F
             */
            if (protocolVersion >= ProtocolVersions.V17w45a)      return 0x0E;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5)   return 0x0F;
            if (protocolVersion >= ProtocolVersions.V17w13a)      return 0x10;
            if (protocolVersion >= ProtocolVersions.V15w36a)      return 0X0F;
            else                                                  return 0x02;

        }
    }
}
