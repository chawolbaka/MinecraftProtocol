using System;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Chat_Message_.28clientbound.29
    /// </summary>
    public class ServerChatMessagePacket : Packet
    {
        public ChatMessage Message => _message ??= ChatMessage.Deserialize(Json);
        public string Json { get; }
        /// <summary>
        /// 0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar).
        /// </summary>
        public byte? Position { get; }
        private ChatMessage _message;

        private ServerChatMessagePacket(ReadOnlyPacket packet,string json, byte? position) : base(packet)
        {
            this.Json = json;
            this.Position = position;
        }
        public ServerChatMessagePacket(ChatMessage chatMessage, byte position, int protocolVersion) : this(chatMessage.Serialize(), position, protocolVersion) { }
        public ServerChatMessagePacket(string json, byte position, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            
            this.Json = json;
            WriteString(json);
            //14w02a:Added 'Position' to Chat Message Clientbound
            if (protocolVersion >= ProtocolVersionNumbers.V14w02a)
            {
                this.Position = position;
                WriteUnsignedByte(position);
            }
            if (Count > 32767)
                throw new ArgumentOutOfRangeException(nameof(json));
        }
        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 17w45a(343)
             * Changed ID of Chat Message (clientbound) from 0x0F to 0x0E
             * 1.12-pre5(332)
             * Changed ID of Chat Message (clientbound) from 0x10 to 0x0F
             * 17w13a(318)
             * Changed ID of Chat Message (clientbound) changed from 0x0F to 0x10
             * 15w36a(67)
             * Changed ID of Chat Message (clientbound) changed from 0x02 to 0x0F
             */
            if (protocolVersion >= ProtocolVersionNumbers.V17w45a)      return 0x0E;
            if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5)   return 0x0F;
            if (protocolVersion >= ProtocolVersionNumbers.V17w13a)      return 0x10;
            if (protocolVersion >= ProtocolVersionNumbers.V15w36a)      return 0X0F;
            else                                                        return 0x02;

        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out ServerChatMessagePacket scmp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            scmp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;
            try
            {
                if (packet.Count > 32767)
                    return false;

                string Json = packet.ReadString();
                byte? Position = null;
                if (protocolVersion >= ProtocolVersionNumbers.V14w02a && !packet.IsReadToEnd)
                    Position = packet.ReadUnsignedByte();
                if (packet.IsReadToEnd)
                    scmp = new ServerChatMessagePacket(packet, Json, Position);
                return !(scmp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
