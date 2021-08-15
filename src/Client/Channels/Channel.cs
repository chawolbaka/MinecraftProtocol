using System;
using System.Collections.Generic;
using MinecraftProtocol.IO;
using MinecraftProtocol.DataType.Forge;

namespace MinecraftProtocol.Client.Channels
{
    public abstract class Channel : IEquatable<Channel>
    {
        public virtual string Name => _channelName;
        protected string _channelName;

        public virtual event EventHandler<ChannelReceivedEventArgs> Received;

        public abstract bool CanRead { get; }
        public abstract bool CanSend { get; }

        internal virtual void TriggerEvent(byte[] data)
        {
            if (!CanRead || Received == null) return;
            foreach (EventHandler<ChannelReceivedEventArgs> x in Received.GetInvocationList())
            {
                ChannelReceivedEventArgs eventArgs = new ChannelReceivedEventArgs(new ByteReader(data,false));
                x.Invoke(this, eventArgs);
                if (eventArgs.IsCancelled)
                    return;
            }
        }

        public abstract void Send(ByteWriter writer);
        public abstract void Send(IEnumerable<byte> data);
        public virtual void Send(IForgeStructure data)
        {
            if (CanSend)
                Send(data.ToBytes());
        }

        public override bool Equals(object obj) => obj is Channel channel && channel.Name == _channelName;
        
        public bool Equals(Channel other) => other.Name == _channelName;
        
        public override int GetHashCode() => _channelName.GetHashCode();

        public override string ToString() => _channelName;
    }
}
