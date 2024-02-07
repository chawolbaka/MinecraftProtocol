using System;
using System.Text;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;

namespace MinecraftProtocol.Packets.Server
{

    public partial class ServerPluginChannelPacket : DefinedPacket
    {
        [PacketProperty("Channel")]
        protected string _channel;

        [PacketProperty("Data")]
        protected byte[] _messageData;

        [PacketProperty("IsForge", IsReadProperty = true)]
        protected bool _isForge;

        public ServerPluginChannelPacket(string channel, IForgeStructure structure, int protocolVersion) : this(channel, structure.ToBytes(), true, protocolVersion) { }
        public ServerPluginChannelPacket(string channel, ByteWriter writer, int protocolVersion) : this(channel, writer.AsSpan().ToArray(), true, protocolVersion) { }


        protected override void CheckProperty()
        {
            if (string.IsNullOrEmpty(_channel))
                throw new ArgumentNullException(nameof(_channel));
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _channel = reader.ReadString();

            if (ProtocolVersion <= ProtocolVersions.V14w31a && _isForge)
                reader.Position += VarShort.GetLength(reader.AsSpan());
            else if (ProtocolVersion <= ProtocolVersions.V14w31a)
                reader.Position += 2;

            reader.AsSpan().CopyTo(_messageData);
            reader.SetToEnd();
        }

        protected override void Write()
        {
            TryGrow(VarInt.GetLength(_channel.Length) + Encoding.UTF8.GetByteCount(_channel) + _messageData.Length);
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
             * 17w46a(345)
             * Changed ID of Plugin Message (clientbound) from 0x18 to 0x19
             * 1.12-pre5(332)
             * Changed ID of Plugin Message (clientbound) from 0x19 to 0x18
             * 15w36a(67)
             * Changed ID of Plugin Message (clientbound) from 0x3F to 0x18
             */
            if (protocolVersion >= ProtocolVersions.V1_19)   return 0x15;
            if (protocolVersion >= ProtocolVersions.V1_16)   return 0x18;
            if (protocolVersion >= ProtocolVersions.V1_15)   return 0x19;
            if (protocolVersion >= ProtocolVersions.V1_14)   return 0x18;
            if (protocolVersion >= ProtocolVersions.V17w46a) return 0x19;
            if (protocolVersion >= ProtocolVersions.V15w36a) return 0x18;
            else                                             return 0x3F;
        }


        //public static bool operator ==(ServerPluginChannelPacket left, ReceivedPacket right) => right.ID == GetPacketID(right.ProtocolVersion);
        //public static bool operator !=(ServerPluginChannelPacket left, ReceivedPacket right) => !(left == right);
        //public static bool operator ==(ReceivedPacket left, ServerPluginChannelPacket right) => left.ID == GetPacketID(right.ProtocolVersion);
        //public static bool operator !=(ReceivedPacket left, ServerPluginChannelPacket right) => !(left == right);
    }
}
