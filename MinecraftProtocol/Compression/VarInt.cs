using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace MinecraftProtocol.Compression
{

    public static class VarInt
    {
        public delegate byte ReadByteHandle();

        private const int MaskIntSigned = 0b1000_0000_0000_0000_0000_0000;
        private const byte MaskByteSigned = 0b1000_0000;
        private const byte MaskValue = 0b0111_1111;

        public static int Convert(ReadOnlySpan<byte> bytes) => Read(bytes,out _);
        public static int Convert(ReadOnlySpan<byte> bytes, out int length) => Read(bytes, out length);
        public static int Convert(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static int Convert(byte[] bytes, int offset, out int end) => Read(bytes, offset, out end);
        public static int Convert(List<byte> bytes) => Read(bytes.ToArray(), 0, out _);
        public static int Convert(List<byte> bytes, int offset) => Read(bytes.ToArray(), offset, out _);
        public static int Convert(List<byte> bytes, int offset, out int end) => Read(bytes.ToArray(), offset, out end);
        public static byte[] Convert(int value) => GetBytes(value);


        public static int Read(Stream stream) => Read(stream, out _);
        public static int Read(Stream stream, out int readCount) => Read(() => { int read = stream.ReadByte(); return read >= 0 ? (byte)read : throw new InvalidDataException("negative"); }, out readCount);
        public static int Read(Socket socket) => Read(socket, out _);
        public static int Read(Socket socket, out int readCount) => Read(() => { byte[] buffer = new byte[1]; socket.Receive(buffer); return buffer[0]; }, out readCount);
        public static int Read(ReadByteHandle read) => Read(read, out _);
        public static int Read(ReadByteHandle read, out int readCount)
        {
            if (read == null)
                throw new ArgumentNullException(nameof(read));

            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                byte b = read();
                result |= (b & MaskValue) << i * 7;
                if ((b & MaskByteSigned) == 0)
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
        public static int Read(ReadOnlySpan<byte> bytes) => Read(bytes, out _);
        public static int Read(ReadOnlySpan<byte> bytes, out int length)
        {
            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                result |= (bytes[i] & MaskValue) << i * 7;
                if ((bytes[i] & MaskByteSigned) == 0)
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
