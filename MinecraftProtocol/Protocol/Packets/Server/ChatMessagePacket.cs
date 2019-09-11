using System;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Chat_Message_.28clientbound.29
    /// </summary>
    public class ChatMessagePacket:Packet
    {
        public ChatMessage Message => _message != null ? _message : _message = ChatMessage.Deserialize(Json);
        public string Json { get; }
        /// <summary>
        /// 0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar).
        /// </summary>
        public byte? Position { get; }
        private ChatMessage _message;

        public ChatMessagePacket(ChatMessage chatMessage, byte position, int protocolVersion) : this(chatMessage.Serialize(), position, protocolVersion) { }
        public ChatMessagePacket(string json, byte position, int protocolVersion) : base(GetPacketID(protocolVersion))
        {
            this.Json = json;
            WriteString(json);
            //14w02a:Added 'Position' to Chat Message Clientbound
            if (protocolVersion >= ProtocolVersionNumbers.V14w02a)
            {
                this.Position = position;
                WriteUnsignedByte(position);
            }
            if (Data.Count > 32767)
                throw new ArgumentOutOfRangeException(nameof(json));
        }
        public ChatMessagePacket(Packet packet, int protocolVersion)
        {
            if (packet.ID != GetPacketID(protocolVersion))
                throw new InvalidPacketException(packet);

            var result = ReadPacketData(packet, protocolVersion);
            if (!result.HasValue)
                throw new InvalidPacketException(packet);
            this.ID = packet.ID;
            this.Data = new List<byte>(packet.Data);
            this.Json = result.Value.Json;
            this.Position = result.Value.Position;
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
            if (protocolVersion >= ProtocolVersionNumbers.V17w45a)
                return 0x0E;
            if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5)
                return 0x0F;
            if (protocolVersion >= ProtocolVersionNumbers.V17w13a)
                return 0x10;
            if (protocolVersion >= ProtocolVersionNumbers.V15w36a)
                return 0X0F;
            else
                return 0x02;

        }
        public static bool Verify(Packet packet, int protocolVersion)
        {
            if (packet.ID != GetPacketID(protocolVersion))
                return false;
            return ReadPacketData(packet, protocolVersion).HasValue;
        }
        private static (string Json, byte? Position)? ReadPacketData(Packet packet, int protocolVersion)
        {
            try
            {
                if (packet.Data.Count > 32767)
                    return null;

                string json = ProtocolHandler.ReadString(packet.Data, 0, out int offset, true);
                if (protocolVersion >= ProtocolVersionNumbers.V14w02a && packet.Data.Count == offset + 1)
                    return (json, ProtocolHandler.ReadUnsignedByte(packet.Data, offset, true));
                else if (packet.Data.Count == offset)
                    return (json, null);
                else
                    return null;
            }
            catch (ArgumentOutOfRangeException) { return null; }
            catch (IndexOutOfRangeException) { return null; }
            catch (OverflowException) { return null; }
        }
    }
}
