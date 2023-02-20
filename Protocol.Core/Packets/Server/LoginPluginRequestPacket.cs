using MinecraftProtocol.Compatible;
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

        protected override void CheckProperty() { }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _messageId = reader.ReadVarInt();
            _channel = reader.ReadIdentifier();
            _messageData = reader.ReadByteArray();
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
