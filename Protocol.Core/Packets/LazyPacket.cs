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
        

        protected abstract T InitializePacket();
        public virtual T Get()
        {
            if (_packet == null)
            {
                _packet = InitializePacket();
                _isCreated = true;
            }
            return _packet;

        }

        public virtual void Dispose()
        {
            if (_isCreated)
                _packet.Dispose();
        }
    }

}
