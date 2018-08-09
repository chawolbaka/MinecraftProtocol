using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftProtocol.Protocol
{
    public static class ProtocolHandler
    {
        public const int MC17w45a = 343;
        /// <summary>
        /// 1.12.2
        /// </summary>
        public const int MC1122_ProtocolVersion = 340;
        public const int MC17w31a = 336;
        /// <summary>
        /// 1.9.0 to 1.9.1-pre1
        /// </summary>
        public const int MC19_ProtocolVersion = 107;
        /// <summary>
        /// 15w36a
        /// </summary>
        public const int MC15w36a_ProtocolVersion = 340;
        /// <summary>
        /// 1.8-1.8.9
        /// </summary>
        public const int MC18_189_ProtocolVersion = 47;

        public enum PacketIncomingType
        {
            KeepAlive,
            LoginSuccess,
            Unknown
        }
        public enum PacketOutgoingType
        {
            KeepAlive,
            ChatMessage,
            Unknown
        }
        public static int GetPacketID(PacketIncomingType packetType, int protocolVersion) => GetPacketIncomingID(packetType, protocolVersion);
        public static int GetPacketID(PacketOutgoingType packetType, int protocolVersion) => GetPacketOutgoingID(packetType, protocolVersion);
        public static int GetPacketIncomingID(PacketIncomingType packetType,int protocolVersion)
        {
            if (protocolVersion == MC1122_ProtocolVersion)
            {
                switch (packetType)
                {
                    case PacketIncomingType.KeepAlive: return 0x1F;
                    case PacketIncomingType.LoginSuccess: return 0x02;
                    default: break;
                }
            }
            else if (protocolVersion<= ProtocolVersionNumbers.V1_8)
            {
                switch (packetType)
                {
                    case PacketIncomingType.KeepAlive: return 0x00;
                    default: break;
                }
            }
            return -1;
        }
        public static int GetPacketOutgoingID(PacketOutgoingType packetType, int protocolVersion)
        {
            if (protocolVersion == MC1122_ProtocolVersion)
            {
                switch (packetType)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    default: break;
                }
            }
            else if (protocolVersion <= MC18_189_ProtocolVersion)
            {
                switch (packetType)
                {
                    case PacketOutgoingType.KeepAlive: return 0x00;
                    default: break;
                }
            }
            if (packetType == PacketOutgoingType.ChatMessage)
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
            return -1;
        }
        public static PacketIncomingType GetPackeType(int packetID,int dataLength,int protocolVersion)
        {
            if (packetID == GetPacketID(PacketIncomingType.KeepAlive,protocolVersion))
            {
                if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && dataLength == 8) return PacketIncomingType.KeepAlive;
                else if (protocolVersion < ProtocolVersionNumbers.V1_12_2_pre1 && dataLength > 0 && dataLength <= 5) return PacketIncomingType.KeepAlive;
            }
            if (packetID == GetPacketID(PacketIncomingType.LoginSuccess,protocolVersion))
            {
                //UUID:String(36)
                //PlayerName:String(16)
                //UUIDLength = 28(字母和数字占1个字符) + 8(符号占2个字符) + 1;
                //玩家名长度,UTF-8好像一个字最多占4个字符,所以最大字节数我就设成16*4了(最小的是1个字符,然后加上前面那个标识string长度的varint的字节数)
                if (dataLength >= 37 + 2 && dataLength <= 4 * 16 + 38) return PacketIncomingType.LoginSuccess;
            }
            return PacketIncomingType.Unknown;
        }
        /// <summary>
        /// 获取包的长度
        /// </summary>
        /// <returns>长度(-1=Timeout)</returns>
        public static int GetPacketLength(TcpClient session)
        {
            //0x7F=127 0x80=128
            int length = 0;
            int x = 0;
            byte[] tmp = new byte[5];
            while (true)
            {
                Receive(tmp, 0, 1, SocketFlags.None, session);
                length |= (tmp[0] & 0x7F) << x++ * 7;
                if (x > 5) throw new OverflowException("VarInt too big");
                if ((tmp[0] & 0x80) != 128) break;
            }
            return length;
        }
        /// <summary>
        /// 从TCP在协议栈里面的缓存中取出数据
        /// </summary>
        /// <param name="buffer">取出来的包</param>
        /// <param name="start">从x开始读取</param>
        /// <param name="offset">读取到x结束</param>
        /// <param name="flags"></param>
        /// <param name="tcp"></param>
        /// <returns>错误码</returns>
        public static void Receive(byte[] buffer, int start, int offset, SocketFlags flags, TcpClient tcp)
        {
            int read = 0;
            while (read < offset)
            {
                    read += tcp.Client.Receive(buffer, start + read, offset - read, flags);
            }
        }
        public static byte[] ReceiveData(int start, int offset,TcpClient session)
        {
            byte[] buffer = new byte[offset-start];
            Receive(buffer, start, offset, SocketFlags.None, session);
            return buffer;
        }
        private static void GetProtocolVersionNubers()
        {
            throw new NotImplementedException("这是一次性的东西,我用来把表格转成常量的...");
            WebClient tmp = new WebClient();
            byte[] pageData = tmp.DownloadData(@"http://wiki.vg/Protocol_version_numbers");
            string html = Encoding.UTF8.GetString(pageData);
            string Table = Regex.Match(html, @"(<table class=""wikitable"">)(\s|\S)+?</table>").Value;
            Dictionary<string, string> VersionNumbers = new Dictionary<string, string>();
            int rowspan = 0;
            string protocolnumbrtbuff = "";
            foreach (var tr in Regex.Matches(Table, @"<tr>(\s|\S)+?</tr>"))
            {
                if (Regex.Match(tr.ToString(), @"<td>\s?(\d+)\s?</td>").Success == true)
                {
                    string reg = @"(<tr>[\s\S]+?<a.*?href="".+?"">)(.+?)(</a>[\S\s]+?<td>\s?)(\d+)[\s\S]+?</tr>";
                    VersionNumbers.Add(Regex.Replace(tr.ToString(), reg, "$2"), Regex.Replace(tr.ToString(), reg, "$4"));
                }
                else if (Regex.Match(tr.ToString(), @"<td rowspan=""(\d+)"">").Success)
                {
                    string reg = @"(<tr>[\s\S]+?<a.*?href="".+?"">)(.+?)</a>[\s\S]+?<td rowspan=""(\d+?)"">\s?(\d+)[\s\S]+?</tr>";
                    rowspan = int.Parse(Regex.Replace(tr.ToString(), reg, "$3"));
                    protocolnumbrtbuff = Regex.Replace(tr.ToString(), reg, "$4");
                    VersionNumbers.Add(Regex.Replace(tr.ToString(), reg, "$2"), protocolnumbrtbuff);
                }
                else if (rowspan > 0)
                {
                    string reg = @"(<tr>[\s\S]+?<a.*?href=.+?>)(.+?)</a>[\s\S]+?</tr>";
                    string version = Regex.Replace(tr.ToString(), reg, "$2");
                    if (VersionNumbers.ContainsKey(version) == false)
                        VersionNumbers.Add(version, protocolnumbrtbuff);
                    rowspan--;
                }
            }
            foreach (var item in VersionNumbers)
            {
                // public const int V17w45a = 343;
                /// <summary>
                /// 1.12.2
                /// </summary>
                Console.WriteLine("/// <summary>");
                Console.WriteLine($"/// {item.Key}");
                Console.WriteLine("/// </summary>");
                Console.WriteLine($"public const int V{item.Key.Replace('.', '_').Replace('-', '_')} = {item.Value};");
            }
        }
        public static Packet ReceivePacket(ConnectionPayload connectInfo)
        {
            //写这个方法的时候Data属性暂时改成了可写的,我当初是为了什么设置成只读的?
            //先去睡觉了,醒来后想想看要不要改回去,为什么要只读这两个问题
            Packet Packet_tmp = new Packet();
            int PacketLength = ProtocolHandler.GetPacketLength(connectInfo.Session);
            Packet_tmp.WriteBytes(ReceiveData(0, PacketLength, connectInfo.Session));
            if (connectInfo.CompressionThreshold > 0)
            {
                int DataLength = ReadNextVarInt(Packet_tmp.Data);
                if (DataLength != 0) //如果是0的话就代表这个数据包没有被压缩
                {
                    byte[] uncompressed = ZlibUtils.Decompress(Packet_tmp.Data.ToArray(), DataLength);
                    Packet_tmp.Data.Clear();
                    Packet_tmp.Data.AddRange(uncompressed);
                }
            }
            Packet_tmp.PacketID = ReadNextVarInt(Packet_tmp.Data);
            return Packet_tmp;
        }

        #region ReadNext(DataType)
        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public static int ReadNextVarInt(List<byte> cache)
        {
            var result = VarInt.Read(cache.ToArray(), 0, out int end);
            cache.RemoveRange(0, end);
            return result;
        }
        public static long ReadNextInt(List<byte> cache)
        {
            byte[] result = cache.ToArray();
            Array.Reverse(result);
            return BitConverter.ToInt32(result, 0);
        }
        public static long ReadNextLong(List<byte> cache)
        {
            byte[] result = cache.ToArray();
            Array.Reverse(result);
            return BitConverter.ToInt64(result, 0);
        }
        public static string ReadNextString(List<byte> cache)
        {
            //这边索引可能有问题,我现在懒的思考
            int length = ReadNextVarInt(cache);
            string result = Encoding.UTF8.GetString(cache.Take(length).ToArray());
            cache.RemoveRange(0, length);
            return result;
        }
        public static bool ReadNextBoolean(List<byte> cache)
        {
            bool result = cache[0] == 0x01 ? true : false;
            cache.RemoveAt(0);
            return result;
        }
        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        public static byte ReadNextByte(List<byte> cache)
        {
            byte result = cache[0];
            cache.RemoveAt(0);
            return result;
        }
        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>
        public static byte[] ReadData(int offset, List<byte> cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache.RemoveRange(0, offset);
            return result;
        }
        #endregion
        /// <summary>
        /// 拼接Byte数组
        /// </summary>
        /// <returns>拼接完的数组</returns>
        public static byte[] ConcatBytes(params byte[][] bytes)
        {
            //这段我直接抄来了
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
            {
                if (array == null)
                    continue;
                else
                    result.AddRange(array);
            }
            return result.ToArray();
        }
    }
}
