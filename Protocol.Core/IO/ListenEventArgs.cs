using System;

namespace MinecraftProtocol.IO
{
    public class ListenEventArgs : EventArgs
    {
        public DateTime Time { get; }
        public bool IsStop { get; }

        public ListenEventArgs(bool isStop)
        {
            Time = DateTime.Now;
            IsStop = isStop;
        }
    }

}
