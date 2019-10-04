using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace MinecraftProtocol.Compression
{
    
    public static class VarInt
    {
        private const int MaskIntSigned = 0b1000_0000_0000_0000_0000_0000;
        private const byte MaskByteSigned = 0b1000_0000;
        private const byte MaskValue = 0b0111_1111;

        public static int Convert(byte[] bytes) => Read(bytes, 0, out _);
        public static int Convert(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static int Convert(byte[] bytes, int offset, out int end) => Read(bytes, offset, out end);
        public static int Convert(List<byte> bytes) => Read(bytes.ToArray(), 0, out _);
        public static int Convert(List<byte> bytes, int offset) => Read(bytes.ToArray(), offset, out _);
        public static int Convert(List<byte> bytes, int offset, out int end) => Read(bytes.ToArray(), offset, out end);
        public static byte[] Convert(int value) => GetBytes(value);


        public static int Read(Stream stream) => Read(stream, out _);
        public static int Read(Stream stream, out int readCount)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                int read = stream.ReadByte();
                //某一些流读不了的情况下会给个-1,但是我希望他抛个异常出来，因为我也不知道怎么继续运行下去了
                if ((read & MaskIntSigned) != 0)
                    throw new InvalidDataException("stream.ReadByte return is negative");
                result |= (read & MaskValue) << i * 7;
                if ((read & MaskByteSigned) == 0)
                {
                    readCount = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarInt too big");
        }
        public static int Read(Socket socket) => Read(socket, out _);
        public static int Read(Socket socket, out int readCount)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            byte[] buffer = new byte[1];
            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                socket.Receive(buffer);
                result |= (buffer[0] & MaskValue) << i * 7;
                if ((buffer[0] & MaskByteSigned) == 0)
                {
                    readCount = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarInt too big");
        }
        public static int Read(List<byte> bytes) => Read(bytes.ToArray(), 0, out _);
        public static int Read(List<byte> bytes, int offset) => Read(bytes.ToArray(), offset, out _);
        public static int Read(List<byte> bytes, int offset, out int length) => Read(bytes.ToArray(), offset, out length);
        public static int Read(byte[] bytes) => Read(bytes, 0, out _);
        public static int Read(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static int Read(byte[] bytes, int offset, out int length)
        {
            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                result |= (bytes[offset + i] & MaskValue) << i * 7;
                if ((bytes[offset + i] & MaskByteSigned) == 0)
                {
                    length = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarInt too big");
        }
        public static int Read(ReadOnlySpan<byte> bytes) => Read(bytes, 0, out _);
        public static int Read(ReadOnlySpan<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static int Read(ReadOnlySpan<byte> bytes, int offset, out int length)
        {
            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                result |= (bytes[offset + i] & MaskValue) << i * 7;
                if ((bytes[offset + i] & MaskByteSigned) == 0)
                {
                    length = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarInt too big");
        }

        public static byte[] GetBytes(int value)
        {
            List<byte> bytes = new List<byte>();
            while ((value & -128) != 0)
            {
                bytes.Add((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            bytes.Add((byte)value);
            return bytes.ToArray();
        }


        public static int WriteTo(int value, byte[] dest)
        {
            int offset = 0;
            while ((value & -128) != 0)
            {
                dest[offset++] = ((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            dest[offset++] = (byte)value;
            return offset;
        }
        public static int WriteTo(int value, Span<byte> dest)
        {
            int offset = 0;
            while ((value & -128) != 0)
            {
                dest[offset++] = ((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            dest[offset++] = (byte)value;
            return offset;
        }
        public static int WriteTo(int value, List<byte> dest)
        {
            int offset = 0;
            while ((value & -128) != 0)
            {
                dest[offset++] = ((byte)(value & 127 | 128));
                value = (int)(((uint)value) >> 7);
            }
            dest[offset++] = (byte)value;
            return offset;
        }

        public static int GetLength(int value)
        {
            uint temp = (uint)value;
            if ((temp & 0xFFFFFFF80) == 0) return 1;
            if ((temp >> 14) == 0) return 2;
            if ((temp >> 21) == 0) return 3;
            if ((temp >> 28) == 0) return 4;
            else return 5;

        }
    }
}
