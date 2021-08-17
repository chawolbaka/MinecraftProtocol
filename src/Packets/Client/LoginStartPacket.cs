using System;
using System.Collections.Generic;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Login_Start
    /// </summary>
    public class LoginStartPacket : Packet
    {
        public string PlayerName { get; }

        private LoginStartPacket(ReadOnlyPacket packet, string name) : base(packet)
        {
            this.PlayerName = name;
        }
        public LoginStartPacket(string playerName, int protocolVersion)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentNullException(nameof(playerName));
            if (playerName.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(playerName), "玩家名过长");

            this.ID = GetPacketID(protocolVersion);
            this.PlayerName = playerName;
            WriteString(PlayerName);
        }



        /// <summary>从一个LoginStart包中读取玩家名,如果传入其它包会抛出异常.</summary>
        public static string GetPlayerName(ReadOnlyPacket packet) => packet.ReadString();
        public static int GetPacketID(int protocolVersion)
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

        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out LoginStartPacket lsp)
        {
            lsp = null;
            if(Verify(packet,protocolVersion,out string name))
                lsp = new LoginStartPacket(packet, name);
            return lsp == null;
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out string playerName)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            playerName = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;

            try
            {
                string name = packet.ReadString();
                if (packet.IsReadToEnd)
                    playerName = name;
                return !string.IsNullOrEmpty(playerName);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
