using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace MinecraftProtocol.Compression
{
    public static class VarLong
    {

        private const int MaskIntSigned = 0b1000_0000_0000_0000_0000_0000;
        private const byte MaskByteSigned = 0b1000_0000;
        private const byte MaskValue = 0b0111_1111;

        public static long Convert(byte[] bytes) => Read(bytes, 0, out _);
        public static long Convert(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static long Convert(byte[] bytes, int offset, out int end) => Read(bytes, offset, out end);
        public static long Convert(List<byte> bytes) => Read(bytes.ToArray(), 0, out _);
        public static long Convert(List<byte> bytes, int offset) => Read(bytes.ToArray(), offset, out _);
        public static long Convert(List<byte> bytes, int offset, out int end) => Read(bytes.ToArray(), offset, out end);
        public static byte[] Convert(long value) => GetBytes(value);


        public static long Read(Stream stream) => Read(stream, out _);
        public static long Read(Stream stream, out int readCount)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            long result = 0;
            for (int i = 0; i < 10; i++)
            {
                int read = stream.ReadByte();
                //某一些流读不了的情况下会给个-1,但是我希望他抛个异常出来，因为我也不知道怎么继续运行下去了
                if ((read & MaskIntSigned) != 0)
                    throw new InvalidDataException("stream.ReadByte return is negative");
                result |= (long)(read & MaskValue) << i * 7;
                if ((read & MaskByteSigned) == 0)
                {
                    readCount = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarLong too big");
        }

        public static long Read(Socket socket) => Read(socket, out _);
        public static long Read(Socket socket, out int readCount)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            long result = 0;
            byte[] buffer = new byte[1];
            for (int i = 0; i < 10; i++)
            {

                socket.Receive(buffer);
                result |= (long)(buffer[0] & MaskValue) << i * 7;
                if ((buffer[0] & MaskByteSigned) == 0)
                {
                    readCount = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarLong too big");
        }
        public static long Read(List<byte> bytes) => Read(bytes.ToArray(), 0, out _);
        public static long Read(List<byte> bytes, int offset) => Read(bytes.ToArray(), offset, out _);
        public static long Read(List<byte> bytes, int offset, out int length) => Read(bytes.ToArray(), offset, out length);
        public static long Read(byte[] bytes) => Read(bytes, 0, out _);
        public static long Read(byte[] bytes, int offset) => Read(bytes, offset, out _);
        public static long Read(byte[] bytes, int offset, out int length)
        {
            long result = 0;
            for (int i = 0; i < 10; i++)
            {
                result |= (long)(bytes[offset + i] & MaskValue) << i * 7;
                if ((bytes[offset + i] & MaskByteSigned) == 0)
                {
                    length = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarLong too big");
        }
        public static long Read(ReadOnlySpan<byte> bytes) => Read(bytes, 0, out _);
        public static long Read(ReadOnlySpan<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static long Read(ReadOnlySpan<byte> bytes, int offset, out int length)
        {
            long result = 0;
            for (int i = 0; i < 10; i++)
            {
                result |= (long)(bytes[offset + i] & MaskValue) << i * 7;
                if ((bytes[offset + i] & MaskByteSigned) == 0)
                {
                    length = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarLong too big");
        }
        public static byte[] GetBytes(long value)
        {
            List<byte> bytes = new List<byte>();
            ulong Value = (ulong)value;
            do
            {
                byte temp = (byte)(Value&MaskValue);
                Value >>= 7;
                if (Value != 0) temp |= MaskByteSigned;
                bytes.Add(temp);

            } while (Value!=0);
            return bytes.ToArray();
        }
        public static int WriteTo(long value, byte[] dest)
        {
            ulong Value = (ulong)value;
            int offset = 0;
            do
            {
                byte temp = (byte)(Value & MaskValue);
                Value >>= 7;
                if (Value != 0) temp |= MaskByteSigned;
                dest[offset++] = temp;

            } while (Value != 0);
            return offset;
        }
        public static int WriteTo(long value, Span<byte> dest)
        {
            ulong Value = (ulong)value;
            int offset = 0;
            do
            {
                byte temp = (byte)(Value & MaskValue);
                Value >>= 7;
                if (Value != 0) temp |= MaskByteSigned;
                dest[offset++] = temp;

            } while (Value != 0);
            return offset;
        }
        public static int WriteTo(long value, List<byte> dest)
        {
            ulong Value = (ulong)value;
            int offset = 0;
            do
            {
                byte temp = (byte)(Value & MaskValue);
                Value >>= 7;
                if (Value != 0) temp |= MaskByteSigned;
                dest[offset++] = temp;

            } while (Value != 0);
            return offset;
        }


        public static int GetLength(long value)
        {
            ulong temp = (ulong)value;
            if ((temp >> 07) == 0) return 1;
            if ((temp >> 14) == 0) return 2;
            if ((temp >> 21) == 0) return 3;
            if ((temp >> 28) == 0) return 4;
            if ((temp >> 35) == 0) return 5;
            if ((temp >> 42) == 0) return 6;
            if ((temp >> 49) == 0) return 7;
            if ((temp >> 56) == 0) return 8;
            if ((temp >> 63) == 0) return 8;
            else return 10;
        }
    }
}
