using System;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    public class SetCompressionPacket : Packet
    {
        public int Threshold { get; }

        private SetCompressionPacket(ReadOnlyPacket packet, int threshold) : base(packet) { this.Threshold = threshold; }
        public SetCompressionPacket(int threshold, int protocolVersion)
        {
            this.ID = GetPacketID(protocolVersion);
            this.Threshold = threshold;
            WriteVarInt(threshold);
        }
        

        public static int GetPacketID(int protocolVersion)
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

            if (protocolVersion >= ProtocolVersionNumbers.V14w28a)
            {
#if !DROP_PRE_RELEASE
                if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9)        return 0x03;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3)   return 0x04;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w46a)      return 0x03;
#else
                if (protocolVersion >= ProtocolVersionNumbers.V15w46a) return 0x03;
#endif
                else if (protocolVersion >= ProtocolVersionNumbers.V15w43a) return 0x1E;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w36a) return 0x1D;
                else return 0x46;
            }
            else
                throw new PacketNotFoundException($"协议版本\"{protocolVersion}\"中不存在数据包压缩包(需要14w28a以上的版本才有)");
        }

        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion, out int? _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out SetCompressionPacket scp)
        {
            scp = null;
            if (Verify(packet, protocolVersion, out int? threshold))
                scp = new SetCompressionPacket(packet, threshold.Value);
            return !(scp is null);
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out int? threshold)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            threshold = null;
            if (protocolVersion < ProtocolVersionNumbers.V14w28a)
                return false;

            try
            {
                if (packet.ID != GetPacketID(protocolVersion))
                    return false;

                threshold = packet.ReadVarInt();
                return packet.IsReadToEnd;
            }
            catch (PacketNotFoundException) { return false; }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
