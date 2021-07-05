using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.IO;
using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Client.Channels
{
    public abstract class Channel
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

        public abstract void Send(IEnumerable<byte> data);
        public virtual void Send(IForgeStructure data)
        {
            if (CanSend)
                Send(data.ToBytes());
        }

        public override string ToString() => _channelName;
        public override int GetHashCode() => _channelName.GetHashCode();
    }
}
