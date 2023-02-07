using System;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.IO
{
    public class ListenEventArgs : CancelEventArgs
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
