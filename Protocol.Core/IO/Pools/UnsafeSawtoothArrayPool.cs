using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.Pools
{
    public class UnsafeSawtoothArrayPool<T>
    {
        private static GCHandleType DefaultType = GCHandleType.Normal;
        private readonly Bucket<T>[] _buckets;

        public UnsafeSawtoothArrayPool(params int[] bucketSize)
        {
            if (bucketSize.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bucketSize));
            }

            // Create the buckets.
            int poolId = Id;
            int maxBuckets = SelectBucketIndex((int)Math.Pow(2, bucketSize.Length + 3));
            var buckets = new Bucket<T>[maxBuckets + 1];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new Bucket<T>(GetMaxSizeForBucket(i), bucketSize[i], poolId);
            }
            _buckets = buckets;
        }

        private int Id => GetHashCode();

        public GCHandle Rent(int minimumLength)
        {
            // Arrays can't be smaller than zero.  We allow requesting zero-length arrays (even though
            // pooling such an array isn't valuable) as it's a valid length array, and we want the pool
            // to be usable in general instead of using `new`, even for computed lengths.
            if (minimumLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            }

            GCHandle buffer;

            int index = SelectBucketIndex(minimumLength);
            if (index < _buckets.Length)
            {
                // Search for an array starting at the 'index' bucket. If the bucket is empty, bump up to the
                // next higher bucket and try that one, but only try at most a few buckets.
                const int MaxBucketsToTry = 2;
                int i = index;
                do
                {
                    // Attempt to rent from the bucket.  If we get a buffer from it, return it.
                    buffer = _buckets[i].Rent();
                    if (buffer != default)
                    {
                        return buffer;
                    }
                }
                while (++i < _buckets.Length && i != index + MaxBucketsToTry);

                // The pool was exhausted for this buffer size.  Allocate a new buffer with a size corresponding
                // to the appropriate bucket.
                buffer = GCHandle.Alloc(new T[_buckets[index]._bufferLength], DefaultType);
            }
            else
            {
                // The request was for a size too large for the pool.  Allocate an array of exactly the requested length.
                // When it's returned to the pool, we'll simply throw it away.
                buffer = GCHandle.Alloc(new T[minimumLength], DefaultType);
            }

            return buffer;
        }

        public void Return(GCHandle gcHandle)
        {
            T[] array = (T[])gcHandle.Target;
            if (array == null || gcHandle == default)
            {
                throw new ArgumentNullException(nameof(gcHandle));
            }
            else if (array.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gcHandle), "Empty array");
            }

            // Determine with what bucket this array length is associated
            int bucket = SelectBucketIndex(array.Length);

            // If we can tell that the buffer was allocated, drop it. Otherwise, check if we have space in the pool
            bool haveBucket = bucket < _buckets.Length;
            if (haveBucket)
            {
                // Return the buffer to its bucket.  In the future, we might consider having Return return false
                // instead of dropping a bucket, in which case we could try to return to a lower-sized bucket,
                // just as how in Rent we allow renting from a higher-sized bucket.
                _buckets[bucket].Return(gcHandle);
            }
            else
            {
                gcHandle.Free();
            }
        }

        private static int GetMaxSizeForBucket(int binIndex)
        {
            int maxSize = 16 << binIndex;
            Debug.Assert(maxSize >= 0);
            return maxSize;
        }
        private static int SelectBucketIndex(int bufferSize)
        {
            // Buffers are bucketed so that a request between 2^(n-1) + 1 and 2^n is given a buffer of 2^n
            // Bucket index is log2(bufferSize - 1) with the exception that buffers between 1 and 16 bytes
            // are combined, and the index is slid down by 3 to compensate.
            // Zero is a valid bufferSize, and it is assigned the highest bucket index so that zero-length
            // buffers are not retained by the pool. The pool will return the Array.Empty singleton for these.
            return BitOperations.Log2((uint)bufferSize - 1 | 15) - 3;
        }

        private sealed class Bucket<T>
        {
            internal readonly int _bufferLength;
            private readonly GCHandle[] _buffers;
            private readonly int _poolId;

            private SpinLock _lock; // do not make this readonly; it's a mutable struct
            private int _index;

            internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
            {
                _lock = new SpinLock(Debugger.IsAttached); // only enable thread tracking if debugger is attached; it adds non-trivial overheads to Enter/Exit
                _buffers = new GCHandle[numberOfBuffers];
                _bufferLength = bufferLength;
                _poolId = poolId;
            }

            internal int Id => GetHashCode();

            internal GCHandle Rent()
            {
                GCHandle[] buffers = _buffers;
                GCHandle buffer = default;

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
                        buffers[_index++] = default;
                        allocateBuffer = buffer == default;
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
                    buffer = GCHandle.Alloc(new T[_bufferLength], DefaultType);
                }

                return buffer;
            }

            internal void Return(GCHandle gcHandle)
            {
                T[] array = (T[])gcHandle.Target;
                // Check to see if the buffer is the correct size for this bucket
                if (array.Length != _bufferLength)
                {
#if DEBUG
                    throw new ArgumentException("Buffer Not From Pool", nameof(gcHandle));
#else
                    gcHandle.Free();
#endif
                }

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
                    if (returned)
                    {
                        _buffers[--_index] = gcHandle;
                    }
                    else
                    {
                        gcHandle.Free();
                    }
                }
                finally
                {
                    if (lockTaken) _lock.Exit(false);
                }
            }
        }
    }
}
