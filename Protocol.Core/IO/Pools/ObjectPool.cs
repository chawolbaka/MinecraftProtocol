using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MinecraftProtocol.IO.Pools
{
    /// <summary>
    /// By: https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
    /// </summary>
    public class ObjectPool<T> : IPool<T> where T : class, new()
    {
        protected readonly ConcurrentBag<T> _objects;
        public ObjectPool()
        {
            _objects = new ConcurrentBag<T>();
        }

        public virtual T Rent() => _objects.TryTake(out T item) ? item : new T();
        public virtual void Return(T item) => _objects.Add(item);
    }
}
