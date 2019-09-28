using System;
using System.Collections.Generic;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    public class LoginSuccessPacket:Packet
    {
        public string PlayerName { get; }
        public string PlayerUUID { get; }

        private LoginSuccessPacket(Packet packet,string uuid, string name):base(packet.ID,packet.Data)
        {
            this.PlayerName = name;
            this.PlayerUUID = uuid;
        }
        public LoginSuccessPacket(string uuid,string playerName, int protocolVersion)
        {
            if (uuid.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(uuid), 32, "UUID Max length is 32");
            if (playerName.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(playerName), 16, "Player name too long, max is 16");
            this.ID = GetPacketID(protocolVersion);
            this.PlayerName = playerName;
            this.PlayerUUID = uuid;
            WriteString(uuid);
            WriteString(playerName);
        }
        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Login Success is again 0x02
             * 1.13-pre3(385)
             * Changed the ID of Login Success from 0x02 to 0x03
             */

#if !DROP_PRE_RELEASE
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x02;
            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x03;
            else return 0x02;
#else
            return 0x02;
#endif
        }
        public static bool Verify(Packet packet, int protocolVersion) => Verify(packet, protocolVersion, out _);
        public static bool Verify(Packet packet, int protocolVersion, out LoginSuccessPacket lsp)
        {
            lsp = null;
            if (packet.ID != GetPacketID(protocolVersion))
                return false;
            if (packet.Data.Count <= 34)//随便猜的最低长度,实际上应该更高（但是我懒的算)
                return false;

            try
            {
                string UUID = ProtocolHandler.ReadString(packet.Data, 0, out int offset, true);
                string Name = ProtocolHandler.ReadString(packet.Data, offset, out offset, true);
                if (packet.Data.Count == offset)
                    lsp = new LoginSuccessPacket(packet, UUID, Name);

                return !(lsp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
