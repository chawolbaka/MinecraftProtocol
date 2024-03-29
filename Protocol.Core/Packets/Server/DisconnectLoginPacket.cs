﻿using System;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Chat;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Disconnect_.28login.29
    /// </summary>
    public partial class DisconnectLoginPacket : DefinedPacket
    {
        [PacketProperty]
        internal string _json;

        public virtual ChatComponent Reason => !string.IsNullOrWhiteSpace(_json) ? ChatComponent.Deserialize(Json) : throw new ArgumentNullException(nameof(Json), "json is empty");

        public DisconnectLoginPacket(ChatComponent message, int protocolVersion) : this(message.Serialize(), protocolVersion)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
        }

        protected override void CheckProperty()
        {
            if (string.IsNullOrWhiteSpace(_json))
                throw new ArgumentNullException(nameof(Json));
        }

        protected override void Write()
        {
            WriteString(_json);
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _json = reader.ReadString();
        }


        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Disconnect (login) is again 0x00
             * 1.13-pre3(385)
             * Changed the ID of Disconnect (login) from 0x00 to 0x01
             */
#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersions.V1_13_pre9) return 0x00;
            if (protocolVersion >= ProtocolVersions.V1_13_pre3) return 0x01;
            else return 0x00;
#else
            return 0x00;
#endif
        }

    }
}
