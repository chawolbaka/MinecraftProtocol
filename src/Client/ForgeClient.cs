using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using MinecraftProtocol.Auth;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Both;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Client.Channels;

namespace MinecraftProtocol.Client
{

    /// <summary>
    /// Forge客户端
    /// </summary>
    public class ForgeClient : VanillaClient
    {
     
        public event ForgeLoginEventHandler ForgeLoginStatusChanged { add => _loginStatusChanged += ThrowIfDisposed(value); remove => _loginStatusChanged -= ThrowIfDisposed(value); }
        public delegate void ForgeLoginEventHandler(MinecraftClient sender, ForgeLoginEventArgs args);
        private ForgeLoginEventHandler _loginStatusChanged;

        public virtual byte FMLProtocolVersion { get; set; }

        public virtual ForgeLoginStatus ForgeLoginState
        {
            get => ThrowIfDisposed(_forgeLoginState);
            protected set
            {
                _forgeLoginState = value;
                if (value == ForgeLoginStatus.Success)
                {
                    _loginSuccess?.Invoke(this, new ForgeLoginEventArgs(value));
                    _joined = true;
                }
                _loginStatusChanged?.Invoke(this, new ForgeLoginEventArgs(value));
            }
        }
        public virtual FMLHandshakeClientState HandshakeState
        {
            get => ThrowIfDisposed(_handshakeState);
            protected set
            {
                _handshakeState = value;
                if (value == FMLHandshakeClientState.COMPLETE)
                    ForgeLoginState = ForgeLoginStatus.Success;
                else
                    ForgeLoginState = ForgeLoginStatus.HandshakeAck;
                
                Channel["FML|HS"].Send(new HandshakeAck(value));
            }
        }
        protected ForgeLoginStatus _forgeLoginState;
        protected FMLHandshakeClientState _handshakeState;


        public virtual ClientChannelManager Channel => _channelManager;
        protected ClientChannelManager _channelManager;
        

        public virtual ModList ClientModList => ThrowIfDisposed(_clientModList);
        public virtual ModList ServerModList => ThrowIfDisposed(_serverModList);
        protected ModList _clientModList;
        protected ModList _serverModList;
        protected string[] _serverChannels;

        public ForgeClient(string host, IPAddress ip, ushort port,  ModList clientMods, IClientSettings settings, int protocolVersion) : base(host, ip, port, settings, protocolVersion)
        {
            if (clientMods is null || clientMods.Count < 1)
                throw new ArgumentNullException(nameof(ModList), "缺少必要的客户端mod");
            ServerHost += "\0FML\0";
            _clientModList = clientMods;
            _channelManager = new ClientChannelManager(this);
        }
        public ForgeClient(string host, IPAddress ip, ushort port, ModList clientMods, int protocolVersion) : this(host, ip, port, clientMods, null, protocolVersion) { }
        public ForgeClient(string host, IPEndPoint remoteEP, ModList clientMods, int protocolVersion) : this(host, remoteEP.Address, (ushort)remoteEP.Port, clientMods, null, protocolVersion) { }
        public ForgeClient(string host, IPEndPoint remoteEP, ModList clientMods, IClientSettings settings, int protocolVersion) : this(host, remoteEP.Address, (ushort)remoteEP.Port, clientMods, settings, protocolVersion) { }
        public ForgeClient(IPAddress ip, ushort port, ModList clientMods, int protocolVersion) : this(null, ip, port, clientMods, null, protocolVersion) { }
        public ForgeClient(IPAddress ip, ushort port, ModList clientMods, IClientSettings settings, int protocolVersion) : this(null, ip, port, clientMods, settings, protocolVersion) { }
        public ForgeClient(IPEndPoint remoteEP, ModList clientMods, int protocolVersion) : this(null, remoteEP.Address, (ushort)remoteEP.Port, clientMods, null, protocolVersion) { }
        public ForgeClient(IPEndPoint remoteEP, ModList clientMods, IClientSettings settings, int protocolVersion) : this(null, remoteEP.Address, (ushort)remoteEP.Port, clientMods, settings, protocolVersion) { }

        public override bool Join(SessionToken token)
        {
            ThrowIfDisposed();
            if (!base.Join(token))
                return false;
            _joined = false;
            _handshakeState = FMLHandshakeClientState.START;
            ForgeLoginState = ForgeLoginStatus.Start;
            int count = 0;
            while (ForgeLoginState != ForgeLoginStatus.Failed&&HandshakeState != FMLHandshakeClientState.COMPLETE)
            {
                Packet packet = ReadPacket();
                if (++count > 20960)
                    throw new OverflowException("异常的登录过程，服务端发送的数据包过多");
                else if (DisconnectPacket.Verify(packet, ProtocolVersion, out DisconnectPacket dp))
                    OnDisconnectLoginReceived(dp);
                else if (DisconnectLoginPacket.Verify(packet, ProtocolVersion, out DisconnectLoginPacket dlp))
                    OnDisconnectLoginReceived(dlp);
                else if (!PluginChannelPacket.Verify(packet, ProtocolVersion, Bound.Server, true, out PluginChannelPacket pcp))
                    continue;
                else if (ForgeLoginState == ForgeLoginStatus.Start)
                    OnHandshakeStartState(pcp);
                else if (ForgeLoginState == ForgeLoginStatus.ServerRegisterChannel)
                    OnServerRegisterChannelAfter(pcp);
                else if (ForgeLoginState == ForgeLoginStatus.SendModList)
                    OnSendModListAfter(pcp);
                else if (HandshakeState == FMLHandshakeClientState.WAITINGSERVERDATA)
                    OnWaitServerData(pcp);
                else if (pcp.Data[0] == HandshakeAck.Discriminator)
                    OnHandshakeAckState(pcp);
            }
            return ForgeLoginState == ForgeLoginStatus.Success;
        }
        protected virtual void OnHandshakeStartState(PluginChannelPacket packet)
        {
            if (packet.Channel != "REGISTER")
            {
                ForgeLoginState = ForgeLoginStatus.Failed;
                throw new LoginException("错误的登录流程");
            }
            _serverChannels = Encoding.UTF8.GetString(packet.Data).Split('\0');
            ForgeLoginState = ForgeLoginStatus.ServerRegisterChannel;
        }

        protected virtual void OnServerRegisterChannelAfter(PluginChannelPacket packet)
        {
            FMLProtocolVersion = ServerHello.Read(packet.Data).FMLProtocolVersion;
            ForgeLoginState = ForgeLoginStatus.ServerHello;

            if (_channelManager.Empty)
                _channelManager.Registry(_serverChannels).SendToServer();
            ForgeLoginState = ForgeLoginStatus.ClientRegisterChannel;

            Channel["FML|HS"].Send(new ClientHello(FMLProtocolVersion));
            ForgeLoginState = ForgeLoginStatus.ClientHello;

            Channel["FML|HS"].Send(_clientModList);
            ForgeLoginState = ForgeLoginStatus.SendModList;
        }

        protected virtual void OnSendModListAfter(PluginChannelPacket packet)
        {
            _serverModList = ModList.Read(packet.Data);
            ForgeLoginState = ForgeLoginStatus.ReceiveModList;
            HandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;
        }

        protected virtual void OnWaitServerData(PluginChannelPacket packet)
        {
            if (ProtocolVersion >= ProtocolVersionNumbers.V1_8)
            {
                if (!RegistryData.Read(packet.Data).HasMore)
                {
                    ForgeLoginState = ForgeLoginStatus.RegistryData;
                    HandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;
                }
            }
            else if (packet.Data[0] != ModIdData.Discriminator)
            {
                HandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;
            }
        }
        protected virtual void OnHandshakeAckState(PluginChannelPacket packet)
        {
            if (HandshakeAck.Read(packet.Data) == FMLHandshakeServerState.WAITINGCACK)
                HandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
            if (HandshakeAck.Read(packet.Data) == FMLHandshakeServerState.COMPLETE)
                HandshakeState = FMLHandshakeClientState.COMPLETE;
        }

        protected override void OnLoginSuccessReceived(LoginSuccessPacket lsp)
        {
            SetPlayer(lsp);
            VanillaLoginState = VanillaLoginStatus.Success;
        }

        protected override void OnDisconnectLoginReceived(DisconnectPacket dp)
        {
            ForgeLoginState = ForgeLoginStatus.Failed;
            _kicked?.Invoke(this, new DisconnectEventArgs(dp.Reason, DateTime.Now));
            DisconnectAsync(dp.Json);
        }
        
        protected override void OnDisconnectLoginReceived(DisconnectLoginPacket dlp)
        {
            ForgeLoginState = ForgeLoginStatus.Failed;
            DisconnectAsync(dlp.Json);
        }

        public override void Disconnect(string reason, bool reuseSocket = false)
        {
            base.Disconnect(reason,reuseSocket);
            
            _serverModList = null;
            Channel.Reset();
        }

        public override string ToString() => base.ToString().Replace("\0FML\0", "");

        public class ClientChannelManager : IEnumerable<Channel>
        {
            public bool Empty => _channels.Count <= 0;
            public Channel this[string channel] => _channels[channel];

            private bool IsSend = false;
            private int PluginChannelPacketID;
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
                    PluginChannelPacketID = PluginChannelPacket.GetPacketID(_client.ProtocolVersion, Bound.Server);
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
                _client.SendPacket(new PluginChannelPacket(
                    "REGISTER", Encoding.UTF8.GetBytes(string.Join('\0', _channels.Keys)), _client.ProtocolVersion, Bound.Client, _client is ForgeClient));
            }
            private void PluginChannelReceived(MinecraftClient client, PacketReceivedEventArgs e)
            {
                if (e.Packet.ID == PluginChannelPacketID && PluginChannelPacket.Verify(e.Packet, client.ProtocolVersion, Bound.Server, true, out PluginChannelPacket pcp))
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
}
