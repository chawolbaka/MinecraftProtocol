using System;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Encryption_Request
    /// </summary>
    public partial class EncryptionRequestPacket : DefinedPacket
    {
        [PacketProperty]
        internal string _serverID;

        [PacketProperty]
        internal byte[] _publicKey;
        
        [PacketProperty]
        internal byte[] _verifyToken;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (_publicKey is null)
             throw new ArgumentNullException(nameof(PublicKey));
            if(_verifyToken is null)
                throw new ArgumentNullException(nameof(VerifyToken));
        }
        protected override void Write()
        {
            WriteString(ServerID);
            WriteByteArray(PublicKey);
            WriteByteArray(VerifyToken);
        }

        protected override void Read()
        {
            _serverID = Reader.ReadString();
            _publicKey = Reader.ReadByteArray();
            _verifyToken = Reader.ReadByteArray();
        }

        public static int GetPacketId(int protocolVersion)
        {
            /* 1.13-pre9(391)
             * Encryption Request is again 0x01
             * 1.13-pre3(385)
             * Changed the ID of Encryption Request from 0x01 to 0x02
             * 这个过于原始,加上中间有断层,不考虑兼容.
             * 1.3.1(39)
             * Added packet: 0xFD Encryption Key Request
             */
#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersions.V1_13_pre9) return 0x01;
            if (protocolVersion >= ProtocolVersions.V1_13_pre3) return 0x02;
            else return 0x01;
#else
            return 0x01;
#endif
        }

    }
}
