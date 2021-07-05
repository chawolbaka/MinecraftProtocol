using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.IO
{
    public class ByteReader: IEnumerable<byte>
    {
        protected byte[] _data;

        protected int offset;

        public ByteReader(byte[] data,bool clone = false)
        {
            _data = clone ? (byte[])data.Clone() : data;
        }
        public bool IsReadToEnd => offset >= _data.Length;
        public int Position
        {
            get => offset;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Position), "负数");
                if (value > _data.Length)
                    throw new ArgumentOutOfRangeException(nameof(Position), "超出数组边界");
                offset = value;
            }
        }

        public bool ReadBoolean() => _data[offset++] == 0x01;

        public sbyte ReadByte() => (sbyte)_data[offset++];

        public byte ReadUnsignedByte() => _data[offset++];

        public short ReadShort() => (short)(_data[offset++] << 8 | _data[offset++]);

        public ushort ReadUnsignedShort() => (ushort)(_data[offset++] << 8 | _data[offset++]);

        public int ReadInt()
        {
            return _data[offset++] << 24 |
                   _data[offset++] << 16 |
                   _data[offset++] << 08 |
                   _data[offset++];

        }

        public long ReadLong()
        {
           return ((long)_data[offset++]) << 56 |
                  ((long)_data[offset++]) << 48 |
                  ((long)_data[offset++]) << 40 |
                  ((long)_data[offset++]) << 32 |
                  ((long)_data[offset++]) << 24 |
                  ((long)_data[offset++]) << 16 |
                  ((long)_data[offset++]) << 08 |
                  _data[offset++];
        }

        public float ReadFloat()
        {
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _data[offset + 3 - i];
            offset += size;
            return BitConverter.ToSingle(buffer);
        }

        public double ReadDouble()
        {
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _data[offset + 3 - i];
            offset += size;
            return BitConverter.ToDouble(buffer);
        }

        public string ReadString()
        {
            int length = ReadVarInt();
            var x = _data.AsSpan().Slice(offset, length);
            string result = Encoding.UTF8.GetString(x);
            offset += length;
            return result;
        }

        public int ReadVarShort()
        {
            int result = VarShort.Read(_data.AsSpan().Slice(offset), out int length);
            offset += length;
            return result;
        }

        public int ReadVarInt()
        {
            int result = VarInt.Read(_data.AsSpan().Slice(offset), out int length);
            offset += length;
            return result;
        }

        public long ReadVarLong()
        {
            long result = VarLong.Read(_data.AsSpan().Slice(offset), out int length);
            offset += length;
            return result;
        }

        public UUID ReadUUID()
        {
            return new UUID(ReadLong(), ReadLong());
        }

        public byte[] ReadByteArray(int protocolVersion)
        {
            int ArrayLength = protocolVersion >= ProtocolVersionNumbers.V14w21a ? ReadVarInt() : ReadShort();
            byte[] result = _data.AsSpan().Slice(offset, ArrayLength).ToArray();
            offset += ArrayLength;
            return result;
        }

        public byte[] ReadAll()
        {
            offset = _data.Length;
            return (byte[])_data.Clone();
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)_data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}
