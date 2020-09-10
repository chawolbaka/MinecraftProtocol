using System;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Disconnect_.28play.29
    /// </summary>
    public class DisconnectPacket : Packet
    {

        public ChatMessage Reason => _reason ??= ChatMessage.Deserialize(Json);
        public string Json { get; private set; }
        private ChatMessage _reason;

        private DisconnectPacket(ReadOnlyPacket packet, string json) : base(packet) { this.Json = json; }
        public DisconnectPacket(ChatMessage reason, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            this._reason = reason ?? throw new ArgumentNullException(nameof(reason));
            this.Json = _reason.Serialize();
            WriteString(Json);
        }
        public DisconnectPacket(string json, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));
            this.Json = json;
            WriteString(Json);
        }

        public static int GetPacketID(int protocolVersion)
        {
                /* 
                 * 17w46a(345)
                 * Changed ID of Disconnect (play) from 0x1A to 0x1B
                 * 1.12-pre5(332)
                 * Changed ID of Disconnect (play) from 0x1B to 0x1A
                 * 17w13a(318)
                 * Changed ID of Disconnect (play) from 0x1A to 0x1B
                 * 15w43a?(80)
                 * Changed ID of Disconnect (play) from 0x19 to 0x1A
                 * 15w36a(67)
                 * Changed ID of Disconnect (play) from 0x40 to 0x19
                 */
                if (protocolVersion >= ProtocolVersionNumbers.V1_14)        return 0x1A;
                if (protocolVersion >= ProtocolVersionNumbers.V17w46a)      return 0x1B;
                if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5)   return 0x1A;
                if (protocolVersion >= ProtocolVersionNumbers.V17w13a)      return 0x1B;
                if (protocolVersion >= ProtocolVersionNumbers.V15w43a)      return 0x1A;
                if (protocolVersion >= ProtocolVersionNumbers.V15w36a)      return 0x19;
                else                                                        return 0x40;
            
        }

        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion, out _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out DisconnectPacket dp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            dp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            try
            {
                string json = packet.ReadString();
                if (packet.IsReadToEnd)
                    dp = new DisconnectPacket(packet, json);
                return !(dp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }

    }
}
