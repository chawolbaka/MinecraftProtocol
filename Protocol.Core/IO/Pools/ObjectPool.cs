// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// define TRACE_LEAKS to get additional diagnostics that can lead to the leak sources. note: it will
// make everything about 2-3x slower


using System;
using System.Threading;


namespace MinecraftProtocol.IO.Pools
{

    public class ObjectPool<T>: IPool<T> where T : class, new()
    {
        private struct Element
        {
            internal T Value;
        }

        // Storage for the pool objects. The first item is stored in a dedicated field because we
        // expect to be able to satisfy most requests from it.
        private T _firstItem;
        private readonly Element[] _items;

        // factory is stored for the lifetime of the pool. We will call this only when pool needs to
        // expand. compared to "new T()", Func gives more flexibility to implementers and faster
        // than "new T()".
        private readonly Func<T> _objectGenerator;

        public ObjectPool() : this(() => new T()) { }
        public ObjectPool(Func<T> objectGenerator)  : this(objectGenerator, Environment.ProcessorCount * 1024) { }
        public ObjectPool(Func<T> objectGenerator, int size)
        {
            if(size<=0)
                throw new ArgumentOutOfRangeException(nameof(size));

            _objectGenerator = objectGenerator;
            _items = new Element[size - 1];
        }

        private T CreateInstance()
        {
            var inst = _objectGenerator();
            return inst;
        }

        public virtual T Rent()
        {
            // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            // recently returned objects. Not a big deal.
            var inst = _firstItem;
            if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
            {
                inst = RentSlow();
            }
            return inst;
        }

        private T RentSlow()
        {
            var items = _items;

            for (var i = 0; i < items.Length; i++)
            {
                var inst = items[i].Value;
                if (inst != null)
                {
                    if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                    {
                        return inst;
                    }
                }
            }

            return CreateInstance();
        }

        public virtual void Return(T obj)
        {

            if (_firstItem == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                _firstItem = obj;
            }
            else
            {
                ReturnSlow(obj);
            }
        }

        private void ReturnSlow(T obj)
        {
            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i].Value == null)
                {
                    // Intentionally not using interlocked here. 
                    // In a worst case scenario two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    items[i].Value = obj;
                    break;
                }
            }
        }

    }
}
