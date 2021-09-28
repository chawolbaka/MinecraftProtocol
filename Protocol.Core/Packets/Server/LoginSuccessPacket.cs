using System;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    public partial class LoginSuccessPacket : DefinedPacket
    {
        [PacketProperty]
        public string _playerName;
        [PacketProperty]
        public string _playerUUID;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (_playerUUID.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(PlayerUUID), 32, "UUID max length is 32");
            if (_playerName.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(PlayerName), 16, "Player name too long, max is 16");
        }

        protected override void Write()
        {
            WriteString(_playerUUID);
            WriteString(_playerName);
        }

        protected override void Read()
        {
            _playerUUID = Reader.ReadString();
            _playerName = Reader.ReadString();
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Login Success is again 0x02
             * 1.13-pre3(385)
             * Changed the ID of Login Success from 0x02 to 0x03
             */

#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersions.V1_13_pre9) return 0x02;
            if (protocolVersion >= ProtocolVersions.V1_13_pre3) return 0x03;
            else return 0x02;
#else
            return 0x02;
#endif
        }
    }
}
