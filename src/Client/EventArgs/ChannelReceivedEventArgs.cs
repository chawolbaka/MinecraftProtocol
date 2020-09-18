using System;

namespace MinecraftProtocol.Client
{
    public class ChannelReceivedEventArgs : PacketEventArgs
    {
        public byte[] Data { get; }

        public ChannelReceivedEventArgs(byte[] data) : this(data, DateTime.Now) { }
        public ChannelReceivedEventArgs(byte[] data, DateTime time) : base(time)
        {
            Data = data;
        }
    }
}
