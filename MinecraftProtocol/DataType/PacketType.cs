using MinecraftProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public static class PacketType
    {
        public enum Server
        {
            KeepAlive,
            LoginSuccess,
            Unknown
        }
        public enum Client
        {
            KeepAlive,
            ChatMessage,
            Unknown
        }
        public static int GetPacketID(Client type, int protocolVersion)
        {
            if (type == Client.ChatMessage)
            {
                /*
                 * 17w45a(343)
                 *Changed ID of Chat Message (serverbound) from 0x02 to 0x01
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
        public static void GetPacketID(Server type, int protocolVersion)
        {

        }

        public static void GetType(int packetID,int protocolVersion)
        {
            //我要怎么返回???
        }
    }
}
