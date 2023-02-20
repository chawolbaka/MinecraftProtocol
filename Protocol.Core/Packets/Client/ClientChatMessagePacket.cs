using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Chat_Message_.28serverbound.29
    /// </summary>
    public partial class ClientChatMessagePacket : DefinedPacket
    {
        /// <summary>For New Version(>=16w38a)</summary>
        public const int MaxMessageLength = 256;
        /// <summary>For Old Version(16w38a ago)</summary>
        public const int OldMaxMessageLength = 100;

        [PacketProperty]
        private string _message;

        [PacketProperty(IsOptional = true)]
        private long _timestamp;

        [PacketProperty(IsOptional = true)]
        private long _salt;

        [PacketProperty(IsOptional = true)]
        private byte[] _signature;

        [PacketProperty(IsOptional = true)]
        private int _messageCount;

        [PacketProperty(IsOptional = true)]
        private BitSet _acknowledged;

        [PacketProperty(IsOptional = true)]
        private bool _signedPreview;

        public ClientChatMessagePacket(string message, long timestamp, long salt, byte[] signature, int messageCount, BitSet bitSet, int protocolVersion) : this(message, timestamp, salt, signature, messageCount, bitSet, false, protocolVersion)
        {

        }

        protected override void CheckProperty()
        {
            /*       
             * 16w38a(306)
             * Max length for Chat Message (serverbound) (0x02) changed from 100 to 256.
             */
            base.CheckProperty();

            if (ProtocolVersion >= ProtocolVersions.V16w38a && _message.Length > MaxMessageLength)
                throw new OverflowException($"message too long, max is {MaxMessageLength}");
            if (ProtocolVersion < ProtocolVersions.V16w38a && _message.Length > OldMaxMessageLength)
                throw new OverflowException($"message too long, max is {OldMaxMessageLength}");

            if (_timestamp == default)
                _timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        }

        protected override void Write()
        {
            if (ProtocolVersion == ProtocolVersions.V1_19_1)
                throw new NotSupportedException($"作者偷懒没有去兼容1.19.1和1.19.2 (都{DateTime.Now.Year:D4}年了为什还要玩这种版本呀！)");

            WriteString(_message);

            if(ProtocolVersion >= ProtocolVersions.V1_19)
            {
                WriteLong(_timestamp);
                WriteLong(_salt);
                if (ProtocolVersion >= ProtocolVersions.V1_19_3)
                    WriteOptionalByteArray(_signature);


                if (ProtocolVersion >= ProtocolVersions.V1_19_3)
                    WriteVarInt(_messageCount).WriteBytes(0, 0, 0); //看着java内部1000多行的BitSet暂时偷懒不去支持了
                else if (ProtocolVersion <= ProtocolVersions.V1_19_2)
                    WriteBoolean(_signedPreview);
            }
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _message = reader.ReadString();
            if (ProtocolVersion >= ProtocolVersions.V1_19)
            {
                _timestamp = reader.ReadLong();

                _salt = reader.ReadLong();
                WriteLong(_timestamp);
                WriteLong(_salt);
                if (ProtocolVersion >= ProtocolVersions.V1_19_3)
                    _signature = reader.ReadOptionalByteArray();
                else if (ProtocolVersion <= ProtocolVersions.V1_19_2)
                    _signature = reader.ReadByteArray();


                if (ProtocolVersion >= ProtocolVersions.V1_19_3)
                    _messageCount = reader.ReadVarInt();
                else if (ProtocolVersion <= ProtocolVersions.V1_19_2)
                    _signedPreview = reader.ReadBoolean();

                reader.SetToEnd();
            }
            
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre7(389)
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x01 (back as it was before 1.13)
             * 17w46a(345)
             * Reverted most of the serverbound packet ID changes from 17w45a. The only remaining changes from 1.12.2 are:
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x01
             * 17w45a(343)
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x01
             * 17w31a(336)
             * Changed ID of Chat Message (serverbound) from 0x03 to 0x02
             * 1.12-pre5(332)
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x03(这是什么时候变回0x02的!?我当这条不存在吧..)
             * 17w13a(318)
             * Changed ID of Chat Message (serverbound) changed from 0x02 to 0x03
             * 15w43a(80)
             * Changed ID of Chat Message from 0x01 to 0x02
             */

            if (protocolVersion >= ProtocolVersions.V1_19_1)        return 0x05;
            if (protocolVersion >= ProtocolVersions.V1_19)          return 0x04;
            if (protocolVersion >= ProtocolVersions.V1_14)          return 0x03;
            if (protocolVersion >= ProtocolVersions.V1_13_pre7)     return 0x02;
            if (protocolVersion >= ProtocolVersions.V17w45a)        return 0x01;
            if (protocolVersion >= ProtocolVersions.V17w31a)        return 0x02;
            if (protocolVersion >= ProtocolVersions.V17w13a)        return 0x03;
            if (protocolVersion >= ProtocolVersions.V15w43a)        return 0x02;
            else                                                    return 0x01;
        }
    }
}
