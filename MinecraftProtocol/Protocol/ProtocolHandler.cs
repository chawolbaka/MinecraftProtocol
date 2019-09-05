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
        public static byte[] ReceiveData(int start, int offset, Socket socket)
        {
            byte[] buffer = new byte[offset - start];
            Receive(buffer, start, offset, SocketFlags.None, socket);
            return buffer;
        }
        public static Packet ReceivePacket(Socket session,int compressionThreshold)
        {
            //写这个方法的时候Data属性暂时改成了可写的,我当初是为了什么设置成只读的?
            //先去睡觉了,醒来后想想看要不要改回去,为什么要只读这两个问题
            Packet recPacket = new Packet();
            int PacketLength = ProtocolHandler.GetPacketLength(session);
            recPacket.WriteBytes(ReceiveData(0, PacketLength, session));
            if (compressionThreshold > 0)
            {
                int DataLength = ReadVarInt(recPacket.Data);
                if (DataLength != 0) //如果是0的话就代表这个数据包没有被压缩
                {
                    byte[] uncompressed = ZlibUtils.Decompress(recPacket.Data.ToArray(), DataLength);
                    recPacket.Data.Clear();
                    recPacket.Data.AddRange(uncompressed);
                }
            }
            recPacket.ID = ReadVarInt(recPacket.Data);
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


        public static bool ReadBoolean(List<byte> cache, bool readOnly = false) => ReadBoolean(cache, 0, out _, readOnly);
        public static bool ReadBoolean(List<byte> cache, int offset, bool readOnly = false) => ReadBoolean(cache, offset, out _, readOnly);
        public static bool ReadBoolean(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            bool result = cache[offset] == 0x01 ? true : false;
            count = offset + 1;
            if (!readOnly)
                cache.RemoveAt(offset);
            return result;
        }

        public static sbyte ReadByte(List<byte> cache, bool readOnly = false) => ReadByte(cache, 0, out _, readOnly);
        public static sbyte ReadByte(List<byte> cache, int offset, bool readOnly = false) => ReadByte(cache, offset, out _, readOnly);
        public static sbyte ReadByte(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            sbyte sb = (sbyte)cache[offset];
            count = offset + 1;
            if (!readOnly)
                cache.RemoveAt(offset);
            return sb;
        }

        public static byte ReadUnsignedByte(List<byte> cache, bool readOnly = false) => ReadUnsignedByte(cache, 0, out _, readOnly);
        public static byte ReadUnsignedByte(List<byte> cache, int offset, bool readOnly = false) => ReadUnsignedByte(cache, offset, out _, readOnly);
        public static byte ReadUnsignedByte(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            byte b = cache[offset];
            count = offset + 1;
            if (!readOnly)
                cache.RemoveAt(offset);
            return b;
        }

        public static short ReadShort(List<byte> cache, bool readOnly = false) => ReadShort(cache, 0, out _, readOnly);
        public static short ReadShort(List<byte> cache, int offset, bool readOnly = false) => ReadShort(cache, offset, out _, readOnly);
        public static short ReadShort(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            short result = (short)(cache[offset] << 8 | cache[offset + 1]);
            count = offset + 2;
            if (!readOnly)
                cache.RemoveRange(offset, 2);
            return result;
        }

        public static ushort ReadUnsignedShort(List<byte> cache, bool readOnly = false) => ReadUnsignedShort(cache, 0, out _, readOnly);
        public static ushort ReadUnsignedShort(List<byte> cache, int offset, bool readOnly = false) => ReadUnsignedShort(cache, 0, out _, readOnly);
        public static ushort ReadUnsignedShort(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            ushort result = (ushort)(cache[offset] << 8 | cache[offset + 1]);
            count = offset + 2;
            if (!readOnly)
                cache.RemoveRange(offset, 2);
            return result;
        }

        public static int ReadInt(List<byte> cache, bool readOnly = false) => ReadInt(cache, 0, out _, readOnly);
        public static int ReadInt(List<byte> cache, int offset, bool readOnly = false) => ReadInt(cache, offset, out _, readOnly);
        public static int ReadInt(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            int result = 
                cache[offset]   << 24 |
                cache[offset+1] << 16 |
                cache[offset+2] << 08 |
                cache[offset+3];
            count = offset + 4;
            if (!readOnly)
                cache.RemoveRange(offset, 4);
            return result;
        }

        public static long ReadLong(List<byte> cache, bool readOnly = false) => ReadLong(cache, 0, out _, readOnly);
        public static long ReadLong(List<byte> cache, int offset, bool readOnly = false) => ReadLong(cache, offset, out _, readOnly);
        public static long ReadLong(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            long result =
                ((long)cache[offset])   << 56 |
                ((long)cache[offset+1]) << 48 |
                ((long)cache[offset+2]) << 40 |
                ((long)cache[offset+3]) << 32 |
                ((long)cache[offset+4]) << 24 |
                ((long)cache[offset+5]) << 16 |
                ((long)cache[offset+6]) << 08 |
                cache[offset + 7];

            count = offset + 8;
            if (!readOnly)
                cache.RemoveRange(offset, 8);
            return result;
        }

        public static float ReadFloat(List<byte> cache, bool readOnly = false) => ReadFloat(cache, 0, out _, readOnly);
        public static float ReadFloat(List<byte> cache, int offset, bool readOnly = false) => ReadFloat(cache, offset, out _, readOnly);
        public static float ReadFloat(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            count = offset + size;
            for (int i = 0; i < size; i++)
                buffer[i] = cache[offset+3-i];
            if (!readOnly)
                cache.RemoveRange(offset, size);
            return BitConverter.ToSingle(buffer);
        }

        public static double ReadDouble(List<byte> cache, bool readOnly = false) => ReadDouble(cache, 0, out _, readOnly);
        public static double ReadDouble(List<byte> cache, int offset, bool readOnly = false) => ReadDouble(cache, offset, out _, readOnly);
        public static double ReadDouble(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            count = offset + size;
            for (int i = 0; i <size; i++)
                buffer[i] = cache[offset+7-i];
            if (!readOnly)
                cache.RemoveRange(offset, size);
            return BitConverter.ToDouble(buffer);
        }

        public static string ReadString(List<byte> cache, bool readOnly = false) => ReadString(cache, 0, out _, readOnly);
        public static string ReadString(List<byte> cache, int offset, bool readOnly = false) => ReadString(cache, offset, out _, readOnly);
        public static string ReadString(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            int length = ReadVarInt(cache, offset, out int EndPos, true);
            byte[] buffer = new byte[length];
            for (int i = EndPos; i < length + EndPos; i++)
                buffer[i - EndPos] = cache[i];
            string result = Encoding.UTF8.GetString(buffer.ToArray());
            count = EndPos + length;
            if (!readOnly)
                cache.RemoveRange(offset, length + (EndPos - offset));
            return result;
        }

        public static int ReadVarInt(List<byte> cache, bool readOnly = false) => ReadVarInt(cache, 0, out _, readOnly);
        public static int ReadVarInt(List<byte> cache, int offset, bool readOnly = false) => ReadVarInt(cache, offset, out _, readOnly);
        public static int ReadVarInt(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            VarInt result = new VarInt(cache.ToArray(), offset, out count);
            if (!readOnly)
                cache.RemoveRange(offset, count - offset);
            return result.ToInt();
        }

        public static long ReadVarLong(List<byte> cache, bool readOnly = false) => ReadVarLong(cache, 0, out _, readOnly);
        public static long ReadVarLong(List<byte> cache, int offset, bool readOnly = false) => ReadVarLong(cache, offset, out _, readOnly);
        public static long ReadVarLong(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            throw new NotImplementedException();
        }

        public static UUID ReadUUID(List<byte> cache, bool readOnly = false) => ReadUUID(cache, 0, out _, readOnly);
        public static UUID ReadUUID(List<byte> cache, int offset, bool readOnly = false) => ReadUUID(cache, offset, out _, readOnly);
        public static UUID ReadUUID(List<byte> cache, int offset, out int count, bool readOnly = false)
        {
            throw new NotImplementedException();
        }

        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, bool readOnly = false) => ReadByteArray(cache, protocolVersion, 0, out _, readOnly);
        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, int offset, bool readOnly = false) => ReadByteArray(cache, protocolVersion, offset, out _, readOnly);
        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, out int count, bool readOnly = false) => ReadByteArray(cache, protocolVersion, 0, out count, readOnly);
        public static byte[] ReadByteArray(List<byte> cache, int protocolVersion, int offset, out int count, bool readOnly = false)
        {
            int ArrayLength;
            int EndPos;
            if (protocolVersion>=ProtocolVersionNumbers.V14w21a)
                ArrayLength = ReadVarInt(cache, offset, out EndPos, true);
            else
                ArrayLength = ReadShort(cache, offset, out EndPos, true);
            count = EndPos + ArrayLength;
            byte[] buffer = new byte[ArrayLength];
            for (int i = 0; i < ArrayLength; i++)
                buffer[i] = cache[EndPos+i];
            if (!readOnly)
                cache.RemoveRange(offset, ArrayLength+(EndPos-offset));
            return buffer;
        }
        
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
