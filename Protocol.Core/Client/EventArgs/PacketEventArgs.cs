using MinecraftProtocol.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class PacketEventArgs : MinecraftClientEventArgs
    {
        public PacketEventArgs() : base() { }
        public PacketEventArgs(DateTime time) : base(time) { }
    }
}
