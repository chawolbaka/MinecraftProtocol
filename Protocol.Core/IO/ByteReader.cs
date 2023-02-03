using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.NBT;
using MinecraftProtocol.NBT.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            ReadOnlySpan<byte> span = _data.Span;
            return span[offset++] << 24 |
                   span[offset++] << 16 |
                   span[offset++] << 08 |
                   span[offset++];

        }

        public virtual uint ReadUnsignedInt()
        {
            ReadOnlySpan<byte> span = _data.Span;
            return ((uint)span[offset++]) << 24 |
                   ((uint)span[offset++]) << 16 |
                   ((uint)span[offset++]) << 08 |
                   span[offset++];

        }

        public virtual long ReadLong()
        {
            ReadOnlySpan<byte> span = _data.Span;
            return ((long)span[offset++]) << 56 |
                  ((long)span[offset++]) << 48 |
                  ((long)span[offset++]) << 40 |
                  ((long)span[offset++]) << 32 |
                  ((long)span[offset++]) << 24 |
                  ((long)span[offset++]) << 16 |
                  ((long)span[offset++]) << 08 |
                  span[offset++];
        }

        public virtual ulong ReadUnsignedLong()
        {
            ReadOnlySpan<byte> span = _data.Span;
            return ((ulong)span[offset++]) << 56 |
                   ((ulong)span[offset++]) << 48 |
                   ((ulong)span[offset++]) << 40 |
                   ((ulong)span[offset++]) << 32 |
                   ((ulong)span[offset++]) << 24 |
                   ((ulong)span[offset++]) << 16 |
                   ((ulong)span[offset++]) << 08 |
                   span[offset++];
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
        
        public virtual Position ReadPosition(int protocolVersion)
        {
            return new Position(ReadUnsignedLong(), protocolVersion);
        }
         
        public virtual Identifier ReadIdentifier()
        {
            return Identifier.Parse(ReadString());
        }

        public virtual byte[] ReadBytes(int length)
        {
            byte[] result = _data.Span.Slice(offset, length).ToArray();
            offset += length;
            return result;
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

        public virtual Identifier[] ReadIdentifierArray()
        {
            Identifier[] list = new Identifier[ReadVarInt()];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = Identifier.Parse(ReadString());
            }
            return list;
        }

        public virtual CompoundTag ReadNBT()
        {
            NBTReader reader = new NBTReader(this);
            NBTTagType type = reader.ReadType();
            if (type == NBTTagType.Compound)
                return new CompoundTag().Read(reader) as CompoundTag;
            else if (type == NBTTagType.End)
                return new CompoundTag();
            else
                throw new InvalidDataException("Failed to read nbt");
        }

        public virtual T ReadOptionalField<T>(Func<T> func) where T : class
        {
            return ReadBoolean() ? func() : null;
        }

        public virtual byte[] ReadOptionalBytes(int length)
        {
            return ReadBoolean() ? ReadBytes(length) : null;
        }
        public virtual byte[] ReadOptionalByteArray(int protocolVersion)
        {
            return ReadBoolean() ? ReadByteArray(protocolVersion) : null;
        }

        public virtual byte[] ReadAll()
        {
            SetToEnd();
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


        public virtual void SetToEnd()
        {
            offset = _data.Length;
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
