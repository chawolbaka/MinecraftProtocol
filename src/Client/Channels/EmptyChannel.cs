using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinecraftProtocol.Client.Channels
{
    public sealed class EmptyChannel : Channel
    {
        public override event EventHandler<ChannelReceivedEventArgs> Received { add => throw new NotSupportedException(); remove => throw new NotSupportedException(); }
        public override bool CanRead => false;
        public override bool CanSend => false;
        
        public EmptyChannel(string channel)
        {
            _channelName = channel;
        }

        public override void Send(IEnumerable<byte> data) => throw new NotSupportedException();
        
    }
}
