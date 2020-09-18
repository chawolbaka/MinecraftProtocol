using System;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Disconnect_.28login.29
    /// </summary>
    public class DisconnectLoginPacket : Packet
    {

        public ChatMessage Reason => _reason ??= ChatMessage.Deserialize(Json);
        public string Json { get; private set; }
        private ChatMessage _reason;

        private DisconnectLoginPacket(ReadOnlyPacket packet, string json) : base(packet) { this.Json = json; }
        public DisconnectLoginPacket(ChatMessage reason, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            this._reason = reason ?? throw new ArgumentNullException(nameof(reason));
            this.Json = _reason.Serialize();
            WriteString(Json);
        }
        public DisconnectLoginPacket(string json, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            this.Json = json;
            WriteString(Json);
        }

        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Disconnect (login) is again 0x00
             * 1.13-pre3(385)
             * Changed the ID of Disconnect (login) from 0x00 to 0x01
             */
#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x00;
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x01;
            else return 0x00;
#else
            return 0x00;
#endif
        }

        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion, out _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out DisconnectLoginPacket dlp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            dlp = null;
            if (packet.ID!= GetPacketID(protocolVersion))
                return false;

            try
            {
                string json = packet.ReadString();
                if (packet.IsReadToEnd)
                    dlp = new DisconnectLoginPacket(packet,json);
                return !(dlp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }

    }
}
