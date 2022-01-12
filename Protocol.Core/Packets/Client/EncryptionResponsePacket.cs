using System;
using System.Collections.Generic;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    public partial class EncryptionResponsePacket : DefinedPacket
    {
        [PacketProperty]
        internal byte[] _sharedSecret;
        
        [PacketProperty]
        internal byte[] _verifyToken;
        
        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (_sharedSecret == null || _sharedSecret.Length == 0)
                throw new ArgumentNullException(nameof(SharedSecret));
            if (_verifyToken == null || _verifyToken.Length == 0)
                throw new ArgumentNullException(nameof(VerifyToken));
        }

        protected override void Write()
        {
            WriteByteArray(_sharedSecret, ProtocolVersion);
            WriteByteArray(_verifyToken, ProtocolVersion);
        }

        protected override void Read()
        {
            _sharedSecret = Reader.ReadByteArray(ProtocolVersion);
            _sharedSecret = Reader.ReadByteArray(ProtocolVersion);
        }

        public static int GetPacketId(int protocolVersion)
        {
            /* 
             * 1.13-pre9(391)
             * Encryption Response is again 0x01
             * 1.13-pre3(385)
             * Changed the ID of Encryption Response from 0x01 to 0x02
             * 这个过于原始,加上中间有断层,不考虑兼容.
             * 1.3.1(39)
             * Added packet: 0xFC Encryption Key Response
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
