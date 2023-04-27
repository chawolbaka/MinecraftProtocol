using MinecraftProtocol.Compatible;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets
{
    public abstract class LazyPacket<T> : IDisposable where T : Packet
    {
        public virtual bool IsCreated => _isCreated;
        public virtual int Version => _isCreated ? _packet._version : -1;
        public virtual int Id => _id;
        
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
