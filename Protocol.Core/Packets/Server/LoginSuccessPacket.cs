using System;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Packets.Server
{
    public partial class LoginSuccessPacket : DefinedPacket
    {
        [PacketProperty]
        internal string _playerName;

        [PacketProperty]
        internal UUID _playerUUID;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (_playerName.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(PlayerName), 16, "Player name too long, max is 16");
        }

        protected override void Write()
        {
            if (ProtocolVersion >= ProtocolVersions.V1_16)
                WriteUUID(_playerUUID);
            else
                WriteString(_playerUUID.ToString().Replace('-',' '));
            WriteString(_playerName);
        }

        protected override void Read()
        {
            if (ProtocolVersion >= ProtocolVersions.V1_16)
                _playerUUID = Reader.ReadUUID();
            else
                _playerUUID = UUID.Parse(Reader.ReadString());
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
