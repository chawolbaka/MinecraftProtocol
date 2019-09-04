using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol.Packets;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol
{
    public static class ProtocolHandler
    {
        /*
         * 数据包格式(https://github.com/bangbang93/minecraft-protocol/blob/master/protocol.md)
         * 
         * 未压缩的数据包:
         * 
         * 名称      类型
         * 数据包长度 Varint
         * 数据包ID  Varint
         * 数据
         * 
         * 压缩的数据包:
         * 
         * 名称      类型     备注
         * 数据包长度 Varint  等于下方两者解压前的总字节数
         * 解压后长度 Varint  若为0则表示本包未被压缩
         * 被压缩的数据       经Zlib压缩.开头是一个VarInt字段,代表数据包ID,然后是数据包数据.
         */

        public static int GetPacketLength(TcpClient session) => GetPacketLength(session.Client);
        /// <summary>获取数据包的长度</summary>
        public static int GetPacketLength(Socket session)
        {
            //0x7F=127 0x80=128
            int length = 0;
            int readCount = 0;
            //这边的范围本来是5,可是下面每次就读一个。后面的4个byte根本用不到吧
            byte[] tmp = new byte[1];
            while (true)
            {
                Receive(tmp, 0, 1, SocketFlags.None, session);
                length |= (tmp[0] & 0x7F) << readCount++ * 7;
                if (readCount > 5) throw new OverflowException("VarInt too big");
                if ((tmp[0] & 0x80) != 128) break;
            }
            return length;
        }
        public static byte[] ReceiveData(int start, int offset, Socket session)
        {
            byte[] buffer = new byte[offset - start];
            Receive(buffer, start, offset, SocketFlags.None, session);
            return buffer;
        }
        public static Packet ReceivePacket(Socket session, int compressionThreshold)
        {
            //写这个方法的时候Data属性暂时改成了可写的,我当初是为了什么设置成只读的?
            //先去睡觉了,醒来后想想看要不要改回去,为什么要只读这两个问题
            Packet recPacket = new Packet();
            int PacketLength = ProtocolHandler.GetPacketLength(session);
            recPacket.WriteBytes(ReceiveData(0, PacketLength, session));
            if (compressionThreshold > 0)
            {
                int DataLength = ReadNextVarInt(recPacket.Data);
                if (DataLength != 0) //如果是0的话就代表这个数据包没有被压缩
                {
                    byte[] uncompressed = ZlibUtils.Decompress(recPacket.Data.ToArray(), DataLength);
                    recPacket.Data.Clear();
                    recPacket.Data.AddRange(uncompressed);
                }
            }
            recPacket.ID = ReadNextVarInt(recPacket.Data);
            return recPacket;
        }
        /// <summary>
        /// 从TCP在协议栈里面的缓存中取出数据
        /// </summary>
        /// <param name="buffer">取出来的数据</param>
        /// <param name="start">从x开始读取</param>
        /// <param name="offset">读取到x结束</param>
        private static void Receive(byte[] buffer, int start, int offset, SocketFlags flags, Socket tcp)
        {
            int read = 0;
            while (read < offset)
            {
                read += tcp.Receive(buffer, start + read, offset - read, flags);
            }
        }
        #region ReadDataAndRemove
        /// <summary> Read a varint from a cache of bytes and remove it from the cache</summary>
        public static int ReadNextVarInt(List<byte> cache)
        {
            VarInt result = new VarInt(cache.ToArray(), 0, out int end);
            cache.RemoveRange(0, end);
            return result.ToInt();
        }
        /// <summary> Read a unsigned short from a cache of bytes and remove it from the cache</summary>
        public static ushort ReadNextUnsignedShort(List<byte> cache)
        {
            const int INT16_LENGTH = 2;
            byte[] result = cache.Take(INT16_LENGTH).Reverse().ToArray();
            cache.RemoveRange(0, INT16_LENGTH);
            return BitConverter.ToUInt16(result, 0);
        }
        /// <summary> Read a int32 from a cache of bytes and remove it from the cache</summary>
        public static long ReadNextInt(List<byte> cache)
        {
            const int INT32_LENGTH = 4;
            byte[] result = cache.Take(INT32_LENGTH).Reverse().ToArray();
            cache.RemoveRange(0, INT32_LENGTH);
            return BitConverter.ToInt32(result, 0);
        }
        /// <summary> Read a Long from a cache of bytes and remove it from the cache</summary>
        public static long ReadNextLong(List<byte> cache)
        {
            const int INT64_LENGTH = 8;
            byte[] result = cache.Take(INT64_LENGTH).Reverse().ToArray();
            cache.RemoveRange(0, INT64_LENGTH);
            return BitConverter.ToInt64(result, 0);
        }
        /// <summary> Read a string from a cache of bytes and remove it from the cache</summary>
        public static string ReadNextString(List<byte> cache)
        {
            //这边索引可能有问题,我现在懒的思考
            int length = ReadNextVarInt(cache);
            string result = Encoding.UTF8.GetString(cache.Take(length).ToArray());
            cache.RemoveRange(0, length);
            return result;
        }
        /// <summary> Read a bool from a cache of bytes and remove it from the cache</summary>
        public static bool ReadNextBoolean(List<byte> cache)
        {
            bool result = cache[0] == 0x01 ? true : false;
            cache.RemoveAt(0);
            return result;
        }
        /// <summary> Read a single byte from a cache of bytes and remove it from the cache</summary>
        public static byte ReadNextByte(List<byte> cache)
        {
            byte result = cache[0];
            cache.RemoveAt(0);
            return result;
        }
        public static byte[] ReadData(int offset, List<byte> cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache.RemoveRange(0, offset);
            return result;
        }
        #endregion
        #region ReadDataWithoutRemove
        /// <summary> Read a varint from a cache of bytes</summary>
        public static int ReadVarInt(List<byte> cache) => ReadVarInt(cache, 0, out _);
        /// <summary> Read a varint from a cache of bytes</summary>
        public static int ReadVarInt(List<byte> cache, out int end) => ReadVarInt(cache, 0, out end);
        /// <summary> Read a varint from a cache of bytes</summary>
        public static int ReadVarInt(List<byte> cache, int offset) => ReadVarInt(cache, offset, out _);
        /// <summary> Read a varint from a cache of bytes</summary>
        public static int ReadVarInt(List<byte> cache, int offset, out int end)
        {
            VarInt result = new VarInt(cache.ToArray(), offset, out end);
            return result.ToInt();
        }
        /// <summary> Read a unsigned short from a cache of bytes</summary>
        public static ushort ReadUnsignedShort(List<byte> cache) => ReadUnsignedShort(cache, 0);
        /// <summary> Read a unsigned short from a cache of bytes</summary>
        public static ushort ReadUnsignedShort(List<byte> cache, int offset)
        {
            const int INT16_LENGTH = 2;
            byte[] result;
            if (offset > 0)
            {
                List<byte> buffer = new List<byte>(cache);
                buffer.RemoveRange(0, offset);
                result = buffer.Take(INT16_LENGTH).Reverse().ToArray();
            }
            else
                result = cache.Take(INT16_LENGTH).Reverse().ToArray();
            return BitConverter.ToUInt16(result, 0);
        }
        /// <summary> Read a int32 from a cache of bytes</summary>
        public static long ReadInt(List<byte> cache) => ReadInt(cache, 0);
        /// <summary> Read a int32 from a cache of bytes</summary>
        public static long ReadInt(List<byte> cache, int offset)
        {
            const int INT32_LENGTH = 4;
            byte[] result;
            if (offset > 0)
            {
                List<byte> buffer = new List<byte>(cache);
                buffer.RemoveRange(0, offset);
                result = buffer.Take(INT32_LENGTH).Reverse().ToArray();
            }
            else
                result = cache.Take(INT32_LENGTH).Reverse().ToArray();
            return BitConverter.ToInt32(result, 0);
        }
        /// <summary> Read a Long from a cache of bytes</summary>
        public static long ReadLong(List<byte> cache) => ReadLong(cache, 0);
        /// <summary> Read a Long from a cache of bytes</summary>
        public static long ReadLong(List<byte> cache, int offset)
        {
            const int INT64_LENGTH = 8;
            byte[] result;
            if (offset > 0)
            {
                List<byte> buffer = new List<byte>(cache);
                buffer.RemoveRange(0, offset);
                result = buffer.Take(INT64_LENGTH).Reverse().ToArray();
            }
            else
                result = cache.Take(INT64_LENGTH).Reverse().ToArray();
            return BitConverter.ToInt64(result, 0);
        }
        /// <summary> Read a string from a cache of bytes</summary>
        public static string ReadString(List<byte> cache) => ReadString(cache, 0);
        /// <summary> Read a string from a cache of bytes</summary>
        public static string ReadString(List<byte> cache, int offset)
        {
            int length = ReadVarInt(cache, offset, out int end);
            List<byte> buffer = new List<byte>(cache);
            buffer.RemoveRange(0, end + offset);
            return Encoding.UTF8.GetString(buffer.Take(length).ToArray());
        }
        /// <summary> Read a bool from a cache of bytes</summary>
        public static bool ReadBoolean(List<byte> cache) => ReadBoolean(cache, 0);
        /// <summary> Read a bool from a cache of bytes</summary>
        public static bool ReadBoolean(List<byte> cache, int offset)
        {
            bool result = cache[offset] == 0x01 ? true : false;
            return result;
        }
        /// <summary> Read a single byte from a cache of bytes</summary>
        public static byte ReadByte(List<byte> cache) => ReadByte(cache, 0);
        /// <summary> Read a single byte from a cache of bytes</summary>
        public static byte ReadByte(List<byte> cache, int offset)
        {
            return cache[offset];
        }
        #endregion

        /// <summary>拼接Byte数组</summary>
        public static byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> buffer = new List<byte>();
            foreach (byte[] array in bytes)
            {
                if (array == null)
                    continue;
                else
                    buffer.AddRange(array);
            }
            return buffer.ToArray();
        }
    }
}