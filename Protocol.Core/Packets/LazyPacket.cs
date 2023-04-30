using System;

namespace MinecraftProtocol.Packets
{
    public abstract class LazyPacket<T> : IDisposable where T : Packet
    {
        public virtual int Id { get => _isCreated ? _packet.Id : _id; set => Get().Id = value; }

        public virtual int Version => _isCreated ? _packet._version : -1;

        public virtual bool IsCreated => _isCreated;

        protected bool _isCreated;
        protected T _packet;
        protected int _id;

        private readonly object _getLock = new object();
        private int d;
        protected abstract T InitializePacket();
        public virtual T Get()
        {
            lock (_getLock)
            {
                if (!_isCreated)
                {
                    _packet = InitializePacket();
                    _isCreated = true;
                    d++;
                }

                if (d > 1)
                    Console.WriteLine(d);

                return _packet;
            }
        }

        public virtual void Dispose()
        {
            if (_isCreated)
                _packet.Dispose();
        }
    }

}
