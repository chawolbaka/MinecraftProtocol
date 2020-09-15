using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Threading.Tasks;
using MinecraftProtocol.Protocol.Packets.Both;

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

        public override void Send(ICollection<byte> data)
        {
            if (!CanSend)
                return;

            PluginChannelPacket packet = new PluginChannelPacket(_channelName, data, _client.ProtocolVersion, Bound.Client, _client is ForgeClient);
            _client.SendPacket(packet);
        }

    }
}
