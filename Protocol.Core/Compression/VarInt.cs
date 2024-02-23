using MinecraftProtocol.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace MinecraftProtocol.Compression
{

    public static class VarInt
    {

        private const byte MaskByteSigned = 0b1000_0000;
        private const byte MaskValue = 0b0111_1111;

        public static int Convert(ReadOnlySpan<byte> bytes) => Read(bytes,out _);
        public static int Convert(ReadOnlySpan<byte> bytes, out int length) => Read(bytes, out length);
     
        public static int Convert(IList<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static int Convert(IList<byte> bytes, int offset, out int end) => Read(bytes, offset, out end);
        public static byte[] Convert(int value) => GetBytes(value);


        public static int Read(Stream stream) => Read(stream, out _);
        public static int Read(Stream stream, out int readCount) => Read(() => { int read = stream.ReadByte(); return read >= 0 ? (byte)read : throw new InvalidDataException("negative"); }, out readCount);
        public static int Read(Socket socket) => Read(socket, out _);
        public static int Read(Socket socket, out int readCount) => Read(() => NetworkUtils.ReceiveDataAsync(socket, 1).Result[0], out readCount);
        public static int Read(Func<byte> readByte) => Read(readByte, out _);
        public static int Read(Func<byte> readByte, out int readCount)
        {
            if (readByte == null)
                throw new ArgumentNullException(nameof(readByte));

            int result = 0;
            for (int i = 0; i < 5; i++)
            {
                byte b = readByte();
                result |= (b & MaskValue) << i * 7;
                if ((b & MaskByteSigned) == 0)
                {
                    readCount = i + 1;
                    return result;
                }
            }
            throw new OverflowException("VarInt too big");
        }

        public static int Read(byte[] bytes, int offset) => Read(bytes as IList<byte>, offset, out _);
        public static int Read(byte[] bytes, int offset, out int length) => Read(bytes as IList<byte>, offset, out length);

        public static int Read(IList<byte> bytes, int offset) => Read(bytes, offset, out _);
        public static int Read(IList<byte> bytes, int offset, out int length)
        {
            if (bytes == null|| bytes.Count==0)
                throw new ArgumentNullException(nameof(bytes));
            
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


        public static Span<byte> GetSpan(int value)
        {
            byte[] bytes = new byte[5];
            byte offset = 0;
            while ((value & -128) != 0)
            {
                bytes[offset++] = (byte)(value & 127 | 128);
                value = (int)(((uint)value) >> 7);
            }
            bytes[offset++] = (byte)value;
            return offset == 5 ? bytes : bytes.AsSpan().Slice(0, offset);
        }

        public static byte[] GetBytes(int value)
        {
            return GetSpan(value).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
