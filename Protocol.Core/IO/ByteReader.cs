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
        public virtual int Count => _data.Length;

        protected ReadOnlyMemory<byte> _data;

        protected int offset;

        public ByteReader(ReadOnlyMemory<byte> data)
        {
            _data = data;
        }

        public virtual byte this[int index] => _data.Span[index];

        public virtual bool IsReadToEnd => offset >= _data.Length;
        public virtual int Position
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

        public virtual bool ReadBoolean() => _data.Span[offset++] == 0x01;

        public virtual sbyte ReadByte() => (sbyte)_data.Span[offset++];

        public virtual byte ReadUnsignedByte() => _data.Span[offset++];

        public virtual short ReadShort() => (short)(_data.Span[offset++] << 8 | _data.Span[offset++]);

        public virtual ushort ReadUnsignedShort() => (ushort)(_data.Span[offset++] << 8 | _data.Span[offset++]);

        public virtual int ReadInt()
        {
            return _data.Span[offset++] << 24 |
                   _data.Span[offset++] << 16 |
                   _data.Span[offset++] << 08 |
                   _data.Span[offset++];

        }

        public virtual long ReadLong()
        {
           return ((long)_data.Span[offset++]) << 56 |
                  ((long)_data.Span[offset++]) << 48 |
                  ((long)_data.Span[offset++]) << 40 |
                  ((long)_data.Span[offset++]) << 32 |
                  ((long)_data.Span[offset++]) << 24 |
                  ((long)_data.Span[offset++]) << 16 |
                  ((long)_data.Span[offset++]) << 08 |
                  _data.Span[offset++];
        }

        public virtual float ReadFloat()
        {
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _data.Span[offset + 3 - i];
            offset += size;
            return BitConverter.ToSingle(buffer);
        }

        public virtual double ReadDouble()
        {
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _data.Span[offset + 3 - i];
            offset += size;
            return BitConverter.ToDouble(buffer);
        }

        public virtual string ReadString()
        {
            int length = ReadVarInt();
            var x = _data.Span.Slice(offset, length);
            string result = Encoding.UTF8.GetString(x);
            offset += length;
            return result;
        }

        public virtual int ReadVarShort()
        {
            int result = VarShort.Read(_data.Span.Slice(offset), out int length);
            offset += length;
            return result;
        }

        public virtual int ReadVarInt()
        {
            int result = VarInt.Read(_data.Span.Slice(offset), out int length);
            offset += length;
            return result;
        }

        public virtual long ReadVarLong()
        {
            long result = VarLong.Read(_data.Span.Slice(offset), out int length);
            offset += length;
            return result;
        }

        public virtual UUID ReadUUID()
        {
            return new UUID(ReadLong(), ReadLong());
        }

        public virtual byte[] ReadByteArray(int protocolVersion)
        {
            int ArrayLength = protocolVersion >= ProtocolVersions.V14w21a ? ReadVarInt() : ReadShort();
            byte[] result = _data.Span.Slice(offset, ArrayLength).ToArray();
            offset += ArrayLength;
            return result;
        }


        public virtual string[] ReadStringArray()
        {
            string[] list = new string[ReadVarInt()];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = ReadString();
            }
            return list;
        }


        public virtual byte[] ReadAll()
        {
            offset = _data.Length;
            return _data.Span.ToArray();
        }


        public virtual ReadOnlySpan<byte> AsSpan()
        {
            return _data.Span;
        }

        public virtual ReadOnlyMemory<byte> AsMemory()
        {
            return _data;
        }

        public virtual void Reset()
        {
            offset = 0;
        }

        public virtual IEnumerator<byte> GetEnumerator()
        {
            for (int i = 0; i < _data.Span.Length; i++)
            {
                yield return _data.Span[i]; 
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _data.Span.Length; i++)
            {
                yield return _data.Span[i];
            }
        }
    }
}
