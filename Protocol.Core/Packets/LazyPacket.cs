using System;
using System.Diagnostics;
using System.Threading;

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

#if DEBUG
        protected SpinLock _getLock = new SpinLock(Debugger.IsAttached);
#else
        protected SpinLock _getLock = new SpinLock(false);
#endif

        protected abstract T InitializePacket();
        public virtual T Get()
        {
            bool lockTaken = false;
            try
            {
                _getLock.Enter(ref lockTaken);

                if (_isCreated)
                    return _packet;

                _isCreated = true;
            }
            finally
            {
                if (lockTaken)
                    _getLock.Exit();
            }

            _packet = InitializePacket();
            return _packet;
        }

        public virtual void Dispose()
        {
            if (_isCreated)
                _packet.Dispose();
        }
    }
}
