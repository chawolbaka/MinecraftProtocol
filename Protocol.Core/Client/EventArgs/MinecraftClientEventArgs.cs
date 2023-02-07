using MinecraftProtocol.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class MinecraftClientEventArgs : CancelEventArgs
    {
        public virtual DateTime Time { get; }
        public MinecraftClientEventArgs() : this(DateTime.Now) { }
        public MinecraftClientEventArgs(DateTime time)
        {
            Time = time; 
        }
    }
}
