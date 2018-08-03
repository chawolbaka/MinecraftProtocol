using MinecraftProtocol.Protocol;
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
            KeepAlive,
            LoginSuccess,
            Unknown
        }
        /// <summary>
        /// 从客户端发送的包
        /// </summary>
        public enum Client
        {
            KeepAlive,
            ChatMessage,
            Unknown
        }

        public static int GetPacketID(Client type, int protocolVersion)
        {
            if (type == Client.KeepAlive)
            {
                /*
                 * 1.13-pre7(389)
                 * Changed ID of Keep Alive (clientbound) from 0x20 to 0x21
                 * 17w46a(345)
                 * Changed ID of Keep Alive (clientbound) from 0x1F to 0x20
                 */
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
                 * 80Ago
                 * 0x01
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
        

        public static void GetPacketType(Packet packet,int protocolVersion)
        {
            //这我要怎么返回???
        }
    }
}
