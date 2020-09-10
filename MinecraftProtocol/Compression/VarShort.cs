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
        public static int Convert(ReadOnlySpan<byte> bytes) => Read(bytes,out _);
        public static int Convert(ReadOnlySpan<byte> bytes, out int length) => Read(bytes,out length);
        public static int Convert(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static int Convert(byte[] bytes, int offset, out int length) => Read(bytes, offset, out length);
        public static int Convert(List<byte> bytes) => Read(bytes, 0, out _);
        public static int Convert(List<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static int Convert(List<byte> bytes, int offset, out int length) => Read(bytes, offset, out length);
        public static byte[] Convert(int value) => GetBytes(value);

        public static int Read(Stream stream) => Read(stream, out _);
        public static int Read(Stream stream, out int readCount)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = new byte[2];
            stream.Read(buffer);
            readCount = 2;
            ushort low = (ushort)(buffer[0] << 8 | buffer[1]);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = (byte)stream.ReadByte();
                readCount++;
            }
            return ((high & 0xFF) << 15) | low;
        }
        public static int Read(Socket socket) => Read(socket, out _);
        public static int Read(Socket socket, out int readCount)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            byte[] buffer = new byte[2];
            socket.Receive(buffer);
            readCount = 2;
            ushort low = (ushort)(buffer[0] << 8 | buffer[1]);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                buffer = new byte[1];
                socket.Receive(buffer);
                high = buffer[0];
                readCount++;
            }
            return ((high & 0xFF) << 15) | low;
        }
        public static int Read(List<byte> bytes) => Read(bytes.ToArray(), 0, out _);
        public static int Read(List<byte> bytes, int offset) => Read(bytes.ToArray(), offset, out _);
        public static int Read(List<byte> bytes, int offset, out int length) => Read(bytes.ToArray(), offset, out length);
        public static int Read(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static int Read(byte[] bytes, int offset, out int length)
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
        public static byte[] GetBytes(int value)
        {
            List<byte> buffer = new List<byte>();
            int low = value & 0x7FFF;
            int high = (value & 0x7F8000) >> 15;
            if (high != 0)
                low |= 0x8000;
            buffer.Add((byte)(low >> 8));
            buffer.Add((byte)(low));
            if (high != 0)
                buffer.Add((byte)high);
            return buffer.ToArray();
        }

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
