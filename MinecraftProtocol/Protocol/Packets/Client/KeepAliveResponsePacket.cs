using System;
using System.Collections.Generic;
using MinecraftProtocol.Compression;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Keep_Alive_.28serverbound.29
    /// </summary>
    public class KeepAliveResponsePacket : Packet
    {
        /*
         * 1.12.2-pre1, -pre2(339)
         * Changed parameters in Keep Alive (clientbound - 0x1F) and Keep Alive (serverbound - 0x0B) from VarInts to longs.
         * 14w31a(32)
         * Changed the type of Keep Alive ID from Int to VarInt (Clientbound)
         */
        private int ProtocolVersion;
        public long Code
        {
            get {
                if (ProtocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1)
                    return SpanConvert.AsLong(_data);
                else if (ProtocolVersion >= ProtocolVersionNumbers.V14w31a)
                    return SpanConvert.AsVarInt(_data);
                else
                    return SpanConvert.AsInt(_data);
            }
        }
        private KeepAliveResponsePacket(ReadOnlyPacket packet, int protocolVersion) : base(packet)
        {
            this.ProtocolVersion = protocolVersion;
        }
        public KeepAliveResponsePacket(ReadOnlySpan<byte> code, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            this.ProtocolVersion = protocolVersion;
            WriteBytes(code);
        }
        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre7(389)
             * Changed ID of Keep Alive (serverbound) from 0x0C to 0x0E
             * 1.13-pre4(386)
             * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0C
             * 17w45a(343)
             * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0A
             * 17w31a(336)
             * Changed ID of Keep Alive (serverbound) from 0x0C to 0x0B
             * 1.12-pre5(332)
             * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0C
             * 17w13a(318)
             * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0C
             * 15w43a(80)
             * Changed ID of Keep Alive (serverbound) from 0x0A to 0x0B
             * 15w36a(67)
             * Changed ID of Keep Alive (serverbound) from 0x00 to 0x0A
             */

            if (protocolVersion >= ProtocolVersionNumbers.V1_14)                return 0x0F; //不知道什么时候更新成这个的
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7)      return 0x0E;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre4)      return 0x0C;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w45a)         return 0x0A;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w31a)         return 0x0B;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5)      return 0x0C;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w13a)         return 0x0C;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w43a)         return 0x0B;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w36a)         return 0x0A;
            else                                                                return 0x00;
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion,out byte[] _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out KeepAliveResponsePacket karp)
        {
            karp = null;
            if (Verify(packet, protocolVersion))
                karp = new KeepAliveResponsePacket(packet, protocolVersion);
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

            if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Count == 8)
                code = packet.ReadAll();
            else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Count <= 5 && packet.Count > 0)
                code = packet.ReadAll();
            else if (protocolVersion < ProtocolVersionNumbers.V14w31a && packet.Count == 4)
                code = packet.ReadAll();

            return !(code is null);
        }
    }
}
