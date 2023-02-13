using MinecraftProtocol.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace MinecraftProtocol.Compression
{
    /// <summary>
    /// Copy From: https://github.com/MinecraftForge/MinecraftForge/blob/ebe9b6d4cbc4a5281c386994f1fbda04df5d2e1f/src/main/java/net/minecraftforge/fml/common/network/ByteBufUtils.java#L58-L89
    /// </summary>
    public static class VarShort
    {
        //懒的去看懂VarShort了
        //所以一些地方可能和隔壁的两个长的不一样

        public static int Convert(ReadOnlySpan<byte> bytes) => Read(bytes,out _);
        public static int Convert(ReadOnlySpan<byte> bytes, out int length) => Read(bytes,out length);
        public static int Convert(byte[] bytes) => Read(bytes as IList<byte>, 0, out _);
        public static int Convert(byte[] bytes, int offset) => Read(bytes as IList<byte>, offset, out _);
        public static int Convert(byte[] bytes, int offset, out int end) => Read(bytes as IList<byte>, offset, out end);
        public static int Convert(IList<byte> bytes) => Read(bytes, 0, out _);
        public static int Convert(IList<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static int Convert(IList<byte> bytes, int offset, out int end) => Read(bytes, offset, out end);
        public static byte[] Convert(int value) => GetBytes(value);

        public static int Read(Stream stream) => Read(stream, out _);
        public static int Read(Stream stream, out int readCount)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            return Read((c) =>
            {
                byte[] buffer = new byte[c];
                stream.Read(buffer);
                return buffer;

            }, out readCount);
        }
        public static int Read(Socket socket) => Read(socket, out _);
        public static int Read(Socket socket, out int readCount) => Read((c) => NetworkUtils.ReceiveDataAsync(socket, c).Result, out readCount);
        public static int Read(Func<int,byte[]> readBytes, out int readCount)
        {
            byte[] buffer = readBytes(2);
            readCount = 2;
            ushort low = (ushort)(buffer[0] << 8 | buffer[1]);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                buffer = readBytes(1);
                high = buffer[0];
                readCount++;
            }
            return ((high & 0xFF) << 15) | low;
        }

        public static int Read(byte[] bytes) => Read(bytes as IList<byte>, 0, out _);
        public static int Read(byte[] bytes, int offset) => Read(bytes as IList<byte>, offset, out _);
        public static int Read(byte[] bytes, int offset, out int length) => Read(bytes as IList<byte>, offset, out length);
        public static int Read(IList<byte> bytes) => Read(bytes, 0, out _);
        public static int Read(IList<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static int Read(IList<byte> bytes, int offset, out int length)
        {
            length = 2;
            ushort low = (ushort)(bytes[offset] << 8 | bytes[offset + 1]);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = bytes[offset + 2];
                length++;
            }
            return ((high & 0xFF) << 15) | low;
        }
        public static int Read(ReadOnlySpan<byte> bytes) => Read(bytes, out _);
        public static int Read(ReadOnlySpan<byte> bytes, out int length)
        {
            length = 2;
            ushort low = (ushort)(bytes[0] << 8 | bytes[1]);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = bytes[2];
                length++;
            }
            return ((high & 0xFF) << 15) | low;
        }

        

        public static Span<byte> GetSpan(int value)
        {
            byte[] buffer = new byte[3];
            
            int low = value & 0x7FFF;
            int high = (value & 0x7F8000) >> 15;
            if (high != 0)
                low |= 0x8000;
            buffer[0] = (byte)(low >> 8);
            buffer[1] = (byte)(low);
            if (high != 0)
            {
                buffer[2] = (byte)high;
                return buffer;
            }
            return buffer.AsSpan().Slice(0,2);
        }

        public static byte[] GetBytes(int value)
        {
            return GetSpan(value).ToArray();
        }


        public static int GetLength(byte[] bytes, int offset = 0) => GetLength(bytes as IList<byte>, offset);
        public static int GetLength(IList<byte> bytes, int offset = 0)
        {
            if (bytes.Count >= offset + 3 && ((bytes[offset] << 8 | bytes[offset + 1]) & 0x8000) != 0)
                return 3;
            else
                return bytes.Count >= offset + 2 ? 2 : throw new OverflowException("VarShort too small");
        }
        public static int GetLength(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= 3 && ((bytes[0] << 8 | bytes[1]) & 0x8000) != 0)
                return 3;
            else
                return bytes.Length >= 2 ? 2 : throw new OverflowException("VarShort too short");
        }
        public static int GetLength(int value)
        {
            if ((value & 0x7F8000) >> 15 != 0)
                return 3;
            else
                return 2;
        }
    }
}
