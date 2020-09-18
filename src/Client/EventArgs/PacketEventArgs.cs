using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class PacketEventArgs : MinecraftClientEventArgs
    {
        public virtual bool IsCancelled => _isCancelled;
        private bool _isCancelled;

        public PacketEventArgs() : base() { }
        public PacketEventArgs(DateTime time) : base(time) { }

        public virtual void Cancel()
        {
            _isCancelled = true;
        }
    }
}
