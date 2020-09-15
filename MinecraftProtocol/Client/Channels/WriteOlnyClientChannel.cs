using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client.Channels
{
    /// <summary>
    /// 仅用于写入的频道，不会接收到任何属于频道的数据
    /// </summary>
    public sealed class WriteOlnyClientChannel : ClientChannel
    {
        public override bool CanRead => false;
        public override bool CanSend => true;

        public WriteOlnyClientChannel(string channel, MinecraftClient client) : base(channel, client)  { }
    }
}
