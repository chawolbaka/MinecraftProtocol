using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.IO.Pools;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
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
                    return _data[index];
            }
            set
            {
                ThrowIfDisposed();
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                _version++;
                _data[index] = value;
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
                GCHandle newDataGCHandle = _dataPool.Rent(newSize);
                byte[] newData = (byte[])newDataGCHandle.Target;
                if (_data != null)
                {
                    Array.Copy(_data, newData, _size);
                    if (_returnToPool)
                        _dataPool.Return(_dataGCHandle);
                }
                _returnToPool = true;
                _dataGCHandle = newDataGCHandle;
                _data = newData;
            }
        }
        public virtual Span<byte> AsSpan() { ThrowIfDisposed(); return _data.AsSpan(0, _size); }

        //mc大部分都是小包所以使用这种形状的线程池可能更适合？
        internal static UnsafeSawtoothArrayPool<byte> _dataPool = new UnsafeSawtoothArrayPool<byte>(true,4096, 2048, 1024, 256, 256, 256, 256, 256, 256, 256, 128, 128, 128, 128, 128, 64, 64, 64, 64, 64, 64, 16);
        protected const int DEFUALT_CAPACITY = 16;
        protected bool _returnToPool;

        internal protected GCHandle _dataGCHandle;
        internal protected byte[] _data;
        internal protected int _size = 0;
        protected int _version;

        public ByteWriter() : this(DEFUALT_CAPACITY) { }
        public ByteWriter(int capacity)
        {
            RerentData(capacity > 0 ? capacity : DEFUALT_CAPACITY);
        }
        public ByteWriter(ref byte[] data)
        {
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
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedByte(byte value)
        {
            _version++;
            TryGrow(sizeof(byte));
            _data[_size++] = value;
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
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteUnsignedShort(ushort value)
        {
            _version++;
            TryGrow(2);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteInt(int value)
        {
            _version++;
            TryGrow(4);
            _data[_size++] = (byte)(value >> 24);
            _data[_size++] = (byte)(value >> 16);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual ByteWriter WriteLong(long value)
        {
            _version++;
            TryGrow(8);
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
            TryGrow(sizeof(float));
            BitConverter.GetBytes(value).CopyTo(_data, _size);
            _data.AsSpan().Slice(_size, sizeof(float)).Reverse();
            _size += sizeof(float);
            return this;
        }
        public virtual ByteWriter WriteDouble(double value)
        {
            _version++;
            TryGrow(sizeof(double));
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
            TryGrow(sizeof(long) * 2);
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
            TryGrow(length);

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
            TryGrow(value.Length);
            Array.Copy(value, 0, _data, _size, value.Length);
            _size += value.Length;
            return this;
        }

        public virtual ByteWriter WriteBytes(ReadOnlySpan<byte> value)
        {
            _version++;
            TryGrow(value.Length);
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

        /// <summary>
        /// 用于对象池的清空需求
        /// </summary>
        internal virtual void ClearToNullable()
        {
            if (_size > 0)
            {
                if (_dataGCHandle != default)
                    _dataPool.Return(_dataGCHandle);
                _version = 0;
                _size = 0;
                _data = null;
            }
        }

        public virtual void Clear()
        {
            if (_size > 0)
            {
                _size = 0;
                RerentData(DEFUALT_CAPACITY);
            }
        }

        protected virtual void TryGrow(int writeLength)
        {
            ThrowIfDisposed();
            if (writeLength + _size > Capacity)
            {
                Capacity += (int)(writeLength * 1.5);
            }
        }

        protected virtual void RerentData(int size)
        {
            GCHandle old = _dataGCHandle;
            _dataGCHandle = _dataPool.Rent(size);
            _data = (byte[])_dataGCHandle.Target;
            _returnToPool = true;
            if (old != default)
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
                yield return _data[i];
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
                yield return _data[i];
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
            bool disposed = _disposed;
            _disposed = true;
            if (!disposed && _returnToPool && _data is not null)
            {
                _dataPool.Return(_dataGCHandle);
                _data = null;
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
