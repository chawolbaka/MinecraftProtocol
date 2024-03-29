﻿using System;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{

    /// <summary>
    /// https://wiki.vg/Protocol#Keep_Alive_.28clientbound.29
    /// </summary>
    public partial class KeepAliveRequestPacket : DefinedPacket
    {
        [PacketProperty]
        internal long _code;

        protected override void Write()
        {
            if (ProtocolVersion >= ProtocolVersions.V1_12_2_pre1)
                WriteLong(_code);
            else if (ProtocolVersion >= ProtocolVersions.V14w31a)
                WriteVarInt((int)_code);
            else
                WriteInt((int)_code);
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            if (ProtocolVersion >= ProtocolVersions.V1_12_2_pre1)
                _code = reader.ReadLong();
            else if (ProtocolVersion >= ProtocolVersions.V14w31a)
                _code = reader.ReadVarInt();
            else
                _code = reader.ReadInt();
        }

        public static int GetPacketId(int protocolVersion)
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

            if (protocolVersion >= ProtocolVersions.V1_19_3)       return 0x1F;
            if (protocolVersion >= ProtocolVersions.V1_19)         return 0x1E;
            if (protocolVersion >= ProtocolVersions.V1_17)         return 0x21;
            if (protocolVersion >= ProtocolVersions.V20w28a)       return 0x1F;
            if (protocolVersion >= ProtocolVersions.V1_16)         return 0x20;
            if (protocolVersion >= ProtocolVersions.V1_15)         return 0x21;
            if (protocolVersion >= ProtocolVersions.V1_14)         return 0x20;//不知道什么时候更新成这个的
            if (protocolVersion >= ProtocolVersions.V1_13_pre7)    return 0x21;
            if (protocolVersion >= ProtocolVersions.V17w46a)       return 0x20;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5)    return 0x1F;
            if (protocolVersion >= ProtocolVersions.V17w13a)       return 0x20;
            if (protocolVersion >= ProtocolVersions.V15w46a)       return 0x1F;
            if (protocolVersion >= ProtocolVersions.V15w43a)       return 0x20;
            if (protocolVersion >= ProtocolVersions.V15w36a)       return 0x1F;
            else                                                   return 0x00;
        }
    }
}
