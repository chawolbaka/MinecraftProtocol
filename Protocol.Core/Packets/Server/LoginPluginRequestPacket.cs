using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets.Server
{
    public partial class LoginPluginRequestPacket : DefinedPacket
    {
        [PacketProperty]
        private int _messageId;

        [PacketProperty("Channel")]
        private Identifier _channel;

        [PacketProperty("Data")]
        private byte[] _messageData;

        protected override void Read()
        {
            _messageId = Reader.ReadVarInt();
            _channel = Reader.ReadIdentifier();
            _messageData = Reader.ReadByteArray(ProtocolVersion);
        }

        protected override void Write()
        {
            WriteVarInt(_messageId);
            WriteIdentifier(_channel);
            WriteByteArray(_messageData);
        }

        public static int GetPacketId(int protocolVersion) => 0x04;
    }
}
