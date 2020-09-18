using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using MinecraftProtocol.Auth;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Both;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Compatible;
using System.Collections;
using System.Threading.Tasks;
using MinecraftProtocol.Client.Channels;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.Client
{

    /// <summary>
    /// Forge客户端
    /// </summary>
    public class ForgeClient : VanillaClient
    {
        public override string ServerHost { get => base.ServerHost + "\0FML\0"; protected set => base.ServerHost = value; }
        public override string ToString() => base.ToString().Replace("\0FML\0", "");

        public event ForgeLoginEventHandler ForgeLoginStatusChanged { add => _loginStatusChanged += ThrowIfDisposed(value); remove => _loginStatusChanged -= ThrowIfDisposed(value); }
        public delegate void ForgeLoginEventHandler(MinecraftClient sender, ForgeLoginEventArgs args);
        
        private ForgeLoginEventHandler _loginStatusChanged;

        private void UpdateLoginStatus(ForgeLoginStatus value)
        {
            if (value == ForgeLoginStatus.Success)
                 _loginSuccess?.Invoke(this, new ForgeLoginEventArgs(value));
            
            _loginStatusChanged?.Invoke(this, new ForgeLoginEventArgs(value));
        }
        public virtual FMLHandshakeClientState HandshakeState
        {
            get => ThrowIfDisposed(_handshakeState);
            protected set
            {
                SendPluginMessage("FML|HS", new HandshakeAck(value));
                _handshakeState = value;
                UpdateLoginStatus(ForgeLoginStatus.HandshakeAck);
            }
        }
        protected FMLHandshakeClientState _handshakeState;


        public virtual ClientChannelManager Channel => _channelManager;
        protected ClientChannelManager _channelManager;

        public virtual ModList ClientModList => ThrowIfDisposed(_clientModList);
        public virtual ModList ServerModList => ThrowIfDisposed(_serverModList);
        protected ModList _clientModList;
        protected ModList _serverModList;

        public ForgeClient(string host, IPAddress ip, ushort port,  ModList clientMods, IClientSettings settings, int protocolVersion) : base(host, ip, port, settings, protocolVersion)
        {
            if (clientMods is null || clientMods.Count < 1)
                throw new ArgumentNullException(nameof(ModList), "缺少必要的客户端mod");
            _clientModList = clientMods;
            _channelManager = new ClientChannelManager(this);
            LoginSuccess += RegisterEvent;
        }
        public ForgeClient(string host, IPAddress ip, ushort port, ModList clientMods, int protocolVersion) : this(host, ip, port, clientMods, null, protocolVersion) { }
        public ForgeClient(string host, IPEndPoint remoteEP, ModList clientMods, int protocolVersion) : this(host, remoteEP.Address, (ushort)remoteEP.Port, clientMods, null, protocolVersion) { }
        public ForgeClient(string host, IPEndPoint remoteEP, ModList clientMods, IClientSettings settings, int protocolVersion) : this(host, remoteEP.Address, (ushort)remoteEP.Port, clientMods, settings, protocolVersion) { }
        public ForgeClient(IPAddress ip, ushort port, ModList clientMods, int protocolVersion) : this(null, ip, port, clientMods, null, protocolVersion) { }
        public ForgeClient(IPAddress ip, ushort port, ModList clientMods, IClientSettings settings, int protocolVersion) : this(null, ip, port, clientMods, settings, protocolVersion) { }
        public ForgeClient(IPEndPoint remoteEP, ModList clientMods, int protocolVersion) : this(null, remoteEP.Address, (ushort)remoteEP.Port, clientMods, null, protocolVersion) { }
        public ForgeClient(IPEndPoint remoteEP, ModList clientMods, IClientSettings settings, int protocolVersion) : this(null, remoteEP.Address, (ushort)remoteEP.Port, clientMods, settings, protocolVersion) { }

        private void RegisterEvent(MinecraftClient client,LoginEventArgs e)
        {
            //注册频道接收到消息后的处理事件
            foreach (var channel in Channel)
            {
                //如果没有可读的频道就不注册事件了
                if (channel.CanRead)
                {
                    //防止启动大量的线程，所以提前获取一下包ID
                    PluginChannelPacketID = PluginChannelPacket.GetPacketID(ProtocolVersion, Bound.Server);
                    client.PacketReceived += PluginChannelReceived;
                    break;
                }
            }
        }
        private void ClaerRegisterEvent()
        {
            PacketReceived -= PluginChannelReceived;
        }
        private int PluginChannelPacketID;
        private void PluginChannelReceived(MinecraftClient client, PacketReceivedEventArgs e)
        {
            if (e.Packet.ID != PluginChannelPacketID)
                return;
            else
                Task.Run(() =>
                {
                    if (PluginChannelPacket.Verify(e.Packet, client.ProtocolVersion, Bound.Server, true, out PluginChannelPacket pcp))
                    {
                        foreach (var channel in Channel)
                        {
                            if (channel.CanRead && channel.ToString() == pcp.Channel)
                                channel.TriggerEvent(pcp.Data);
                        }
                    }
                });
        }        

        public override bool Join(SessionToken token, out ChatMessage disconnectReason)
        {
            ThrowIfDisposed();
            disconnectReason = null;
            try
            {
                //发送原版的登录包
                Packet LoginSuccessPacket = SendLoginPacket(token);
                SetPlayer(LoginSuccessPacket);

                //S→C: 注册插件频道
                PluginChannelPacket RegisterChannelPacket = ReadPluginMessage();
                string[] ServerChannels;
                if (RegisterChannelPacket.Channel == "REGISTER")
                    ServerChannels = Encoding.UTF8.GetString(RegisterChannelPacket.Data).Split('\0');
                else
                    throw new LoginException("错误的登录流程");

                UpdateLoginStatus(ForgeLoginStatus.ServerRegisterChannel);

                //S→C: A ServerHello packet is sent on FML|HS including the player's dimension (0 if it's the first login)
                PluginChannelPacket ServerHelloPacket = ReadPluginMessage();
                byte FMLProtocolVersion = ServerHello.Read(ServerHelloPacket.Data).FMLProtocolVersion;
                UpdateLoginStatus(ForgeLoginStatus.ServerHello);

                //C→S: 注册插件频道
                if (_channelManager.Empty)
                    _channelManager.Registry(ServerChannels).SendToServer();
                UpdateLoginStatus(ForgeLoginStatus.ClientRegisterChannel);

                //C→S: 通过 FML|HS 频道发送一个ClientHello
                Channel["FML|HS"].Send(new ClientHello(FMLProtocolVersion).ToBytes());
                UpdateLoginStatus(ForgeLoginStatus.ClientHello);

                //C→S: 发送客户端mod列表, 服务器会拿去和自己的比较,如果有缺少就断开连接。
                var x = NetworkUtils.CheckConnect(TCP);
                Channel["FML|HS"].Send(_clientModList.ToBytes());
                UpdateLoginStatus(ForgeLoginStatus.SendModList);

                //S→C: A ModList packet is sent.
                PluginChannelPacket ServerModListPacket = ReadPluginMessage();
                _serverModList = ModList.Read(ServerModListPacket.Data);
                UpdateLoginStatus(ForgeLoginStatus.ReceiveModList);

                //C→S: A HandshakeAck packet is sent, with the phase being WAITINGSERVERDATA (2).
                HandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;

                //S→C: A series of RegistryData packets is sent, with hasMore being true for all packets except the last.
                if (ProtocolVersion >= ProtocolVersionNumbers.V1_8)
                {
                    while (RegistryData.Read(ReadPluginMessage().Data).HasMore) { }
                    UpdateLoginStatus(ForgeLoginStatus.RegistryData);
                }

                //C→S: A HandshakeAck packet is sent with phase being WAITINGSERVERCOMPLETE (3).
                HandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;

                while (HandshakeState != FMLHandshakeClientState.COMPLETE)
                {
                    try
                    {
                        PluginChannelPacket StatePacket = ReadPluginMessage();
                        if (StatePacket.Data.Length != 2)
                            continue;
                        else if (HandshakeAck.Read(StatePacket.Data) == FMLHandshakeServerState.WAITINGCACK)
                            HandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                        else if (HandshakeAck.Read(StatePacket.Data) == FMLHandshakeServerState.COMPLETE)
                            HandshakeState = FMLHandshakeClientState.COMPLETE;
                    }
                    catch (InvalidCastException) { }
                }
                _joined = true;
                UpdateLoginStatus(ForgeLoginStatus.Success);
                return true;
            }
            catch (InvalidPacketException ipe)
            {
                if (DisconnectLoginPacket.Verify(ipe.Packet, ProtocolVersion, out DisconnectLoginPacket dlp))
                {
                    disconnectReason = dlp.Reason;
                    Disconnect(disconnectReason.ToString()); return false;
                }
                else if (DisconnectPacket.Verify(ipe.Packet, ProtocolVersion, out DisconnectPacket dp))
                {
                    disconnectReason = dp.Reason;
                    Disconnect(disconnectReason.ToString()); return false;
                }
                else
                    throw;
            }
        }

        protected virtual PluginChannelPacket ReadPluginMessage()
        {
            Packet packet = ReadPacket();
            if (PluginChannelPacket.Verify(packet, ProtocolVersion, Bound.Server, true, out PluginChannelPacket pcp))
                return pcp;
            else
                throw new InvalidPacketException(packet);
        }

        public virtual void SendPluginMessage(string channel, IForgeStructure forgeStruct)
        {
            SendPacket(new PluginChannelPacket(channel, forgeStruct, ProtocolVersion, Bound.Client, true));
        }

        public virtual void SendPluginMessage(string channel, byte[] data)
        {
            SendPacket(new PluginChannelPacket(channel, data, ProtocolVersion, Bound.Client, true));
        }

        public override void Disconnect(string reason, bool reuseSocket = false)
        {
            base.Disconnect(reason,reuseSocket);
            _handshakeState = FMLHandshakeClientState.START;
            _serverModList = null;
            ClaerRegisterEvent();
        }

        public class ClientChannelManager:IEnumerable<Channel>
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

            IEnumerator IEnumerable.GetEnumerator() => _channels.Values.GetEnumerator();
            public IEnumerator<Channel> GetEnumerator() => _channels.Values.GetEnumerator();
            public void Remove(string channel) => _channels.Remove(channel);
            public void Clear() => _channels.Clear();


        }
    }
}
