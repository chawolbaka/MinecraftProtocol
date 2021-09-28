using System;
using System.Collections.Generic;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Login_Start
    /// </summary>
    public partial class LoginStartPacket : DefinedPacket
    {
        [PacketProperty]
        private string _playerName;

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
        }

        protected override void Read()
        {
            _playerName = Reader.ReadString();
        }


        /// <summary>从一个LoginStart包中读取玩家名,如果传入其它包会抛出异常.</summary>
        public static string GetPlayerName(ReadOnlyPacket packet) => packet.ReadString();
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
