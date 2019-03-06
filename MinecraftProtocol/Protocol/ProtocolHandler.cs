using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol.VersionCompatible;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MinecraftProtocol.Protocol
{
    public static class ProtocolHandler
    {
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
        /// 获取包的长度
        /// </summary>
        /// <returns>长度(-1=Timeout)</returns>
        public static int GetPacketLength(ConnectionPayload connectInfo) => GetPacketLength(connectInfo.Session);
        /// <summary>
        /// 通过包的id&data加上协议号来分析出是什么类型的包(这是一个低效率的方法,不推荐使用这个方法)
        /// 如果无法分析出是什么类型的话会返回null
        /// </summary>
        /// <returns>PacketType.Client or PacketType.Server or null</returns>
        public static object GetPacketType(Packet packet, int protocolVersion)
        {
            //按照包的重复量或者重要性来排序(比如KeepAlive的优先级是最高的,登陆成功的信息这种放很后面都可以
            #region Keep Alive
            /*
            * 1.12.2-pre1, -pre2(339)
            * Changed parameters in Keep Alive (clientbound - 0x1F) and Keep Alive (serverbound - 0x0B) from VarInts to longs.
            * 14w31a
            * Changed the type of Keep Alive ID from Int to VarInt (Clientbound)
            */
            if (packet.ID == PacketType.GetPacketID(PacketType.Client.KeepAlive, protocolVersion))
            {
                if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Data.Count == 8)
                    return PacketType.Client.KeepAlive;
                else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Data.Count <= 5 && packet.Data.Count > 0)
                    return PacketType.Client.KeepAlive;
                else if (packet.Data.Count == 4)
                    return PacketType.Client.KeepAlive;
            }
            if (packet.ID == PacketType.GetPacketID(PacketType.Server.KeepAlive, protocolVersion))
            {

                if (protocolVersion >= ProtocolVersionNumbers.V1_12_2_pre1 && packet.Data.Count == 8)
                    return PacketType.Server.KeepAlive;
                else if (protocolVersion >= ProtocolVersionNumbers.V14w31a && packet.Data.Count <= 5 && packet.Data.Count > 0)
                    return PacketType.Server.KeepAlive;
                else if (packet.Data.Count == 4)
                    return PacketType.Server.KeepAlive;
            }
            #endregion

            if (packet.ID == PacketType.GetPacketID(PacketType.Server.SetCompression, protocolVersion))
            {
                if (packet.Data.Count <= 5 && packet.Data.Count > 0)
                    return PacketType.Server.SetCompression;

            }
            if (packet.ID == PacketType.GetPacketID(PacketType.Server.LoginSuccess, protocolVersion))
            {
                //如果不是这个包的话,我这样读取会报错的,但是我还需要继续检测下去,所以丢掉异常了
                try
                {
                    //UUID:String(36)
                    //PlayerName:String(16)
                    Packet tmp = new Packet(packet.ID, packet.Data);
                    string UUID = ProtocolHandler.ReadNextString(tmp.Data);
                    string PlayerName = ProtocolHandler.ReadNextString(tmp.Data);
                    if (UUID.Length == 36 && PlayerName.Length > 0 && PlayerName.Length <= 16)
                        return PacketType.Server.LoginSuccess;
                }
                catch { }
            }
            return null;
        }
        public static byte[] ReceiveData(int start, int offset,TcpClient session)
        {
            byte[] buffer = new byte[offset-start];
            Receive(buffer, start, offset, SocketFlags.None, session);
            return buffer;
        }
        public static byte[] ReceiveDate(int start, int offset, ConnectionPayload connectInfo) => ReceiveData(start, offset, connectInfo.Session);
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
            Packet_tmp.ID = ReadNextVarInt(Packet_tmp.Data);
            return Packet_tmp;
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
        private static void Receive(byte[] buffer, int start, int offset, SocketFlags flags, TcpClient tcp)
        {
            int read = 0;
            while (read < offset)
            {
                read += tcp.Client.Receive(buffer, start + read, offset - read, flags);
            }
        }

        #region ReadNext(DataType)
        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>
        public static int ReadNextVarInt(List<byte> cache)
        {
            VarInt result = new VarInt(cache.ToArray(), 0, out int end);
            cache.RemoveRange(0, end);
            return result.ToInt();
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
        #region ToolMethod
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
        private static void GetProtocolVersionNubers()
        {

#pragma warning disable 162
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
#pragma warning restore 162
        }
        #endregion
    }
}
