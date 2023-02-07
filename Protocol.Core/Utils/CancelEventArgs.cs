using System;

namespace MinecraftProtocol.Utils
{
    public class CancelEventArgs : EventArgs, ICancelEvent
    {
        public virtual bool IsCancelled => _isCancelled;
        protected bool _isCancelled;

        public virtual void Cancel()
        {
            _isCancelled = true;
        }

    }
}
