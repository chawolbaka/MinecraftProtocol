using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets.Server
{
    public enum MessageFilterType
    {
        PassThrough       = 0,
        FullyFiltered     = 1,
        PartiallyFiltered = 2
    }

    public partial class PlayerChatMessagePacket : DefinedPacket
    {

        /*Header*/
        [PacketProperty]
        private UUID _sender;

        [PacketProperty]
        private int _index;

        [PacketProperty]
        private byte[] _messageSignature;

        /*Body*/
        [PacketProperty]
        private string _message;

        [PacketProperty]
        private long _timestamp;

        [PacketProperty]
        private long _salt;

        /*Previous Messages*/

        [PacketProperty]
        PreviousMessage[] _previousMessages;

        /*Other*/

        [PacketProperty]
        private string _unsignedContent;
        [PacketProperty]
        private int _filterType;
        [PacketProperty]
        private BitSet _FilterTypeBits;


        /*Network target*/

        [PacketProperty]
        private int _chatType;
        [PacketProperty]
        private string _networkName;
        [PacketProperty]
        private string _networkTargetName;

        protected override void Read()
        {
            _sender = Reader.ReadUUID();
            _index = Reader.ReadVarInt();
            _messageSignature = Reader.ReadOptionalBytes(256);

            _message = Reader.ReadString();
            _timestamp = Reader.ReadLong();
            _salt = Reader.ReadLong();

            _previousMessages = new PreviousMessage[Reader.ReadVarInt()]; 
            for (int i = 0; i < _previousMessages.Length; i++)
            {
               PreviousMessage previousMessage=  new PreviousMessage();
                previousMessage.MessageId = Reader.ReadVarInt() - 1;
                if (previousMessage.MessageId != -1 )
                    previousMessage.Signature = Reader.ReadBytes(256);
                _previousMessages[i] = previousMessage;
            }


            _unsignedContent = Reader.ReadString();
            _filterType = Reader.ReadByte();
            if (_filterType == 2)
                Reader.ReadBytes(Reader.ReadVarInt() * sizeof(long));

            _chatType = Reader.ReadVarInt();
            _networkName = Reader.ReadString();
            _networkTargetName = Reader.ReadOptionalField(Reader.ReadString);
        }

        protected override void Write()
        {
            WriteUUID(_sender);
            WriteVarInt(_index);
            WriteOptionalBytes( _messageSignature);

            WriteString(_message);
            WriteLong(_timestamp);
            WriteLong(_salt);


            WriteVarInt(_previousMessages.Length);
            for (int i = 0; i < _previousMessages.Length; i++)
            {
                WriteVarInt(_previousMessages[i].MessageId + 1);
                if (_previousMessages[i].MessageId != -1)
                    WriteBytes(_previousMessages[i].Signature);
            }

            WriteOptionalString(_unsignedContent);
            WriteVarInt(_filterType);
            WriteUnsignedByte(0); //暂时先这样子吧，我不会bitset...

            WriteVarInt(_chatType);
            WriteString(_networkName);
            WriteOptionalString(_networkTargetName);
        }


        public static int GetPacketId(int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_19_3) return 0x31;
            if (protocolVersion >= ProtocolVersions.V1_19_1) return 0x33;
            if (protocolVersion >= ProtocolVersions.V1_19)   return 0x30;
            else return UnsupportPacketId;

        }
    }
}
