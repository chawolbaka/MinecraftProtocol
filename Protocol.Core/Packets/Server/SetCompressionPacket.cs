using System;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    public partial class SetCompressionPacket : DefinedPacket
    {
        [PacketProperty]
        internal int _threshold;

        protected override void Write()
        {
            if (ProtocolVersion < ProtocolVersions.V14w28a)
                throw new InvalidPacketException($"协议版本\"{ProtocolVersion}\"中不存在数据包压缩包(至少需要14w28a以上版本)", this);
            WriteVarInt(_threshold);
        }

        protected override void Read()
        {
            if (ProtocolVersion < ProtocolVersions.V14w28a)
                throw new PacketNotFoundException($"协议版本\"{ProtocolVersion}\"中不存在数据包压缩包(至少需要14w28a以上版本)", this);
            _threshold = Reader.ReadVarInt();
        }

        public static int GetPacketId(int protocolVersion)
        {

            /*
             * 1.13-pre9(391)
             * Set Compression is again 0x03
             * 1.13-pre3(385)
             * Changed the ID of Set Compression from 0x03 to 0x04
             * 15w46a(86)
             * Removed Set Compression (0x1E) during play. Its login varient should be used instead.
             * 15w43a(80)
             * Changed ID of Set Compression from 0x1D to 0x1E
             * 15w36a(67)
             * Changed ID of Set Compression from 0x46 to 0x1D
             */

            if (protocolVersion >= ProtocolVersions.V14w28a)
            {
#if !DROP_PRE_RELEASE
                if (protocolVersion >= ProtocolVersions.V1_13_pre9)        return 0x03;
                else if (protocolVersion >= ProtocolVersions.V1_13_pre3)   return 0x04;
                else if (protocolVersion >= ProtocolVersions.V15w46a)      return 0x03;
#else
                if (protocolVersion >= ProtocolVersionNumbers.V15w46a) return 0x03;
#endif
                else if (protocolVersion >= ProtocolVersions.V15w43a) return 0x1E;
                else if (protocolVersion >= ProtocolVersions.V15w36a) return 0x1D;
                else return 0x46;
            }
            else
                return UnsupportPacketId;
        }
    }
}
