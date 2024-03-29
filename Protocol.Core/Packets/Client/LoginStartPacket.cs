﻿using System;
using System.Collections.Generic;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Login_Start
    /// </summary>
    public partial class LoginStartPacket : DefinedPacket
    {
        [PacketProperty]
        private string _playerName;

        [PacketProperty(IsOptional = true)]
        private UUID _playerUUID;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrWhiteSpace(_playerName))
                throw new ArgumentNullException(nameof(PlayerName));
            if (_playerName.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(PlayerName), "玩家名过长");

        }

        protected override void Write()
        {
            WriteString(_playerName);
            if (ProtocolVersion > ProtocolVersions.V1_19)
            {
                WriteBoolean(_playerUUID != default);
                if (_playerUUID != default)
                    WriteUUID(_playerUUID);
            }
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _playerName = reader.ReadString();
            if (ProtocolVersion > ProtocolVersions.V1_19 && reader.ReadBoolean())
                _playerUUID = reader.ReadUUID();
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Login Start is again 0x00
             * 1.13-pre3(385)
             * Changed the ID of Login Start from 0x00 to 0x01
             */

#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersions.V1_13_pre9)       return 0x00;
            else if (protocolVersion >= ProtocolVersions.V1_13_pre3)  return 0x01;
            else return 0x00;
#else
            return 0x01;
#endif
        }
    }
}
