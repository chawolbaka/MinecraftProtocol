using System;
using System.Collections.Generic;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    public class EncryptionResponsePacket : Packet
    {
        public byte[] SharedSecret { get; }
        public byte[] VerifyToken { get; }


        private EncryptionResponsePacket(Packet packet, byte[] sharedSecret, byte[] verifyToken) : base(packet.ID, packet.Data)
        {
            SharedSecret = sharedSecret;
            VerifyToken = verifyToken;
        }
        public EncryptionResponsePacket(byte[] sharedSecret, byte[] verifyToken, int protocolVersion)
        {
            
            this.ID = GetPacketID(protocolVersion);
            this.SharedSecret = sharedSecret ?? throw new ArgumentNullException(nameof(sharedSecret));
            this.VerifyToken = verifyToken ?? throw new ArgumentNullException(nameof(verifyToken));
            WriteByteArray(SharedSecret, protocolVersion);
            WriteByteArray(VerifyToken, protocolVersion);
        }


        public static int GetPacketID(int protocolVersion)
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
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x01;
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x02;
            else return 0x01;
#else
            return 0x01;
#endif
        }
        public bool Verify(Packet packet, int protocolVersion) => Verify(packet, protocolVersion,out _);
        public bool Verify(Packet packet, int protocolVersion,out EncryptionResponsePacket erp)
        {
            erp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            try
            {
                byte[] SharedSecret = ProtocolHandler.ReadByteArray(packet.Data, protocolVersion, 0, out int offset, true);
                byte[] VerifyToken = ProtocolHandler.ReadByteArray(packet.Data, protocolVersion, offset, out offset, true);
                if (offset == packet.Data.Count)
                    erp = new EncryptionResponsePacket(packet, SharedSecret, VerifyToken);
                return !(erp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }

    }
}
