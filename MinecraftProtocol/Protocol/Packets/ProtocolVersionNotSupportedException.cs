using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Protocol.Packets
{
    public class ProtocolVersionNotSupportedException:Exception
    {
        int MaxSupportVersion { get; }
        int MinSupportVersion { get; }
        public ProtocolVersionNotSupportedException(string meassage,int max,int min):base(meassage)
        {
            this.MaxSupportVersion = max;
            this.MinSupportVersion = min;
        }
    }
}
