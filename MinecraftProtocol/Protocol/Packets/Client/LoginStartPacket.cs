using System;
using System.Collections.Generic;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Login_Start
    /// </summary>
    public class LoginStartPacket : Packet
    {
        public string PlayerName { get; }

        public LoginStartPacket(string playerName, int protocolVersion)
        {
            this.ID = PacketType.GetPacketID(PacketType.Client.LoginStart, protocolVersion);
            this.PlayerName = playerName;
            WriteString(PlayerName);
        }
        public LoginStartPacket(Packet loginStartPacket, int protocolVersion)
        {
            if (Verify(loginStartPacket, protocolVersion))
            {
                this.ID = loginStartPacket.ID;
                this.Data = new List<byte>(loginStartPacket.Data);
                this.PlayerName = ProtocolHandler.ReadString(Data);
            }
            else
                throw new InvalidPacketException("Not a LoginStart Packet", loginStartPacket);
        }

        /// <summary>从一个LoginStart包中读取玩家名,如果传入其它包会抛出异常.</summary>
        public static string GetPlayerName(Packet packet) => ProtocolHandler.ReadString(packet.Data);
        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre9(391)
             * Login Start is again 0x00
             * 1.13-pre3(385)
             * Changed the ID of Login Start from 0x00 to 0x01
             */

            if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x00;
            else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x01;
            else return 0x00;
        }
        public static bool Verify(Packet packet,int protocolVersion)
        {
            if (packet.ID != GetPacketID(protocolVersion))
                return false;
            List<byte> buffer = new List<byte>(packet.Data);
            try
            {
                ProtocolHandler.ReadNextString(buffer);
                return buffer.Count == 0;
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
