using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Packets.Client;

namespace MinecraftProtocol.Client.Channels
{

    public class ClientChannelManager : IEnumerable<Channel>
    {
        public bool Empty => _channels.Count <= 0;
        public Channel this[string channel] => _channels[channel];

        private bool IsSend = false;
        private MinecraftClient _client;
        private Dictionary<string, Channel> _channels = new Dictionary<string, Channel>();

        public ClientChannelManager(MinecraftClient client) : this(client, null) { }
        public ClientChannelManager(MinecraftClient client, params string[] channels)
        {
            if (channels != null && channels.Length > 0)
                Registry(channels);
            _client = client;
            _client.LoginSuccess += (client, e) =>
            {
                if (_channels.Any(c => c.Value.CanRead))
                    _client.PacketReceived += PluginChannelReceived;
            };
            _client.Disconnected += (client, e) => client.PacketReceived -= PluginChannelReceived;
        }


        public ClientChannelManager Registry(string channel) => Registry(new WriteOlnyClientChannel(channel, _client));
        public ClientChannelManager Registry(params string[] channels)
        {
            foreach (var channel in channels)
                Registry(channel);

            return this;
        }
        public ClientChannelManager Registry(ClientChannel channel)
        {
            if (channel is null)
                throw new ArgumentNullException(nameof(channel));
            if (_client.Joined)
                throw new InvalidOperationException("无法在进入服务器后注册频道");

            if (!_channels.ContainsKey(channel.ToString()))
                _channels.Add(channel.ToString(), channel);

            return this;
        }

        public void SendToServer()
        {
            if (IsSend)
                throw new InvalidOperationException("无法重复注册频道。");
            IsSend = true;
            _client.SendPacket(new ClientPluginChannelPacket(
                "REGISTER", Encoding.UTF8.GetBytes(string.Join('\0', _channels.Keys)), _client is ForgeClient, _client.ProtocolVersion));
        }
        private void PluginChannelReceived(MinecraftClient client, PacketReceivedEventArgs e)
        {
            if (e.Packet == PacketType.Play.Server.PluginChannel && ServerPluginChannelPacket.TryRead(e.Packet, true, out ServerPluginChannelPacket pcp))
            {
                foreach (var channel in _channels.Values)
                {
                    if (channel.CanRead && channel.Name == pcp.Channel)
                        channel.TriggerEvent(pcp.Data);
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => _channels.Values.GetEnumerator();
        public IEnumerator<Channel> GetEnumerator() => _channels.Values.GetEnumerator();
        public void Remove(string channel) => _channels.Remove(channel);
        public void Clear() => _channels.Clear();
        public void Reset()
        {
            IsSend = false;
            Clear();
        }

    }
}
