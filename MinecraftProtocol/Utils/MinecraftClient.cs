using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace MinecraftProtocol.Utils
{
    public abstract class MinecraftClient
    {
        public IPAddress ServerIP { get; protected set; }
        public ushort ServerPort { get; protected set; }
        public abstract void Connect();
    }
}
