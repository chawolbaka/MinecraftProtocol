﻿using MinecraftProtocol.Compatible;
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
        SignaturedContent<int>[] _previousMessages;

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

        protected override void Read(ref CompatibleByteReader reader)
        {
            _sender = reader.ReadUUID();
            _index = reader.ReadVarInt();
            _messageSignature = reader.ReadOptionalBytes(256);

            _message = reader.ReadString();
            _timestamp = reader.ReadLong();
            _salt = reader.ReadLong();

            _previousMessages = new SignaturedContent<int>[reader.ReadVarInt()]; 
            for (int i = 0; i < _previousMessages.Length; i++)
            {
                SignaturedContent<int> previousMessage =  new SignaturedContent<int>();
                previousMessage.Content = reader.ReadVarInt() - 1;
                if (previousMessage.Content != -1)
                    previousMessage.Signature = reader.ReadBytes(256);
                _previousMessages[i] = previousMessage;
            }


            _unsignedContent = reader.ReadString();
            _filterType = reader.ReadByte();
            if (_filterType == 2)
                reader.ReadBytes(reader.ReadVarInt() * sizeof(long));

            _chatType = reader.ReadVarInt();
            _networkName = reader.ReadString();
            _networkTargetName = reader.ReadOptionalString();
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
                WriteVarInt(_previousMessages[i].Content + 1);
                if (_previousMessages[i].Content != -1)
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
