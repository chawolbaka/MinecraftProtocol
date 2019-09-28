using System;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Encryption_Request
    /// </summary>
    public class EncryptionRequestPacket : Packet
    {
        public string ServerID { get; }
        public byte[] PublicKey { get; }
        public byte[] VerifyToken { get; }

        private EncryptionRequestPacket(Packet packet, string serverID, byte[] publicKey, byte[] verifyToken) : base(packet.ID, packet.Data)
        {
            this.ServerID = serverID;
            this.PublicKey = publicKey;
            this.VerifyToken = verifyToken;
        }
        public EncryptionRequestPacket(string serverID, byte[] publicKey, int protocolVersion) : this(serverID, publicKey, Guid.NewGuid().ToByteArray(), protocolVersion) { }
        public EncryptionRequestPacket(string serverID, byte[] publicKey, byte[] verifyToken, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            this.ServerID = serverID;
            this.PublicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
            this.VerifyToken = verifyToken ?? throw new ArgumentNullException(nameof(verifyToken));
            WriteString(ServerID);
            WriteByteArray(publicKey, protocolVersion);
            WriteByteArray(verifyToken, protocolVersion);
        }
        public static int GetPacketID(int protocolVersion)
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
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x01;
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x02;
            else return 0x01;
#else
            return 0x01;
#endif
        }
        public static bool Verify(Packet packet, int protocolVersion) => Verify(packet, protocolVersion, out _);
        public static bool Verify(Packet packet, int protocolVersion, out EncryptionRequestPacket erp)
        {
            erp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            try
            {
                string ServerID = ProtocolHandler.ReadString(packet.Data, 0, out int offset, true);
                byte[] PublicKey = ProtocolHandler.ReadByteArray(packet.Data, protocolVersion, offset, out offset, true);
                byte[] VerifyToken = ProtocolHandler.ReadByteArray(packet.Data, protocolVersion, offset, out offset, true);
                if (packet.Data.Count == offset)
                    erp = new EncryptionRequestPacket(packet, ServerID, PublicKey, VerifyToken);
                return !(erp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
