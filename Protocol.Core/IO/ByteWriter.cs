using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.IO.NBT;
using MinecraftProtocol.IO.NBT.Tags;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MinecraftProtocol.IO
{
    public class ByteWriter : IEnumerable<byte>, IDisposable
    {
        public virtual int Count => ThrowIfDisposed(_size);

        public virtual byte this[int index]
        {
            get
            {
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                else
                    return _data[_start + index];
            }
            set
            {
                ThrowIfDisposed();
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                _version++;
                _data[_start + index] = value;
            }
        }

        public virtual int Capacity
        {
            get => ThrowIfDisposed(_data is not null ? _data.Length : 0);
            set
            {
                ThrowIfDisposed();
                int newSize = value;
                if (newSize < _size)
                    throw new ArgumentOutOfRangeException(nameof(Capacity), "Capacity was less than the current size.");
                if (newSize == _size)
                    return;
                if (newSize > Array.MaxLength)
                    newSize = Array.MaxLength;

                _version++;
                byte[] newData = _dataPool.Rent(newSize);
                if (_data != null)
                {
                    Array.Copy(_data, newData, _start + _size);
                    if (_returnToPool)
                        _dataPool.Return(_data);
                }
                _returnToPool = true;
                _data = newData;
            }
        }
        public virtual Span<byte> AsSpan() { ThrowIfDisposed(); return _data.AsSpan(_start, _size); }
        public virtual Memory<byte> AsMemory() { ThrowIfDisposed(); return _data.AsMemory(_start, _size); }

        //mc大部分都是小包所以使用这种形状的数组池可能更适合？
        internal static SawtoothArrayPool<byte> _dataPool = new SawtoothArrayPool<byte>(4096, 2048, 1024, 256, 256, 256, 256, 256, 256, 256, 128, 128, 128, 128, 128, 64, 64, 64, 64, 64, 64, 16);
        protected const int DEFUALT_CAPACITY = 16;
        internal protected bool _returnToPool;

        internal bool _needDisable = true; //提供给对象池使用的选项
        internal protected byte[] _data;
        internal protected int _start = 0;
        internal protected int _size = 0;
        protected int _version;

        public ByteWriter() : this(DEFUALT_CAPACITY) { }
        public ByteWriter(int capacity)
        {
            RerentData(capacity > 0 ? capacity : DEFUALT_CAPACITY);
        }

        //这边的ref是因为我比较病态，想减少几次值类型的复制，但仅限内部使用，外部容易引起误会
        internal ByteWriter(ref int size, ref byte[] data)
        {
            _size = size;
            _data = data;
            _returnToPool = false;
        }


        /// <summary>
        /// 初始化一个ByteWriter
        /// </summary>
        /// <param name="size">data的有效范围</param>
        /// <param name="data">该参数会直接赋值给ByteWriter内部的data，不产生复制。</param>
        public ByteWriter(int size, ref byte[] data)
        {
            _size = size;
            _data = data;
            _returnToPool = false;
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
            TryGrow(sizeof(sbyte));
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedByte(byte value)
        {
            _version++;
            TryGrow(sizeof(byte));
            _data[_start+_size++] = value;
            return this;
        }
        public virtual ByteWriter WriteString(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            TryGrow(VarInt.GetLength(str.Length) + str.Length);
            WriteVarInt(str.Length);
            WriteBytes(str);
            return this;
        }
        public virtual ByteWriter WriteShort(short value)
        {
            _version++;
            TryGrow(2);
            _data[_start+_size++] = (byte)(value >> 8);
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedShort(ushort value)
        {
            _version++;
            TryGrow(2);
            _data[_start+_size++] = (byte)(value >> 8);
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteInt(int value)
        {
            _version++;
            TryGrow(4);
            _data[_start+_size++] = (byte)(value >> 24);
            _data[_start+_size++] = (byte)(value >> 16);
            _data[_start+_size++] = (byte)(value >> 8);
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedInt(uint value)
        {
            _version++;
            TryGrow(4);
            _data[_start+_size++] = (byte)(value >> 24);
            _data[_start+_size++] = (byte)(value >> 16);
            _data[_start+_size++] = (byte)(value >> 8);
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteLong(long value)
        {
            _version++;
            TryGrow(8);
            _data[_start+_size++] = (byte)(value >> 54);
            _data[_start+_size++] = (byte)(value >> 48);
            _data[_start+_size++] = (byte)(value >> 40);
            _data[_start+_size++] = (byte)(value >> 32);
            _data[_start+_size++] = (byte)(value >> 24);
            _data[_start+_size++] = (byte)(value >> 16);
            _data[_start+_size++] = (byte)(value >> 8);
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedLong(ulong value)
        {
            _version++;
            TryGrow(8);
            _data[_start+_size++] = (byte)(value >> 54);
            _data[_start+_size++] = (byte)(value >> 48);
            _data[_start+_size++] = (byte)(value >> 40);
            _data[_start+_size++] = (byte)(value >> 32);
            _data[_start+_size++] = (byte)(value >> 24);
            _data[_start+_size++] = (byte)(value >> 16);
            _data[_start+_size++] = (byte)(value >> 8);
            _data[_start+_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteFloat(float value)
        {
            _version++;
            TryGrow(sizeof(float));
            BitConverter.GetBytes(value).CopyTo(_data, _start + _size);
            _data.AsSpan(_start + _size, sizeof(float)).Reverse();
            _size += sizeof(float);
            return this;
        }
        public virtual ByteWriter WriteDouble(double value)
        {
            _version++;
            TryGrow(sizeof(double));
            BitConverter.GetBytes(value).CopyTo(_data, _start + _size);
            _data.AsSpan(_start + _size, sizeof(double)).Reverse();
            _size += sizeof(double);
            return this;
        }
        public virtual ByteWriter WriteVarShort(int value)
        {
            WriteBytes(VarShort.GetSpan(value));
            return this;
        }
        public virtual ByteWriter WriteVarInt(int value)
        {
            WriteBytes(VarInt.GetSpan(value));
            return this;
        }
        public virtual ByteWriter WriteVarLong(long value)
        {
            WriteBytes(VarLong.GetSpan(value));
            return this;
        }
        public virtual ByteWriter WriteIdentifier(Identifier identifier)
        {
            WriteString(identifier.ToString());
            return this;
        }
        public virtual ByteWriter WriteNBT(NBTTag tag)
        {
            tag.Write(new NBTWriter(this));
            return this;
        }
        public virtual ByteWriter WriteUUID(UUID value)
        {
            TryGrow(sizeof(long) * 2);
            WriteLong(value.Most);
            WriteLong(value.Least);
            return this;
        }
        public virtual ByteWriter WritePosition(Position value, int protocolVersion)
        {
            WriteUnsignedLong(value.Encode(protocolVersion));
            return this;
        }

        public virtual ByteWriter WriteBytes(params ICollection<byte>[] collections)
        {
            if (collections == null || collections.Length == 0)
                return this;

            _version++;
            int length = 0;
            foreach (var conllection in collections)
                length += conllection.Count;
            TryGrow(length);

            foreach (var collection in collections)
            {
                collection.CopyTo(_data, _start + _size);
                _size += collection.Count;
            }
            return this;
        }
        public virtual ByteWriter WriteBytes(params byte[] value)
        {
            if (value == null || value.Length == 0)
                return this;
            _version++;
            TryGrow(value.Length);
            Array.Copy(value, 0, _data, _start + _size, value.Length);
            _size += value.Length;
            return this;
        }

        public virtual ByteWriter WriteBytes(ReadOnlySpan<byte> value)
        {
            if (value == null || value.Length == 0)
                return this;
            _version++;
            TryGrow(value.Length);
            value.CopyTo(AsSpan());
            _size += value.Length;
            return this;
        }
        public virtual ByteWriter WriteByteArray(ReadOnlySpan<byte> array, int protocolVersion)
        {
            int length = array == null ? 0 : array.Length;
            if (protocolVersion >= ProtocolVersions.V14w21a)
                WriteVarInt(length);
            else
                WriteShort((short)length);

            WriteBytes(array);
            return this;
        }
        public virtual ByteWriter WriteStringArray(IEnumerable<string> array)
        {
            return WriteArray(WriteString, array, array.Count()); ;
        }
        public virtual ByteWriter WriteStringArray(ReadOnlySpan<string> array)
        {
            return WriteArray(WriteString, array); ;
        }
        public virtual ByteWriter WriteIdentifierArray(IEnumerable<Identifier> array)
        {
            return WriteArray(WriteIdentifier, array, array.Count());
        }
        public virtual ByteWriter WriteIdentifierArray(ReadOnlySpan<Identifier> array)
        {
            return WriteArray(WriteIdentifier, array);
        }

        private ByteWriter WriteArray<T>(Func<T, ByteWriter> write, ReadOnlySpan<T> array)
        {
            if(array != null)
            {
                WriteVarInt(array.Length);
                foreach (T item in array)
                    write(item);
                return this;
            }
            else
            {
                WriteVarInt(0);
                return this;
            }
        }
        private ByteWriter WriteArray<T>(Func<T, ByteWriter> write, IEnumerable<T> array, int count)
        {
            WriteVarInt(count);
            if (array != null)
            {
                foreach (T item in array)
                {
                    write(item);
                }
            }
            return this;
        }

        public virtual ByteWriter WriteOptionalField<T>(Func<T, ByteWriter> func, T value) where T : class
        {
            bool isNull = value == null;
            WriteBoolean(!isNull);
            if (!isNull)
                return func(value);
            else
                return this;
        }
        public virtual ByteWriter WriteOptionalString(string value)
        {
            bool isNull = string.IsNullOrEmpty(value);
            WriteBoolean(!isNull);
            if (!isNull)
                return WriteString(value);
            else
                return this;
        }
        public virtual ByteWriter WriteOptionalBytes(ReadOnlySpan<byte> array)
        {
            bool isNull = array == null || array.Length == 0;
            WriteBoolean(!isNull);
            if (!isNull)
                return WriteBytes(array);
            else
                return this;
        }
        public virtual ByteWriter WriteOptionalByteArray(ReadOnlySpan<byte> array, int protocolVersion)
        {
            bool isNull = array == null || array.Length == 0;
            WriteBoolean(!isNull);
            if (!isNull)
                return WriteByteArray(array, protocolVersion);
            else
                return this;
        }



        /// <summary>
        /// 用于对象池的清空需求
        /// </summary>
        internal virtual void ClearToNullable()
        {
            if (_size > 0)
            {
                if (_data != null && _returnToPool)
                    _dataPool.Return(_data);
                _version = 0;
                _start = 0;
                _size = 0;
                _data = null;
            }
        }

        public virtual void Clear()
        {
            if (_size > 0)
            {
                _start = 0;
                _size = 0;
                RerentData(DEFUALT_CAPACITY);
            }
        }

        protected virtual void TryGrow(int writeLength)
        {
            ThrowIfDisposed();
            if (writeLength + _start + _size > Capacity)
            {
                Capacity += (int)(writeLength * 1.5);
            }
        }

        protected virtual void RerentData(int size)
        {
            bool returnToPool = _returnToPool;
            byte[] old = _data;
            _data = _dataPool.Rent(_start + size);
            _returnToPool = true;
            if (returnToPool)
                _dataPool.Return(old);
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            ThrowIfDisposed();
            if (_size <= 0)
                yield break;

            int version = _version;
            for (int i = 0; i < _size; i++)
            {
                if (_version != version)
                    throw new InvalidOperationException("data was modified, enumeration operation may not execute");
                yield return _data[_start + i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfDisposed();
            if (_size <= 0)
                yield break;

            int version = _version;
            for (int i = 0; i < _size; i++)
            {
                if (_version != version)
                    throw new InvalidOperationException("data was modified, enumeration operation may not execute");
                yield return _data[_start + i];
            }
        }

        internal bool _disposed = false;
        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_needDisable)
                return;
            bool disposed = _disposed;
            _disposed = true;
            if (!disposed && _returnToPool && _data is not null)
            {
                try
                {
                    _dataPool.Return(_data);
                }
                catch (ArgumentException) { }
                finally
                {
                    _data = null;
                }
            }
        }

        ~ByteWriter()
        {
            Dispose(false);
        }

        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        protected virtual T ThrowIfDisposed<T>(T value)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            return value;
        }
    }
}
