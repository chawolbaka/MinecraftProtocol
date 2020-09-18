using System;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.Compression;

namespace MinecraftProtocol.IO.Extensions
{
    public static class SpanConvert
    {
        public static ReadOnlySpan<byte> ReadBoolean(this ReadOnlySpan<byte> span, out bool @bool)
        {
            @bool = span[0] == 0x01;
            return span.Slice(1);
        }
        public static ReadOnlySpan<byte> ReadByte(this ReadOnlySpan<byte> span, out sbyte @sbyte)
        {
            @sbyte = (sbyte)span[0];
            return span.Slice(1);
        }
        public static ReadOnlySpan<byte> ReadUnsignedByte(this ReadOnlySpan<byte> span, out byte @byte)
        {
            @byte = span[0];
            return span.Slice(1);
        }
        public static ReadOnlySpan<byte> ReadShort(this ReadOnlySpan<byte> span, out short @short)
        {
            @short = (short)(span[0] << 8 | span[1]);
            return span.Slice(2);
        }
        public static ReadOnlySpan<byte> ReadUnsignedShort(this ReadOnlySpan<byte> span, out ushort @ushort)
        {
            @ushort = (ushort)(span[0] << 8 | span[1]);
            return span.Slice(2);
        }
        public static ReadOnlySpan<byte> ReadInt(this ReadOnlySpan<byte> span, out int @int)
        {
            @int = span[0] << 24 | span[1] << 16 | span[2] << 08 | span[3];
            return span.Slice(4);
        }
        public static ReadOnlySpan<byte> ReadLong(this ReadOnlySpan<byte> span, out long @long)
        {
            @long = ((long)span[0]) << 56 |
                    ((long)span[1]) << 48 |
                    ((long)span[2]) << 40 |
                    ((long)span[3]) << 32 |
                    ((long)span[4]) << 24 |
                    ((long)span[5]) << 16 |
                    ((long)span[6]) << 08 |
                    span[7];
            return span.Slice(8);
        }
        public static ReadOnlySpan<byte> ReadFloat(this ReadOnlySpan<byte> span, out float @float)
        {
            Span<byte> buffer = span.Slice(0, 4).ToArray();
            buffer.Reverse();
            @float = BitConverter.ToSingle(buffer);
            return span.Slice(4);
        }
        public static ReadOnlySpan<byte> ReadDouble(this ReadOnlySpan<byte> span, out double @double)
        {
            Span<byte> buffer = span.Slice(0, 8).ToArray();
            buffer.Reverse();
            @double = BitConverter.ToDouble(buffer);
            return span.Slice(8);
        }
        public static ReadOnlySpan<byte> ReadString(this ReadOnlySpan<byte> span, out string @string)
        {
            ReadOnlySpan<byte> buffer = span.ReadVarInt(out int length);
            @string = Encoding.UTF8.GetString(buffer.Slice(0, length));
            return buffer.Slice(length);
        }
        public static ReadOnlySpan<byte> ReadVarShort(this ReadOnlySpan<byte> span, out int varShort)
        {
            varShort = VarShort.Read(span, out int offset);
            return span.Slice(offset);
        }
        public static ReadOnlySpan<byte> ReadVarInt(this ReadOnlySpan<byte> span, out int varInt)
        {
            varInt = VarInt.Read(span, out int offset);
            return span.Slice(offset);
        }
        public static ReadOnlySpan<byte> ReadVarLong(this ReadOnlySpan<byte> span, out long varLong)
        {
            varLong = VarLong.Read(span, out int offset);
            return span.Slice(offset);
        }


        public static ReadOnlySpan<byte> ReadBoolean(this ReadOnlySpan<byte> span, bool needRead, ref bool @bool)
        {
            return needRead ? ReadBoolean(span, out @bool) : span;
        }
        public static ReadOnlySpan<byte> ReadByte(this ReadOnlySpan<byte> span, bool needRead, ref sbyte @sbyte)
        {
            return needRead ? ReadByte(span, out @sbyte) : span;
        }
        public static ReadOnlySpan<byte> ReadUnsignedByte(this ReadOnlySpan<byte> span, bool needRead, ref byte @byte)
        {
            return needRead ? ReadUnsignedByte(span, out @byte) : span;
        }
        public static ReadOnlySpan<byte> ReadShort(this ReadOnlySpan<byte> span, bool needRead, ref short @short)
        {
            return needRead ? ReadShort(span, out @short) : span;
        }
        public static ReadOnlySpan<byte> ReadUnsignedShort(this ReadOnlySpan<byte> span, bool needRead, ref ushort @ushort)
        {
            return needRead ? ReadUnsignedShort(span, out @ushort) : span;
        }
        public static ReadOnlySpan<byte> ReadInt(this ReadOnlySpan<byte> span, bool needRead, ref int @int)
        {
            return needRead ? ReadInt(span, out @int) : span;
        }
        public static ReadOnlySpan<byte> ReadLong(this ReadOnlySpan<byte> span, bool needRead, ref long @long)
        {
            return needRead ? ReadLong(span, out @long) : span;
        }
        public static ReadOnlySpan<byte> ReadFloat(this ReadOnlySpan<byte> span, bool needRead, ref float @float)
        {
            return needRead ? ReadFloat(span, out @float) : span;
        }
        public static ReadOnlySpan<byte> ReadDouble(this ReadOnlySpan<byte> span, bool needRead, ref double @double)
        {
            return needRead ? ReadDouble(span, out @double) : span;
        }
        public static ReadOnlySpan<byte> ReadString(this ReadOnlySpan<byte> span, bool needRead, ref string @string)
        {
            return needRead ? ReadString(span, out @string) : span;
        }
        public static ReadOnlySpan<byte> ReadVarShort(this ReadOnlySpan<byte> span, bool needRead, ref int varShort)
        {
            return needRead ? ReadVarShort(span, out varShort) : span;
        }
        public static ReadOnlySpan<byte> ReadVarInt(this ReadOnlySpan<byte> span, bool needRead, ref int varInt)
        {
            return needRead ? ReadVarInt(span, out varInt) : span;
        }
        public static ReadOnlySpan<byte> ReadVarLong(this ReadOnlySpan<byte> span, bool needRead, ref long varLong)
        {
            return needRead ? ReadVarLong(span, out varLong) : span;
        }

        public static ReadOnlySpan<byte> ReadIntArray(this ReadOnlySpan<byte> span, out int[] array)
        {
            return ReadIntArray(ReadVarInt(span, out int arrayLength), out array, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadVarIntArray(this ReadOnlySpan<byte> span, out int[] array)
        {
            return ReadVarIntArray(ReadVarInt(span, out int arrayLength), out array, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadStringArray(this ReadOnlySpan<byte> span, out string[] array)
        {
            return ReadStringArray(ReadVarInt(span, out int arrayLength), out array, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadByteArray(this ReadOnlySpan<byte> span, out byte[] array)
        {
            return ReadByteArray(ReadVarInt(span, out int arrayLength), out array, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadIntArray(this ReadOnlySpan<byte> span, out List<int> list)
        {
            return ReadIntArray(ReadVarInt(span, out int arrayLength), out list, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadVarIntArray(this ReadOnlySpan<byte> span, out List<int> list)
        {
            return ReadVarIntArray(ReadVarInt(span, out int arrayLength), out list, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadStringArray(this ReadOnlySpan<byte> span, out List<string> list)
        {
            return ReadStringArray(ReadVarInt(span, out int arrayLength), out list, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadByteArray(this ReadOnlySpan<byte> span, out List<byte> list)
        {
            return ReadByteArray(ReadVarInt(span, out int arrayLength), out list, arrayLength);
        }
        [Obsolete("Obsoleted in 14w21a")]
        public static ReadOnlySpan<byte> ReadLegacyByteArray(this ReadOnlySpan<byte> span, out List<byte> list)
        {
            return ReadByteArray(ReadShort(span, out short arrayLength), out list, arrayLength);
        }
        [Obsolete("Obsoleted in 14w21a")]
        public static ReadOnlySpan<byte> ReadLegacyByteArray(this ReadOnlySpan<byte> span, out byte[] array)
        {
            return ReadByteArray(ReadShort(span, out short arrayLength), out array, arrayLength);
        }
        public static ReadOnlySpan<byte> ReadIntArray(this ReadOnlySpan<byte> span, out List<int> list, int arrayLength)
        {
            list = new List<int>();
            for (int i = 0; i < arrayLength; i++)
            {
                span = span.ReadInt(out int value);
                list.Add(value);
            }
            return span;
        }
        public static ReadOnlySpan<byte> ReadVarIntArray(this ReadOnlySpan<byte> span, out List<int> list, int arrayLength)
        {
            list = new List<int>();
            for (int i = 0; i < arrayLength; i++)
            {
                span = span.ReadVarInt(out int value);
                list.Add(value);
            }
            return span;
        }
        public static ReadOnlySpan<byte> ReadStringArray(this ReadOnlySpan<byte> span, out List<string> list, int arrayLength)
        {
            list = new List<string>();
            for (int i = 0; i < arrayLength; i++)
            {
                span = span.ReadString(out string str);
                list.Add(str);
            }
            return span;
        }
        public static ReadOnlySpan<byte> ReadByteArray(this ReadOnlySpan<byte> span, out List<byte> list, int arrayLength)
        {
            list = new List<byte>(span.Slice(0, arrayLength).ToArray());
            return span.Slice(arrayLength);
        }

        public static ReadOnlySpan<byte> ReadIntArray(this ReadOnlySpan<byte> span, out int[] array, int arrayLength)
        {
            array = new int[arrayLength];
            for (int i = 0; i < array.Length; i++)
                span = span.ReadInt(out array[i]);
             
            return span;
        }
        public static ReadOnlySpan<byte> ReadVarIntArray(this ReadOnlySpan<byte> span, out int[] array, int arrayLength)
        {
            array = new int[arrayLength];
            for (int i = 0; i < array.Length; i++)
                span = span.ReadVarInt(out array[i]);
             
            return span;
        }
        public static ReadOnlySpan<byte> ReadStringArray(this ReadOnlySpan<byte> span, out string[] array, int arrayLength)
        {
            array = new string[arrayLength];
            for (int i = 0; i < array.Length; i++)
                span = span.ReadString(out array[i]);

            return span;
        }
        public static ReadOnlySpan<byte> ReadByteArray(this ReadOnlySpan<byte> span, out byte[] array, int arrayLength)
        {
            array = span.Slice(0, arrayLength).ToArray();
            return span.Slice(arrayLength);
        }


        public static byte AsBoolean(this ReadOnlySpan<byte> span) => span[0];
        public static byte AsBoolean(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(1);
            return span[0];
        }
        public static sbyte AsByte(this ReadOnlySpan<byte> span) => (sbyte)span[0];
        public static sbyte AsByte(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(1);
            return (sbyte)span[0];
        }
        public static byte AsUnsignedByte(this ReadOnlySpan<byte> span) => span[0];
        public static byte AsUnsignedByte(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(1);
            return span[0];
        }
        public static short AsShort(this ReadOnlySpan<byte> span) => (short)(span[0] << 8 | span[1]);
        public static short AsShort(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(2);
            return (short)(span[0] << 8 | span[1]);
        }
        public static ushort AsUnsignedShort(this ReadOnlySpan<byte> span) => (ushort)(span[0] << 8 | span[1]);
        public static ushort AsUnsignedShort(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(2);
            return (ushort)(span[0] << 8 | span[1]);
        }
        public static int AsInt(this ReadOnlySpan<byte> span) => span[0] << 24 | span[1] << 16 | span[2] << 08 | span[3];
        public static int AsInt(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(4);
            return span[0] << 24 | span[1] << 16 | span[2] << 08 | span[3];
        }
        public static long AsLong(this ReadOnlySpan<byte> span)
        {
            return ((long)span[0]) << 56 |
                   ((long)span[1]) << 48 |
                   ((long)span[2]) << 40 |
                   ((long)span[3]) << 32 |
                   ((long)span[4]) << 24 |
                   ((long)span[5]) << 16 |
                   ((long)span[6]) << 08 |
                   span[7];
        }
        public static long AsLong(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            outSpan = span.Slice(8);
            return ((long)span[0]) << 56 |
                   ((long)span[1]) << 48 |
                   ((long)span[2]) << 40 |
                   ((long)span[3]) << 32 |
                   ((long)span[4]) << 24 |
                   ((long)span[5]) << 16 |
                   ((long)span[6]) << 08 |
                   span[7];
        }
        public static float AsFloat(this ReadOnlySpan<byte> span) => AsFloat(span, out _);
        public static float AsFloat(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            Span<byte> buffer = span.Slice(0, 4).ToArray();
            buffer.Reverse();
            outSpan = span.Slice(4);
            return BitConverter.ToSingle(buffer);
        }
        public static double AsDouble(this ReadOnlySpan<byte> span) => AsDouble(span, out _);
        public static double AsDouble(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            Span<byte> buffer = span.Slice(0, 8).ToArray();
            buffer.Reverse();
            outSpan = span.Slice(4);
            return BitConverter.ToDouble(buffer);
        }
        public static string AsString(this ReadOnlySpan<byte> span) => AsString(span, out _);
        public static string AsString(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int length = span.AsVarInt(out ReadOnlySpan<byte> buffer);
            outSpan = buffer.Slice(length);
            return Encoding.UTF8.GetString(buffer.Slice(0, length));
        }
        public static int AsVarShort(this ReadOnlySpan<byte> span) => VarShort.Read(span);
        public static int AsVarShort(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int result = VarShort.Read(span, out int offset);
            outSpan = span.Slice(offset);
            return result;
        }
        public static int AsVarInt(this ReadOnlySpan<byte> span) => VarInt.Read(span);
        public static int AsVarInt(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int result = VarInt.Read(span, out int offset);
            outSpan = span.Slice(offset);
            return result;
        }
        public static long AsVarLong(this ReadOnlySpan<byte> span) => VarLong.Read(span);
        public static long AsVarLong(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            long result = VarLong.Read(span, out int offset);
            outSpan = span.Slice(offset);
            return result;
        }

        public static int[] AsIntArray(this ReadOnlySpan<byte> span) => AsIntArray(span, out _);
        public static int[] AsIntArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int length = VarInt.Read(span, out int offset);
            return AsIntArray(span.Slice(offset, length), out outSpan, length);
        }
        public static int[] AsVarIntArray(this ReadOnlySpan<byte> span) => AsVarIntArray(span, out _);
        public static int[] AsVarIntArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int length = VarInt.Read(span, out int offset);
            return AsVarIntArray(span.Slice(offset, length), out outSpan, length);
        }
        public static string[] AsStringArray(this ReadOnlySpan<byte> span) => AsStringArray(span, out _);
        public static string[] AsStringArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int length = VarInt.Read(span, out int offset);
            return AsStringArray(span.Slice(offset, length), out outSpan, length);
        }
        public static byte[] AsByteArray(this ReadOnlySpan<byte> span) => AsByteArray(span, out _);
        public static byte[] AsByteArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            int length = VarInt.Read(span, out int offset);
            return AsByteArray(span.Slice(offset, length), out outSpan, length);
        }

        [Obsolete("Obsoleted in 14w21a")]
        public static byte[] AsLegacyByteArray(this ReadOnlySpan<byte> span) => span.Slice(2, span[3] | span[2] << 8).ToArray();
        [Obsolete("Obsoleted in 14w21a")]
        public static byte[] AsLegacyByteArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan)
        {
            short length = span.AsShort();
            return AsByteArray(span.Slice(2, length), out outSpan, length);
        }

        public static int[] AsIntArray(this ReadOnlySpan<byte> span, int arrayLength) => AsIntArray(span, out _, arrayLength);
        public static int[] AsIntArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan, int arrayLength)
        {
            int[] array = new int[arrayLength];
            for (int i = 0; i < array.Length; i++)
                array[i] = span.Slice(i * 4, 4).AsInt();
            outSpan = span.Slice(arrayLength * 4);
            return array;
        }
        public static int[] AsVarIntArray(this ReadOnlySpan<byte> span, int arrayLength) => AsVarIntArray(span, out _, arrayLength);
        public static int[] AsVarIntArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan, int arrayLength)
        {
            int[] array = new int[arrayLength];
            int offset = 0;
            for (int i = 0; i < array.Length; i++)
                array[i] = VarInt.Read(span.Slice(offset), out offset);
            outSpan = span.Slice(offset);
            return array;
        }
        public static string[] AsStringArray(this ReadOnlySpan<byte> span, int arrayLength) => AsStringArray(span, out _, arrayLength);
        public static string[] AsStringArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan, int arrayLength)
        {
            string[] array = new string[arrayLength];
            for (int i = 0; i < array.Length; i++)
                array[i] = span.AsString(out span);
            outSpan = span;
            return array;
        }
        public static byte[] AsByteArray(this ReadOnlySpan<byte> span, int arrayLength) => span.Slice(0, arrayLength).ToArray();
        public static byte[] AsByteArray(this ReadOnlySpan<byte> span, out ReadOnlySpan<byte> outSpan, int arrayLength)
        {
            outSpan = span.Slice(arrayLength);
            return span.Slice(0, arrayLength).ToArray(); 
        }

    }
}
