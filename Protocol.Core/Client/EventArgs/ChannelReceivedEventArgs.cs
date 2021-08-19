using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.Client
{
    public class ChannelReceivedEventArgs : PacketEventArgs
    {
        public ByteReader Data { get; }

        public ChannelReceivedEventArgs(ByteReader data) : this(data, DateTime.Now) { }
        public ChannelReceivedEventArgs(ByteReader data, DateTime time) : base(time)
        {
            Data = data;
        }
    }
}
