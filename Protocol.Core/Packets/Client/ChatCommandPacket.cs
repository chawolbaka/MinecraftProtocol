using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets.Client
{
    public partial class ChatCommandPacket : DefinedPacket
    {
        [PacketProperty]
        private string _command;

        [PacketProperty]
        private long _timestamp;

        [PacketProperty(IsOptional = true)]
        private long _salt;

        [PacketProperty(IsOptional = true)]
        private SignaturedContent<string>[] _signatures;

        [PacketProperty(IsOptional = true)]
        private int _messageCount;

        [PacketProperty(IsOptional = true)]
        private BitSet _acknowledged;

        public ChatCommandPacket(string command, int protocolVersion) : this(command, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), protocolVersion)
        {

        }

        protected override void CheckProperty()
        {
            base.CheckProperty();

            if (ProtocolVersion <= ProtocolVersions.V1_19_3)
                throw new NotSupportedException($"{nameof(ChatCommandPacket)}需要至少1.19.3(包含)以上的版本才可被创建。");
            if (_command.Length > 256)
                throw new ArgumentOutOfRangeException(nameof(Command));
            
        }
        protected override void Read()
        {
            if (ProtocolVersion <= ProtocolVersions.V1_19_3)
                throw new NotSupportedException($"{nameof(ChatCommandPacket)}需要至少1.19.3(包含)以上的版本才可被创建。");

            _command = Reader.ReadString();
            _timestamp = Reader.ReadLong();
            _salt = Reader.ReadLong();
            _signatures = new SignaturedContent<string>[Reader.ReadVarInt()];
            for (int i = 0; i < _signatures.Length; i++)
            {
                _signatures[i].Content = Reader.ReadString();
                _signatures[i].Signature = Reader.ReadByteArray();
            }
            _messageCount = Reader.ReadVarInt();
            Reader.SetToEnd();
        }

        protected override void Write()
        {
            if (ProtocolVersion <= ProtocolVersions.V1_19_3)
                throw new NotSupportedException($"{nameof(ChatCommandPacket)}需要至少1.19.3(包含)以上的版本才可被创建。");

            WriteString(_command);
            WriteLong(_timestamp);
            WriteLong(_salt);
            if(_signatures != null)
            {
                WriteVarInt(_signatures.Length);
                foreach (var sign in _signatures)
                {
                    WriteString(sign.Content);
                    WriteByteArray(sign.Signature);
                }
            }
            else
            {
                WriteUnsignedByte(0);
            }
            WriteVarInt(_messageCount);
            WriteBytes(0, 0, 0);
        }

        public static int GetPacketId(int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_19_3)
                return 0x04;
            else
                return UnsupportPacketId;
        }
    }
}
