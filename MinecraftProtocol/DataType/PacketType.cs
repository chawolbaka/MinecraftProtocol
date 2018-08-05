﻿using MinecraftProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public static class PacketType
    {
        /// <summary>
        /// 从服务端发送的包
        /// </summary>
        public enum Server
        {
            Unknown,
            KeepAlive,
            LoginSuccess
        }
        /// <summary>
        /// 从客户端发送的包
        /// </summary>
        public enum Client
        {
            Unknown,
            LoginStart,
            KeepAlive,
            ChatMessage
        }

        public static int GetPacketID(Client type, int protocolVersion)
        {
            if (type==Client.LoginStart)
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
            else if (type == Client.KeepAlive)
            {
                /*
                 * 1.13-pre7(389)
                 * Changed ID of Keep Alive (clientbound) from 0x20 to 0x21
                 * 17w46a(345)
                 * Changed ID of Keep Alive (clientbound) from 0x1F to 0x20
                 * 1.12-pre5(332)
                 * Changed ID of Keep Alive (clientbound) from 0x20 to 0x1F
                 * 17w13a(318)
                 * Changed ID of Keep Alive (clientbound) from 0x1F to 0x20
                 * 15w46a(86)
                 * Changed ID of Keep Alive from 0x20 to 0x1F
                 * 15w43a(80)
                 * Changed ID of Keep Alive from 0x1F to 0x20
                 * 15w36a(67)
                 * Changed ID of Keep Alive from 0x00 to 0x1F
                 */
                if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7)      return 0x21;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w46a)    return 0x20;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x1F;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w13a)    return 0x20;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w46a)    return 0x1F;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w43a)    return 0x20;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w36a)    return 0x1F;
                else return 0x00;
            }
            else if (type == Client.ChatMessage)
            {
                /*
                 * 17w45a(343)
                 * Changed ID of Chat Message (serverbound) from 0x02 to 0x01
                 * 17w31a(336)
                 * Changed ID of Chat Message (serverbound) from 0x03 to 0x02
                 * 1.12-pre5(332)
                 * Changed ID of Chat Message (serverbound) from 0x02 to 0x03
                 * 17w13a(318)
                 * Changed ID of Chat Message (serverbound) changed from 0x02 to 0x03
                 * 16w38a(306)
                 * Max length for Chat Message (serverbound) (0x02) changed from 100 to 256.
                 * 15w43a(80)
                 * Changed ID of Chat Message from 0x01 to 0x02
                 */
                if (protocolVersion >= ProtocolVersionNumbers.V17w45a) return 0x01;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w31a) return 0x02;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x03;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w43a) return 0x02;
                else return 0x01;
            }

            throw new Exception("Can not Get PacketID");
        }
        public static int GetPacketID(Server type, int protocolVersion)
        {
            if (type == Server.LoginSuccess)
            {
                /*
                 * 1.13-pre9(391)
                 * Login Success is again 0x02
                 * 1.13-pre3(385)
                 * Changed the ID of Login Success from 0x02 to 0x03
                 */

                if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x02;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x03;
                else return 0x02;
            }
            if (type == Server.KeepAlive)
            {
                /*
                 * 1.13-pre7(389)
                 * Changed ID of Keep Alive (serverbound) from 0x0C to 0x0E
                 * 1.13-pre4(386)
                 * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0C
                 * 17w45a(343)
                 * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0A
                 * 17w31a(336)
                 * Changed ID of Keep Alive (serverbound) from 0x0C to 0x0B
                 * 1.12-pre5(332)
                 * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0C
                 * 17w13a(318)
                 * Changed ID of Keep Alive (serverbound) from 0x0B to 0x0C
                 * 15w43a(80)
                 * Changed ID of Keep Alive (serverbound) from 0x0A to 0x0B
                 * 15w36a(67)
                 * Changed ID of Keep Alive (serverbound) from 0x00 to 0x0A
                 */

                if      (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7) return 0x0E;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre4) return 0x0C;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w45a)    return 0x0A;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w31a)    return 0x0B;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x0C;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w13a)    return 0x0C;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w43a)    return 0x0B;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w36a)    return 0x0A;
                else return 0x00;
            }
            throw new Exception("Can not Get PacketID");
        }
        

        public static object GetPacketType(Packet packet,int protocolVersion)
        {

            if (packet.PacketID == GetPacketID(Server.LoginSuccess, protocolVersion))
            {
                //如果不是这个包的话,我这样读取会报错的,但是我还需要继续检测下去,所以直接这样做啦.
                try
                {
                    //UUID:String(36)
                    //PlayerName:String(16)
                    Packet tmp = new Packet(packet.PacketID, packet.Data);
                    string UUID = ProtocolHandler.ReadNextString(tmp.Data);
                    string PlayerName = ProtocolHandler.ReadNextString(tmp.Data);
                    if (UUID.Length == 36 && PlayerName.Length > 0 && PlayerName.Length <= 16)
                        return Server.LoginSuccess;
                }
                catch { }

            }
            #region Keep Alive
            /*
            * 1.12.2-pre1, -pre2(339)
            * Changed parameters in Keep Alive (clientbound - 0x1F) and Keep Alive (serverbound - 0x0B) from VarInts to longs.
            * 14w31a
            * Changed the type of Keep Alive ID from Int to VarInt (Clientbound)
            */
            if (packet.PacketID == GetPacketID(Client.KeepAlive, protocolVersion))
            {
                if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Data.Count == 8)
                    return Client.KeepAlive;
                else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Data.Count > 1 && packet.Data.Count <= 5)
                    return Client.KeepAlive;
                else if (packet.Data.Count == 4)
                    return Client.KeepAlive;
            }
            if (packet.PacketID == GetPacketID(Server.KeepAlive, protocolVersion))
            {

                if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Data.Count == 8)
                    return Server.KeepAlive;
                else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Data.Count > 1 && packet.Data.Count <= 5)
                    return Server.KeepAlive;
                else if (packet.Data.Count == 4)
                    return Server.KeepAlive;
            }
            #endregion

            return null;
        }
    }
}
