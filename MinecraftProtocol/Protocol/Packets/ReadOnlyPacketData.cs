using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Buffers;
#if UNSAFE
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace MinecraftProtocol.Protocol.Packets
{
    /// <summary>
    /// ReadOnlyCollection和ReadOnlyMemory的缝和类
    /// </summary>
    public class ReadOnlyPacketData<T>: ReadOnlyCollection<T>
    {
        private static readonly FieldInfo ItemsField = typeof(List<T>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
#if UNSAFE
        //By: https://stackoverflow.com/questions/30817924/obtain-non-explicit-field-offset
        private static int ItemsFieldOffset => IntPtr.Size + (Marshal.ReadInt32(ItemsField.FieldHandle.Value + (4 + IntPtr.Size)) & 0xFFFFFF);
        private static MemoryHandle? _bufferHandle;
#endif
        private List<T> _list;

#if UNSAFE
        private unsafe ReadOnlyMemory<T> Buffer
#else
        private ReadOnlyMemory<T> Buffer
#endif
        {
            get
            {
                if (!_buffer.HasValue || _buffer.Value.Length != _list.Capacity)
                {
#if UNSAFE
                    if (_bufferHandle.HasValue)
                        _bufferHandle.Value.Dispose();
                    _buffer = new ReadOnlyMemory<T>(Unsafe.AsRef<T[]>((Unsafe.As<List<T>, IntPtr>(ref _list) + ItemsFieldOffset).ToPointer()));
                    _bufferHandle = _buffer.Value.Pin();                        
#else
                    //本来是直接copy出来的，但这样子原来的list内的数据变了这边不会跟着变，没办法做到和ReadOnlyCollection一样的效果
                    //所以犹豫了挺久后决定还是用反射直接获取list类里面的_items
                    _buffer = new ReadOnlyMemory<T>((T[])ItemsField.GetValue(_list));
#endif
                }
                return _buffer.Value;
            }
        }
        private ReadOnlyMemory<T>? _buffer;

        public ReadOnlyPacketData(List<T> list) : base(list) { _list = list; }

        public ReadOnlySpan<T> Slice(int start)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));

            return Buffer.Span.Slice(start, _list.Count - start);
        }

        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if (start < 0 || start > _list.Count)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0 || start + length > _list.Count)
                throw new ArgumentOutOfRangeException(nameof(length));
            
            return Buffer.Span.Slice(start, length);
        }

        public ReadOnlySpan<T> ToSpan() => Buffer.Span.Slice(0,_list.Count);

        public T[] ToArray()
        {
            T[] bytes = new T[Count];
            CopyTo(bytes, 0);
            return bytes;
        }

#if UNSAFE
        ~ReadOnlyPacketData()
        {
            if (_bufferHandle.HasValue)
                _bufferHandle.Value.Dispose();
        }
#endif

    }
}
