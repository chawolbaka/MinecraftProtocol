using System;
using System.Text;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;

namespace MinecraftProtocol.Packets.Client
{
    public partial class ClientPluginChannelPacket : DefinedPacket
    {
        [PacketProperty("Channel")]
        protected string _channel;

        [PacketProperty("Data")]
        protected byte[] _messageData;

        [PacketProperty("IsForge", IsReadProperty = true)]
        protected bool _isForge;

        public ClientPluginChannelPacket(string channel, IForgeStructure structure, int protocolVersion) : this(channel, structure.ToBytes(), true, protocolVersion) { }
        public ClientPluginChannelPacket(string channel, ByteWriter writer, int protocolVersion) : this(channel, writer.AsSpan().ToArray(), true, protocolVersion) { }

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrEmpty(_channel))
                throw new ArgumentNullException(nameof(_channel));
        }

        protected override void Read()
        {
            ReadOnlySpan<byte> buffer = Reader.ReadAll();
            _data = buffer.ToArray();
            _channel = buffer.AsString(out buffer);

            if (ProtocolVersion <= ProtocolVersions.V14w31a && _isForge)
                _messageData = buffer[VarShort.GetLength(buffer)..].ToArray();
            else if (ProtocolVersion <= ProtocolVersions.V14w31a)
                _messageData = buffer[2..].ToArray();
            else
                _messageData = buffer.ToArray();
        }

        protected override void Write()
        {
            TryAllocateCapacity(VarInt.GetLength(_channel.Length) + Encoding.UTF8.GetByteCount(_channel) + _messageData.Length);
            WriteString(_channel);
            if (ProtocolVersion <= ProtocolVersions.V14w31a)
            {
                if (_isForge)
                    WriteVarShort(_messageData.Length).WriteBytes(_messageData);
                else
                    WriteShort((short)_messageData.Length).WriteBytes(_messageData);
            }
            else
                WriteBytes(_messageData);
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre7(389)
             * Changed ID of Plugin message (serverbound) from 0x09 to 0x0A
             * 17w31a(336)
             * Changed ID of Plugin Message (serverbound) from 0x0A to 0x09
             * 1.12-pre5(332)
             * Changed ID of Plugin Message (serverbound) from 0x09 to 0x0A
             * 15w43a(80)
             * Changed ID of Plugin Message (serverbound) from 0x08 to 0x09
             * 15w36a(67)
             * Changed ID of Plugin Message (serverbound) from 0x18 to 0x08
             * 15w31a(49)
             * Changed ID of Plugin Message (serverbound) from 0x17 to 0x18
             */

            if (protocolVersion >= ProtocolVersions.V1_17)      return 0x0A;
            if (protocolVersion >= ProtocolVersions.V1_14)      return 0x0B;
            if (protocolVersion >= ProtocolVersions.V1_13_pre7) return 0x0A;
            if (protocolVersion >= ProtocolVersions.V17w31a)    return 0x09;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5) return 0x0A;
            if (protocolVersion >= ProtocolVersions.V15w43a)    return 0x08;
            if (protocolVersion >= ProtocolVersions.V15w36a)    return 0x18;
            else return 0x17;
        }
    }
}
