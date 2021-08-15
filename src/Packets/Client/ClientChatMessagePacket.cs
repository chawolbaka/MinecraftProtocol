using System;
using System.Collections.Generic;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Chat_Message_.28serverbound.29
    /// </summary>
    public class ClientChatMessagePacket:Packet
    {
        /// <summary>For New Version(>=16w38a)</summary>
        public const int MaxMessageLength = 256;
        /// <summary>For Old Version(16w38a ago)</summary>
        public const int OldMaxMessageLength = 100;

        public string Message { get; }

        private ClientChatMessagePacket(ReadOnlyPacket packet, string message) : base(packet)
        {
            Message = message;
        }
        public ClientChatMessagePacket(string message, int protocolVersion)
        {
            /*       
             * 16w38a(306)
             * Max length for Chat Message (serverbound) (0x02) changed from 100 to 256.
             */
            if (protocolVersion >= ProtocolVersionNumbers.V16w38a && message.Length > MaxMessageLength)
                throw new OverflowException($"message too long, max is {MaxMessageLength}");
            else if (protocolVersion < ProtocolVersionNumbers.V16w38a && message.Length > OldMaxMessageLength)
                throw new OverflowException($"message too long, max is {OldMaxMessageLength}");
            ID = GetPacketID(protocolVersion);
            Message = Message;
            WriteString(message);
        }


        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre7(389)
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x01 (back as it was before 1.13)
             * 17w46a(345)
             * Reverted most of the serverbound packet ID changes from 17w45a. The only remaining changes from 1.12.2 are:
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x01
             * 17w45a(343)
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x01
             * 17w31a(336)
             * Changed ID of Chat Message (serverbound) from 0x03 to 0x02
             * 1.12-pre5(332)
             * Changed ID of Chat Message (serverbound) from 0x02 to 0x03(这是什么时候变回0x02的!?我当这条不存在吧..)
             * 17w13a(318)
             * Changed ID of Chat Message (serverbound) changed from 0x02 to 0x03
             * 15w43a(80)
             * Changed ID of Chat Message from 0x01 to 0x02
             */

            if (protocolVersion >= ProtocolVersionNumbers.V1_14)            return 0x03;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7)  return 0x02;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w45a)     return 0x01;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w31a)     return 0x02;
            //else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x03;
            else if (protocolVersion >= ProtocolVersionNumbers.V17w13a)     return 0x03;
            else if (protocolVersion >= ProtocolVersionNumbers.V15w43a)     return 0x02;
            else return 0x01;
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion,out _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out ClientChatMessagePacket ccmp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            ccmp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            try
            {
                string Message = packet.ReadString();
                if (protocolVersion >= ProtocolVersionNumbers.V16w38a && Message.Length > MaxMessageLength)
                    return false;
                else if (Message.Length > OldMaxMessageLength)
                    return false;
                if (packet.IsReadToEnd)
                    ccmp = new ClientChatMessagePacket(packet, Message);
                return !(ccmp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
