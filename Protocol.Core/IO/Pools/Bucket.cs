using System;
using System.Diagnostics;
using System.Threading;

namespace MinecraftProtocol.IO.Pools
{
    public sealed class Bucket<T>
    {
        internal readonly int _bufferLength;
        private readonly T[]?[] _buffers;
        private readonly int _poolId;

        private SpinLock _lock; // do not make this readonly; it's a mutable struct
        private int _index;

        public Bucket(int bufferLength, int numberOfBuffers, int poolId, bool preAlloc)
        {
            _lock = new SpinLock(Debugger.IsAttached); // only enable thread tracking if debugger is attached; it adds non-trivial overheads to Enter/Exit
            _buffers = new T[numberOfBuffers][];
            _bufferLength = bufferLength;
            _poolId = poolId;
            if (preAlloc)
            {
                for (int i = 0; i < numberOfBuffers; i++)
                {
                    _buffers[i] = new T[bufferLength];
                }
            }
        }

        internal int Id => GetHashCode();

        public T[]? Rent()
        {
            T[]?[] buffers = _buffers;
            T[]? buffer = null;

            // While holding the lock, grab whatever is at the next available index and
            // update the index.  We do as little work as possible while holding the spin
            // lock to minimize contention with other threads.  The try/finally is
            // necessary to properly handle thread aborts on platforms which have them.
            bool lockTaken = false, allocateBuffer = false;
            try
            {
                _lock.Enter(ref lockTaken);

                if (_index < buffers.Length)
                {
                    buffer = buffers[_index];
                    buffers[_index++] = null;
                    allocateBuffer = buffer == null;
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            // While we were holding the lock, we grabbed whatever was at the next available index, if
            // there was one.  If we tried and if we got back null, that means we hadn't yet allocated
            // for that slot, in which case we should do so now.
            if (allocateBuffer)
            {
                buffer = new T[_bufferLength];
            }

            return buffer;
        }

        public void Return(T[] array)
        {
            // Check to see if the buffer is the correct size for this bucket
#if DEBUG
            if (array.Length != _bufferLength)
            {
                throw new ArgumentException("Buffer Not From Pool", nameof(array));
            }
#else
            return;
#endif
            bool returned;

            // While holding the spin lock, if there's room available in the bucket,
            // put the buffer into the next available slot.  Otherwise, we just drop it.
            // The try/finally is necessary to properly handle thread aborts on platforms
            // which have them.
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);

                returned = _index != 0;
#if DEBUG
                //在调试时清空数组可以看的更清晰，不至于与之前的数据混淆，但非调试环境下没必要清空数组。
                Array.Clear(array);
#endif
                if (returned)
                {
                    _buffers[--_index] = array;
                }
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }
    }
}
