using System;
using System.Collections.Generic;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Chat;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Chat_Message_.28clientbound.29
    /// </summary>
    public partial class ServerChatMessagePacket : DefinedPacket
    {
        public ChatComponent Message => !string.IsNullOrEmpty(_context) ? _message ??= ChatComponent.Deserialize(_context) : throw new ArgumentNullException(nameof(_context), $"{nameof(Context)} is empty");
        private ChatComponent _message;

        [PacketProperty]
        public string _context;
        
        [PacketProperty]
        public byte? _position; // 0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar).

        [PacketProperty]
        public UUID? _sender;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrWhiteSpace(_context))
                throw new ArgumentNullException(nameof(Context));
        }

        protected override void Write()
        {
            WriteString(_context);
            //14w02a:Added 'Position' to Chat Message Clientbound
            if (ProtocolVersion >= ProtocolVersions.V14w02a)
                WriteUnsignedByte(_position ?? 0);
            if (ProtocolVersion >= ProtocolVersions.V1_16)
                WriteUUID(_sender ?? throw new ArgumentNullException(nameof(Sender)));

            if (_size > 32767)
                throw new ArgumentOutOfRangeException(nameof(Context));
        }

        protected override void Read()
        {
            _context = Reader.ReadString();
            if (ProtocolVersion >= ProtocolVersions.V14w02a && !Reader.IsReadToEnd)
                _position = Reader.ReadUnsignedByte();
            if (ProtocolVersion >= ProtocolVersions.V1_16)
                _sender = Reader.ReadUUID();
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
            if (protocolVersion >= ProtocolVersions.V1_17)        return 0x0F;
            if (protocolVersion >= ProtocolVersions.V1_16)        return 0x0E;
            if (protocolVersion >= ProtocolVersions.V1_15)        return 0x0F;
            if (protocolVersion >= ProtocolVersions.V17w45a)      return 0x0E;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5)   return 0x0F;
            if (protocolVersion >= ProtocolVersions.V17w13a)      return 0x10;
            if (protocolVersion >= ProtocolVersions.V15w36a)      return 0X0F;
            else                                                  return 0x02;

        }
    }
}
