using System;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Encryption_Request
    /// </summary>
    public class EncryptionRequestPacket : Packet
    {
        public string ServerID { get; }
        public byte[] PublicKey { get; }
        public byte[] VerifyToken { get; }

        private EncryptionRequestPacket(ReadOnlyPacket packet, string serverID, byte[] publicKey, byte[] verifyToken) : base(packet)
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
            if (protocolVersion >= ProtocolVersions.V1_13_pre9) return 0x01;
            if (protocolVersion >= ProtocolVersions.V1_13_pre3) return 0x02;
            else return 0x01;
#else
            return 0x01;
#endif
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion, out _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out EncryptionRequestPacket erp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            erp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            try
            {
                string ServerID = packet.ReadString();
                byte[] PublicKey = packet.ReadByteArray(protocolVersion);
                byte[] VerifyToken = packet.ReadByteArray(protocolVersion);
                if (packet.IsReadToEnd)
                    erp = new EncryptionRequestPacket(packet, ServerID, PublicKey, VerifyToken);
                return !(erp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
