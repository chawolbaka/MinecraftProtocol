using System;
using System.Net;
using System.Text;
using MinecraftProtocol.Auth;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Client.Channels;
using MinecraftProtocol.Utils;
using System.Threading.Tasks;

namespace MinecraftProtocol.Client
{

    /// <summary>
    /// Forge客户端
    /// </summary>
    public partial class ForgeClient : VanillaClient
    {
        public event CommonEventHandler<ForgeClient, ForgeLoginEventArgs> ForgeLoginStatusChanged { add => _loginStatusChanged += ThrowIfDisposed(value); remove => _loginStatusChanged -= ThrowIfDisposed(value); }
        private CommonEventHandler<ForgeClient, ForgeLoginEventArgs> _loginStatusChanged;

        public virtual byte FMLProtocolVersion { get; set; }

        public virtual ForgeLoginStatus ForgeLoginState
        {
            get => ThrowIfDisposed(_forgeLoginState);
            protected set
            {
                _forgeLoginState = value;
                if (value == ForgeLoginStatus.Success)
                {
                    EventUtils.InvokeCancelEvent(_loginSuccess, this, new ForgeLoginEventArgs(value));
                    _joined = true;
                }
                EventUtils.InvokeCancelEvent(_loginStatusChanged, this, new ForgeLoginEventArgs(value));
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

        public override async Task<bool> JoinAsync(SessionToken token)
        {
            ThrowIfDisposed();
            if (!await base.JoinAsync(token))
                return false;

            _joined = false;
            _handshakeState = FMLHandshakeClientState.START;
            ForgeLoginState = ForgeLoginStatus.Start;
            int count = 0;
            while (ForgeLoginState != ForgeLoginStatus.Failed&&HandshakeState != FMLHandshakeClientState.COMPLETE)
            {
                using CompatiblePacket packet = ReadPacket();
                if (++count > 20960)
                    throw new OverflowException("异常的登录过程，服务端发送的数据包过多");
                else if (DisconnectPacket.TryRead(packet, out DisconnectPacket dp))
                    OnDisconnectLoginReceived(dp);
                else if (DisconnectLoginPacket.TryRead(packet, out DisconnectLoginPacket dlp))
                    OnDisconnectLoginReceived(dlp);
                else if (!ServerPluginChannelPacket.TryRead(packet, true, out ServerPluginChannelPacket pcp))
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
        protected virtual void OnHandshakeStartState(ServerPluginChannelPacket packet)
        {
            if (packet.Channel != "REGISTER")
            {
                ForgeLoginState = ForgeLoginStatus.Failed;
                throw new LoginException("错误的登录流程");
            }
            _serverChannels = Encoding.UTF8.GetString(packet.Data).Split('\0');
            ForgeLoginState = ForgeLoginStatus.ServerRegisterChannel;
        }

        protected virtual void OnServerRegisterChannelAfter(ServerPluginChannelPacket packet)
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

        protected virtual void OnSendModListAfter(ServerPluginChannelPacket packet)
        {
            _serverModList = ModList.Read(packet.Data);
            ForgeLoginState = ForgeLoginStatus.ReceiveModList;
            HandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;
        }

        protected virtual void OnWaitServerData(ServerPluginChannelPacket packet)
        {
            if (ProtocolVersion >= ProtocolVersions.V1_8)
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
        protected virtual void OnHandshakeAckState(ServerPluginChannelPacket packet)
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
            EventUtils.InvokeCancelEvent(_kicked, this, new DisconnectEventArgs(dp.Reason, DateTime.Now));
            DisconnectAsync(dp.Json);
        }
        
        protected override void OnDisconnectLoginReceived(DisconnectLoginPacket dlp)
        {
            ForgeLoginState = ForgeLoginStatus.Failed;
            DisconnectAsync(dlp.Json);
        }

        public override void Disconnect(string reason, bool reuseSocket = false)
        {
            base.Disconnect(reason, reuseSocket);
            _clientModList = null;
            _serverModList = null;
            _serverChannels = null;
            Channel.Reset();
        }

    }
}
