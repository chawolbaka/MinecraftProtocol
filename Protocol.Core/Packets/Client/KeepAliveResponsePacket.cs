using System;
using System.Collections.Generic;
using MinecraftProtocol.Compression;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Keep_Alive_.28serverbound.29
    /// </summary>
    public partial class KeepAliveResponsePacket : DefinedPacket
    {
        /*
         * 1.12.2-pre1, -pre2(339)
         * Changed parameters in Keep Alive (clientbound - 0x1F) and Keep Alive (serverbound - 0x0B) from VarInts to longs.
         * 14w31a(32)
         * Changed the type of Keep Alive ID from Int to VarInt (Clientbound)
         */
        [PacketProperty]
        private long _code;

        protected override void Write()
        {
            if (ProtocolVersion >= ProtocolVersions.V1_12_2_pre1)
                WriteLong(_code);
            else if (ProtocolVersion >= ProtocolVersions.V14w31a)
                WriteVarInt((int)_code);
            else
                WriteInt((int)_code);
        }

        protected override void Read()
        {
            if (ProtocolVersion >= ProtocolVersions.V1_12_2_pre1)
                _code = Reader.ReadLong();
            else if (ProtocolVersion >= ProtocolVersions.V14w31a)
                _code = Reader.ReadVarInt();
            else
                _code = Reader.ReadInt();
        }

        public static int GetPacketId(int protocolVersion)
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

            if (protocolVersion >= ProtocolVersions.V1_14)                return 0x0F; //不知道什么时候更新成这个的
            else if (protocolVersion >= ProtocolVersions.V1_13_pre7)      return 0x0E;
            else if (protocolVersion >= ProtocolVersions.V1_13_pre4)      return 0x0C;
            else if (protocolVersion >= ProtocolVersions.V17w45a)         return 0x0A;
            else if (protocolVersion >= ProtocolVersions.V17w31a)         return 0x0B;
            else if (protocolVersion >= ProtocolVersions.V1_12_pre5)      return 0x0C;
            else if (protocolVersion >= ProtocolVersions.V17w13a)         return 0x0C;
            else if (protocolVersion >= ProtocolVersions.V15w43a)         return 0x0B;
            else if (protocolVersion >= ProtocolVersions.V15w36a)         return 0x0A;
            else                                                          return 0x00;
        }
    }
}
