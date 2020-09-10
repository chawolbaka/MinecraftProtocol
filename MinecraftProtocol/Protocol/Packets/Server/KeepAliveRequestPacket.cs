using System;
using System.Collections.Generic;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Server
{

    /// <summary>
    /// https://wiki.vg/Protocol#Keep_Alive_.28clientbound.29
    /// </summary>
    public class KeepAliveRequestPacket : Packet
    {
        private int ProtocolVersion;
        public long Code
        {
            get
            {
                if (ProtocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1)
                    return BitConverter.ToInt64(Data.ToArray(), 0);
                else if (ProtocolVersion >= ProtocolVersionNumbers.V14w31a)
                    return VarInt.Read(Data);
                else
                    return BitConverter.ToInt32(Data.ToArray(), 0);
            }
        }
        private KeepAliveRequestPacket(ReadOnlyPacket packet, int protocolVersion) : base(packet)
        {
            this.ProtocolVersion = protocolVersion;
        }
        public KeepAliveRequestPacket(IEnumerable<byte> code, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            this.ProtocolVersion = protocolVersion;
            WriteBytes(code);
        }
        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre7(389)
             * Changed ID of Keep Alive (clientbound) from 0x20 to 0x21
             * 17w46a(345)
             * Changed ID of Keep Alive (clientbound) from 0x1F to 0x20
             * 1.12-pre5(332)
             * Changed ID of Keep Alive (clientbound) from 0x20 to 0x1F
             * 17w13a(318)
             * Changed ID of Keep Alive (clientbound) from 0x1F to 0x20
             * 15w46a(86)
             * Changed ID of Keep Alive from 0x20 to 0x1F
             * 15w43a(80)
             * Changed ID of Keep Alive from 0x1F to 0x20
             * 15w36a(67)
             * Changed ID of Keep Alive from 0x00 to 0x1F
             */
            if (protocolVersion >= ProtocolVersionNumbers.V1_14)              return 0x20;//不知道什么时候更新成这个的
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7)    return 0x21;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w46a)       return 0x20;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5)    return 0x1F;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w13a)       return 0x20;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w46a)       return 0x1F;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w43a)       return 0x20;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w36a)       return 0x1F;
            else                                                              return 0x00;
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion, out byte[] _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out KeepAliveRequestPacket karp)
        {
            karp = null;
            if (Verify(packet, protocolVersion))
                karp = new KeepAliveRequestPacket(packet, protocolVersion);
            return karp == null;
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out byte[] code)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            code = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Data.Count == 8)
                code = packet.Data.ToArray();
            else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Data.Count <= 5 && packet.Data.Count > 0)
                code = packet.Data.ToArray();
            else if (protocolVersion < ProtocolVersionNumbers.V14w31a && packet.Data.Count == 4)
                code = packet.Data.ToArray();

            return !(code is null);
        }
    }
}
