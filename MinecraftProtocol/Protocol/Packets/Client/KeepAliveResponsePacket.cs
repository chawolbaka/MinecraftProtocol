using System;
using System.Collections.Generic;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Keep_Alive_.28serverbound.29
    /// </summary>
    public class KeepAliveResponsePacket : Packet
    {
        private int ProtocolVersion;
        public long Code
        {
            get {
                if (ProtocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1)
                    return BitConverter.ToInt64(Data.ToArray(), 0);
                else if (ProtocolVersion >= ProtocolVersionNumbers.V14w31a)
                    return new VarInt(Data.ToArray(), 0).ToInt();
                else if (ProtocolVersion < ProtocolVersionNumbers.V14w31a)
                    return BitConverter.ToInt32(Data.ToArray(), 0);
                else //这个永远不会执行到吧!?
                    throw new ProtocolVersionNotSupportedException($"version {ProtocolVersion} not support", ProtocolVersionNumbers.V1_14_3, 1);
            }
        }
        public KeepAliveResponsePacket(List<byte> code, int protocolVersion)
        {
            this.ID = GetPacketID(protocolVersion);
            this.ProtocolVersion = protocolVersion;
            WriteBytes(code);
        }
        public KeepAliveResponsePacket(byte[] code, int protocolVersion)
        {
            this.ID = GetPacketID(protocolVersion);
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

            if (protocolVersion >= ProtocolVersionNumbers.V1_14) return 0x0F; //不知道什么时候更新成这个的
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7) return 0x0E;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre4) return 0x0C;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w45a) return 0x0A;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w31a) return 0x0B;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x0C;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w13a) return 0x0C;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w43a) return 0x0B;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w36a) return 0x0A;
            else return 0x00;
        }
        public static bool Verify(Packet packet,int protocolVersion)
        {
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            /*
             * 1.12.2-pre1, -pre2(339)
             * Changed parameters in Keep Alive (clientbound - 0x1F) and Keep Alive (serverbound - 0x0B) from VarInts to longs.
             * 14w31a(32)
             * Changed the type of Keep Alive ID from Int to VarInt (Clientbound)
             */

            if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Data.Count == 8)
                return true;
            else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Data.Count <= 5 && packet.Data.Count > 0)
                return true;
            else if (protocolVersion < ProtocolVersionNumbers.V14w31a && packet.Data.Count == 4)
                return true;
            else
                return false;
        }
    }
}
