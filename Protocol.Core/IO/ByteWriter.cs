﻿using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.IO
{
    public class ByteWriter: IEnumerable<byte>
    {
        public virtual int Count => _size;

        public virtual byte this[int index]
        {
            get
            {
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                else
                    return _data[index];
            }
            set
            {
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                _version++;
                _data[index] = value;
            }
        }

        public virtual int Capacity
        {
            get => _data.Length;
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(Capacity), "Capacity was less than the current size.");
                if (value == _size)
                    return;

                _version++;
                byte[] temp = new byte[value];
                Array.Copy(_data, temp, _size);
                _data = temp;
            }
        }
        public virtual Span<byte> AsSpan() => _data.AsSpan(0, _size);
       
        protected const int DEFUALT_CAPACITY = 16;

        protected byte[] _data;
        protected int _size = 0;
        protected int _version;

        public ByteWriter() : this(DEFUALT_CAPACITY) { }
        public ByteWriter(int capacity)
        {
            _data = new byte[capacity];
        }



        public virtual ByteWriter WriteBoolean(bool boolean)
        {
            _version++;
            if (boolean)
                WriteUnsignedByte(0x01);
            else
                WriteUnsignedByte(0x00);
            return this;
        }
        public virtual ByteWriter WriteByte(sbyte value)
        {
            _version++;
            TryAllocateCapacity(sizeof(sbyte));
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedByte(byte value)
        {
            _version++;
            TryAllocateCapacity(sizeof(byte));
            _data[_size++] = value;
            return this;
        }
        public virtual ByteWriter WriteString(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            TryAllocateCapacity(VarInt.GetLength(str.Length) + str.Length);
            WriteVarInt(str.Length);
            WriteBytes(str);
            return this;
        }
        public virtual ByteWriter WriteShort(short value)
        {
            _version++;
            TryAllocateCapacity(2);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedShort(ushort value)
        {
            _version++;
            TryAllocateCapacity(2);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteInt(int value)
        {
            _version++;
            TryAllocateCapacity(4);
            _data[_size++] = (byte)(value >> 24);
            _data[_size++] = (byte)(value >> 16);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteLong(long value)
        {
            _version++;
            TryAllocateCapacity(8);
            _data[_size++] = (byte)(value >> 54);
            _data[_size++] = (byte)(value >> 48);
            _data[_size++] = (byte)(value >> 40);
            _data[_size++] = (byte)(value >> 32);
            _data[_size++] = (byte)(value >> 24);
            _data[_size++] = (byte)(value >> 16);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteFloat(float value)
        {
            _version++;
            TryAllocateCapacity(sizeof(float));
            BitConverter.GetBytes(value).CopyTo(_data, _size);
            _data.AsSpan().Slice(_size, sizeof(float)).Reverse();
            _size += sizeof(float);
            return this;
        }
        public virtual ByteWriter WriteDouble(double value)
        {
            _version++;
            TryAllocateCapacity(sizeof(double));
            BitConverter.GetBytes(value).CopyTo(_data, _size);
            _data.AsSpan().Slice(_size, sizeof(double)).Reverse();
            _size += sizeof(double);
            return this;
        }
        public virtual ByteWriter WriteVarShort(int value)
        {
            WriteBytes(VarShort.GetBytes(value));
            return this;
        }
        public virtual ByteWriter WriteVarInt(int value)
        {
            WriteBytes(VarInt.GetBytes(value));
            return this;
        }
        public virtual ByteWriter WriteVarLong(long value)
        {
            WriteBytes(VarLong.GetBytes(value));
            return this;
        }
        public virtual ByteWriter WriteUUID(UUID value)
        {
            TryAllocateCapacity(sizeof(long) * 2);
            WriteLong(value.Most);
            WriteLong(value.Least);
            return this;
        }
        public virtual ByteWriter WriteBytes(params ICollection<byte>[] collections)
        {
            _version++;
            int length = 0;
            foreach (var conllection in collections)
                length += conllection.Count;
            TryAllocateCapacity(length);

            foreach (var collection in collections)
            {
                collection.CopyTo(_data, _size);
                _size += collection.Count;
            }
            return this;
        }
        public virtual ByteWriter WriteBytes(params byte[] value)
        {
            _version++;
            TryAllocateCapacity(value.Length);
            Array.Copy(value, 0, _data, _size, value.Length);
            _size += value.Length;
            return this;
        }

        public virtual ByteWriter WriteBytes(ReadOnlySpan<byte> value)
        {
            _version++;
            TryAllocateCapacity(value.Length);
            value.CopyTo(_data.AsSpan(_size));
            _size += value.Length;
            return this;
        }
        public virtual ByteWriter WriteByteArray(ReadOnlySpan<byte> array, int protocolVersion)
        {
            //14w21a: All byte arrays have VarInt length prefixes instead of short
            if (protocolVersion >= ProtocolVersions.V14w21a)
                WriteVarInt(array.Length);
            else
                WriteShort((short)array.Length);
            WriteBytes(array);
            return this;
        }
        public virtual ByteWriter WriteStringArray(IList<string> array)
        {
            _version++;
            WriteVarInt(array.Count);
            foreach (var str in array)
                WriteString(str);
            return this;
        }
        public virtual ByteWriter WriteStringArray(ReadOnlySpan<string> array)
        {
            _version++;
            WriteVarInt(array.Length);
            foreach (var str in array)
                WriteString(str);
            return this;
        }

        private bool ExtraAllocate = IntPtr.Size > 4;
        protected virtual void TryAllocateCapacity(int writeLength)
        {
            if (writeLength + _size > Capacity)
                Capacity += ExtraAllocate && writeLength < 128 ? writeLength * 2 : writeLength;
        }


        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            if (_size <= 0)
                yield break;

            int version = _version;
            for (int i = 0; i < _size; i++)
            {
                if (_version != version)
                    throw new InvalidOperationException("data was modified, enumeration operation may not execute");
                yield return _data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_size <= 0)
                yield break;

            int version = _version;
            for (int i = 0; i < _size; i++)
            {
                if (_version != version)
                    throw new InvalidOperationException("data was modified, enumeration operation may not execute");
                yield return _data[i];
            }
        }
    }
}