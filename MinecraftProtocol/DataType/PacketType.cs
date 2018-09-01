using System;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.DataType
{
    public static class PacketType
    {
        /// <summary>
        /// 从服务端发送的包
        /// </summary>
        public enum Server
        {
            UnknownPacket,
            KeepAlive,
            LoginSuccess,
            SetCompression
        }
        /// <summary>
        /// 从客户端发送的包
        /// </summary>
        public enum Client
        {
            UnknownPacket,
            LoginStart,
            KeepAlive,
            ChatMessage
        }

        /// <summary>
        /// 通过包的类型来获取包的ID
        /// </summary>
        /// <returns>如果这个类型的包在这个版本中不存在的话会返回int.MinValue</returns>
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

                if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7) return 0x0E;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre4) return 0x0C;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w45a) return 0x0A;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w31a) return 0x0B;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x0C;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w13a) return 0x0C;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w43a) return 0x0B;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w36a) return 0x0A;
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
        /// <summary>
        /// 通过包的类型来获取包的ID
        /// </summary>
        /// <returns>如果这个类型的包在这个版本中不存在的话会返回int.MinValue</returns>
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
            if (type == Server.SetCompression)
            {
                /*
                 * 1.13-pre9(391)
                 * Set Compression is again 0x03
                 * 1.13-pre3(385)
                 * Changed the ID of Set Compression from 0x03 to 0x04
                 * 15w43a(80)
                 * Changed ID of Set Compression from 0x1D to 0x1E
                 * 15w36a(67)
                 * Changed ID of Set Compression from 0x46 to 0x1D
                 */

                if (protocolVersion >= ProtocolVersionNumbers.V14w28a)
                {
                    if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre9) return 0x03;
                    else if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre3) return 0x04;
                    //这边因为wiki上搜不到包ID从0x1E变成0x03的记录,所以我直接wiki最会一次提到0x1E后就返回0x03吧(出事了我再来修把
                    else if (protocolVersion > ProtocolVersionNumbers.V15w46a) return 0x03;
                    else if (protocolVersion >= ProtocolVersionNumbers.V15w43a) return 0x1E;
                    else return 0x46;
                }
                else //数据包压缩是1.8开始才有的东西,所以我取了wiki上第一次提到数据包压缩的协议号
                    return int.MinValue; //如果小于这个协议号就返回一个不存在的协议号

            }
            if (type == Server.KeepAlive)
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
                if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7) return 0x21;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w46a) return 0x20;
                else if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5) return 0x1F;
                else if (protocolVersion >= ProtocolVersionNumbers.V17w13a) return 0x20;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w46a) return 0x1F;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w43a) return 0x20;
                else if (protocolVersion >= ProtocolVersionNumbers.V15w36a) return 0x1F;
                else return 0x00;
            }
            throw new Exception("Can not Get PacketID");
        }
    }
}
