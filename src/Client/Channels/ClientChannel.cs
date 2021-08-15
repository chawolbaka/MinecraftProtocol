using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftProtocol.IO;
using MinecraftProtocol.Packets.Both;

namespace MinecraftProtocol.Client.Channels
{

    public class ClientChannel : Channel
    {
        protected MinecraftClient _client;

        public override bool CanRead => true;
        public override bool CanSend => true;

        public ClientChannel(string channel, MinecraftClient client)
        {
            _channelName = channel;
            _client = client;
        }

        public override void Send(IEnumerable<byte> data)
        {
            if (CanSend)
                SendPluginChannelPacket(data as byte[] ?? data.ToArray());
        }

        public override void Send(ByteWriter writer)
        {
            if (CanSend)
                SendPluginChannelPacket(writer.AsSpan().ToArray());
        }

        protected virtual void SendPluginChannelPacket(byte[] data)
        {
            PluginChannelPacket packet = new PluginChannelPacket(_channelName, data, _client.ProtocolVersion, Bound.Client, _client is ForgeClient);
            _client.SendPacket(packet);
        }
    }
}
