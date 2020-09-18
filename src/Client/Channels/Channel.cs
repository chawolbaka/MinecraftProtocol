using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MinecraftProtocol.Packets;

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
            var invocationList = Received.GetInvocationList();
            if (invocationList.Length == 1)
                (invocationList[0] as EventHandler<ChannelReceivedEventArgs>)?.Invoke(this, new ChannelReceivedEventArgs(data));
            else
                foreach (EventHandler<ChannelReceivedEventArgs> x in invocationList)
                {
                    byte[] temp = new byte[data.Length];
                    Array.Copy(data, temp, data.Length);
                    ChannelReceivedEventArgs eventArgs = new ChannelReceivedEventArgs(temp);
                    x.Invoke(this, eventArgs);
                    if (eventArgs.IsCancelled)
                        return;
                }
        }

        public abstract void Send(ICollection<byte> data);

        public override string ToString() => _channelName;
        public override int GetHashCode() => _channelName.GetHashCode();
    }
}
