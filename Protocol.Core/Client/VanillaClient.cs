using System;
using System.IO;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using MinecraftProtocol.Auth;
using MinecraftProtocol.Auth.Yggdrasil;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Entity;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Chat;
using Ionic.Zlib;

namespace MinecraftProtocol.Client
{
    /// <summary>
    /// 原版客户端
    /// </summary>
    public partial class VanillaClient : MinecraftClient, IDisposable
    {
        //如果连接断开了直接返回false，因为已经不可能是加入状态了。
        public override bool Joined => Connected ? _joined : false;
        //因为TCP的Connected有点延迟,所以这边以接收到Disconnect来分辨是不是断开连接了
        //(当然TCP.Connected优先级还是最高的,只要这个变成false就绝对是断开了的状态)
        public override bool Connected => TCP != null && TCP.Connected ? _connected : false;
        protected bool _connected;
        protected bool _joined;

        public override string ServerHost        { get => ThrowIfDisposed(base.ServerHost); protected set => ThrowIfDisposed(base.ServerHost = value); }
        public override IPAddress ServerIP       { get => ThrowIfDisposed(base.ServerIP);   protected set => ThrowIfDisposed(base.ServerIP = value); }
        public override ushort ServerPort        { get => ThrowIfDisposed(base.ServerPort); protected set => ThrowIfDisposed(base.ServerPort = value); }
        public override int CompressionThreshold { get => ThrowIfDisposed(base.CompressionThreshold); set => ThrowIfDisposed(base.CompressionThreshold = value); }
        public override int ProtocolVersion      { get => ThrowIfDisposed(base.ProtocolVersion);      set => ThrowIfDisposed(base.ProtocolVersion = value); }
        public virtual bool AutoKeepAlive        { get => ThrowIfDisposed(_autoKeepAlive);            set => _autoKeepAlive = ThrowIfDisposed(value); }
        public virtual bool IsServerInOnlineMode { get => ThrowIfNotJoined(PacketListen.CryptoHandler.Enable); }

        public virtual VanillaLoginStatus VanillaLoginState
        {
            get => ThrowIfDisposed(_vanillaLoginState);
            protected set
            {
                _vanillaLoginState = value;
                EventUtils.InvokeCancelEvent(_loginStatusChanged, this, new VanillaLoginEventArgs(value));
            }
        }

        public virtual IClientSettings Settings
        {
            get => ThrowIfDisposed(_settings);
            set
            {
                _settings = ThrowIfDisposed(value);
                if (Joined)
                    SendPacket(new ClientSettingsPacket(value, ProtocolVersion));
            }
        }

        public override event CommonEventHandler<MinecraftClient, PacketReceivedEventArgs> PacketReceived   { add => _packetReceived     += ThrowIfDisposed(value); remove => _packetReceived -= ThrowIfDisposed(value); }
        public override event CommonEventHandler<MinecraftClient, SendPacketEventArgs> PacketSend           { add => _packetSend         += ThrowIfDisposed(value); remove => _packetSend     -= ThrowIfDisposed(value); }

        public override event CommonEventHandler<MinecraftClient, LoginEventArgs> LoginSuccess              { add => _loginSuccess       += ThrowIfDisposed(value); remove => _loginSuccess       -= ThrowIfDisposed(value); }
        public event CommonEventHandler<VanillaClient, VanillaLoginEventArgs> VanillaLoginStatusChanged     { add => _loginStatusChanged += ThrowIfDisposed(value); remove => _loginStatusChanged -= ThrowIfDisposed(value); }

        //这两个事件有什么区别?
        //Kicked是收到了断开连接的包，Disconnected是Disconnect方法被调用
        public virtual event CommonEventHandler<MinecraftClient, DisconnectEventArgs> Kicked                { add => _kicked             += ThrowIfDisposed(value); remove => _kicked             -= ThrowIfDisposed(value); }
        public override event CommonEventHandler<MinecraftClient, DisconnectEventArgs> Disconnected         { add => _disconnected       += ThrowIfDisposed(value); remove => _disconnected       -= ThrowIfDisposed(value); }

        protected CommonEventHandler<MinecraftClient, PacketReceivedEventArgs> _packetReceived;
        protected CommonEventHandler<MinecraftClient, SendPacketEventArgs> _packetSend;

        protected CommonEventHandler<MinecraftClient, LoginEventArgs> _loginSuccess;
        protected CommonEventHandler<MinecraftClient, DisconnectEventArgs> _kicked;
        protected CommonEventHandler<MinecraftClient, DisconnectEventArgs> _disconnected;

        protected Thread PacketQueueHandleThread;
        protected PacketListener PacketListen;
        protected SessionToken LoginToken;
        protected Player Player;
        protected Socket TCP;

        private CommonEventHandler<VanillaClient, VanillaLoginEventArgs> _loginStatusChanged;
        private VanillaLoginStatus _vanillaLoginState;
        private bool _autoKeepAlive = true;
        private IClientSettings _settings;

        public override Socket GetSocket() => TCP;

        /// <summary>
        /// 初始化一个原版客户端
        /// </summary>
        /// <param name="host">服务器地址(仅用于反向代理,如果服务器没有使用反向代理可以传入null)</param>
        /// <param name="serverIP">服务器IP地址(用于TCP连接)</param>
        /// <param name="serverPort">服务器端口号(用于TCP连接)</param>
        public VanillaClient(string host, IPAddress serverIP, ushort serverPort, IClientSettings settings, int protocolVersion)
        {
            ServerHost = string.IsNullOrEmpty(host) ? serverIP.ToString() : host;
            ServerIP = serverIP;
            ServerPort = serverPort;
            ProtocolVersion = protocolVersion >= 0 ? protocolVersion : throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议号不能小于0");
            _settings = settings ?? (ProtocolVersion >= ProtocolVersions.V1_12_pre3 ? ClientSettings.Default : ClientSettings.LegacyDefault);
            _loginSuccess += (sender, e) => Player.Init(this);
        }
        public VanillaClient(string host, IPAddress serverIP, ushort serverPort, int protocolVersion) : this(host, serverIP, serverPort, null, protocolVersion) { }
        public VanillaClient(string host, IPEndPoint remoteEP, int protocolVersion) : this(host, remoteEP, null, protocolVersion) { }
        public VanillaClient(string host, IPEndPoint remoteEP, IClientSettings settings, int protocolVersion) : this(host, remoteEP.Address, (ushort)remoteEP.Port, settings, protocolVersion) { }
        public VanillaClient(IPAddress ip, ushort port, int protocolVersion) : this(ip.ToString(), ip, port, null, protocolVersion) { }
        public VanillaClient(IPAddress ip, ushort port, IClientSettings settings, int protocolVersion) : this(ip.ToString(), ip, port, settings, protocolVersion) { }
        public VanillaClient(IPEndPoint remoteEP, int protocolVersion) : this(remoteEP, null, protocolVersion) { }
        public VanillaClient(IPEndPoint remoteEP, IClientSettings settings, int protocolVersion) : this(remoteEP.Address.ToString(), remoteEP.Address, (ushort)remoteEP.Port, settings, protocolVersion) { }


        public override bool Connect()
        {
            ThrowIfDisposed();
            //初始化数据包监听器
            PacketListen = new PacketListener(TCP ??= new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp));
            PacketListen.PacketReceived += (sender, e) =>
            {
                if (e.Packet == PacketType.Play.Server.Disconnect)
                {
                    ChatComponent reason = e.Packet.AsDisconnect()?.Reason;
                    EventUtils.InvokeCancelEvent(_kicked, this, new DisconnectEventArgs(reason, e.ReceivedTime));
                    DisconnectAsync(reason?.ToString());
                }
                else if (_autoKeepAlive && e.Packet == PacketType.Play.Server.KeepAlive)
                    SendPacketAsync(new KeepAliveResponsePacket(e.Packet.AsCompatibleReadOnly()));
                else
                    ReceiveQueue.TryAdd(new PacketReceivedEventArgs(e));
            };
            PacketListen.UnhandledException += (sender, e) =>
            {
                if (e.Exception is ObjectDisposedException || (e.Exception is SocketException se && se.SocketErrorCode != SocketError.Success))
                {
                    StopListen();
                    if (!_disposed)
                        Disconnect(e.Exception.Message);
                    e.Handled = true;
                }
                else if (e.Exception is ZlibException && NetworkUtils.CheckConnect(TCP))
                    e.Handled = true;
                else if (e.Exception is OverflowException oe && oe.StackTrace.Contains(nameof(VarInt)))
                    throw new InvalidDataException("无法读取数据包长度", e.Exception);
                else
                    e.Handled = false;
            };
            ReceiveQueue = new ();

            //连接TCP
            if (!TCP.Connected)
            {
                TCP.Connect(ServerIP, ServerPort);
                if (!(_connected = NetworkUtils.CheckConnect(TCP)))
                    return false;

                VanillaLoginState = VanillaLoginStatus.Connected;
                return true;
            }
            else
            {
                throw new InvalidOperationException("socket已连接");
            }    
        }

        public override Task<bool> JoinAsync(string playerName) => JoinAsync(new SessionToken(null, playerName, null, null));
        public override async Task<bool> JoinAsync(SessionToken token)
        {
            ThrowIfNotConnected();

            if (Joined)
                throw new InvalidOperationException("玩家已加入服务器.");
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrEmpty(token.PlayerName))
                throw new ArgumentNullException(nameof(token.PlayerName), "玩家名不可为空");
            if (ProtocolVersion < 0)
                throw new LoginException("协议号不能小于0");

            LoginToken = token;

            //开始握手
            SendPacket(new HandshakePacket(ServerHost, ServerPort, HandshakePacket.State.Login, ProtocolVersion));
            VanillaLoginState = VanillaLoginStatus.Handshake;

            //申请加入服务器
            SendPacket(new LoginStartPacket(token.PlayerName, ProtocolVersion));
            VanillaLoginState = VanillaLoginStatus.LoginStart;
            int count = 0;
            while (VanillaLoginState != VanillaLoginStatus.Success && VanillaLoginState != VanillaLoginStatus.Failed)
            {
                using Packet packet = ReadPacket();
                if (++count > 20960)
                    throw new OverflowException("异常的登录过程，服务端发送的数据包过多");
                else if (EncryptionRequestPacket.TryRead(packet, ProtocolVersion, out EncryptionRequestPacket erp))
                    await OnEncryptionRequestReceived(erp);
                else if (SetCompressionPacket.TryRead(packet, ProtocolVersion, out SetCompressionPacket scp))
                    OnSetCompressionReceived(scp.Threshold);
                else if (LoginSuccessPacket.TryRead(packet, ProtocolVersion, out LoginSuccessPacket lsp))
                    OnLoginSuccessReceived(lsp);
                else if (DisconnectPacket.TryRead(packet, ProtocolVersion, out DisconnectPacket dp))
                    OnDisconnectLoginReceived(dp);
                else if (DisconnectLoginPacket.TryRead(packet, ProtocolVersion, out DisconnectLoginPacket dlp))
                    OnDisconnectLoginReceived(dlp);
#if DEBUG
                else
                    throw new LoginException($"收到未知的包，id = {packet.Id}");
#endif
            }
            return VanillaLoginState == VanillaLoginStatus.Success;
        }

        protected virtual async Task OnEncryptionRequestReceived(EncryptionRequestPacket encryptionRequest)
        {
            if (LoginToken.AccessToken == null)
                throw new LoginException($"服务器开启了正版验证，但是{nameof(SessionToken)}中没有提供可用的AccessToken。", Disconnect);

            VanillaLoginState = VanillaLoginStatus.EncryptionRequest;
            byte[] SessionKey = CryptoUtils.GenerateSecretKey();
            string ServerHash = CryptoUtils.GetServerHash(encryptionRequest.ServerID, SessionKey, encryptionRequest.PublicKey);
            await YggdrasilService.JoinAsync(LoginToken, ServerHash);
            RSA RSAService = RSA.Create();
            RSAService.ImportSubjectPublicKeyInfo(encryptionRequest.PublicKey, out _);
            SendPacket(new EncryptionResponsePacket(
                RSAService.Encrypt(SessionKey, RSAEncryptionPadding.Pkcs1),
                RSAService.Encrypt(encryptionRequest.VerifyToken, RSAEncryptionPadding.Pkcs1),
                ProtocolVersion));
            VanillaLoginState = VanillaLoginStatus.EncryptionResponse;

            PacketListen.CryptoHandler.Init(SessionKey);
        }

        protected virtual void OnSetCompressionReceived(int threshold)
        {
            CompressionThreshold = threshold;
            PacketListen.CompressionThreshold = threshold;
            VanillaLoginState = VanillaLoginStatus.SetCompression;
        }

        protected virtual void OnLoginSuccessReceived(LoginSuccessPacket lsp)
        {
            SetPlayer(lsp);
            VanillaLoginState = VanillaLoginStatus.Success;
            _joined = true;
            EventUtils.InvokeCancelEvent(_loginSuccess, this, new VanillaLoginEventArgs(VanillaLoginStatus.Success));
        }

        protected virtual void OnDisconnectLoginReceived(DisconnectPacket dp)
        {
            //DisconnectPacket一般不会在原版登录阶段收到，但Forge登录阶段会，而且可能会有什么插件会乱发...
            VanillaLoginState = VanillaLoginStatus.Failed;
            EventUtils.InvokeCancelEvent(_kicked, this, new DisconnectEventArgs(dp.Reason, DateTime.Now));
            DisconnectAsync(dp.Json);
        }

        protected virtual void OnDisconnectLoginReceived(DisconnectLoginPacket dlp)
        {
            VanillaLoginState = VanillaLoginStatus.Failed;
            DisconnectAsync(dlp.Json);
        }


        /// <summary>
        /// 获取Player，如果未加入服务器会返回null
        /// </summary>
        public override Player GetPlayer()
        {
            ThrowIfDisposed();

            if (!Joined)
                return null;
            else
                return Player;
        }

        protected virtual void SetPlayer(Packet packet)
        {
            ThrowIfDisposed();

            LoginSuccessPacket lsp;
            if (packet is LoginSuccessPacket)
                lsp = packet as LoginSuccessPacket;
            else if (!LoginSuccessPacket.TryRead(packet, ProtocolVersion, out lsp))
                throw new InvalidPacketException(packet);
            Player = new Player(lsp.PlayerName, lsp.PlayerUUID);
        }


        //抄的,不知道为什么要加readonly 也不知道去掉会有什么区别
        private readonly object SendPacketLock = new object();
        private readonly object ReadPacketLock = new object();
        protected virtual CompatiblePacket ReadPacket()
        {
            lock (ReadPacketLock)
            {
                int PacketLength = VarInt.Read(() => PacketListen.CryptoHandler.TryDecrypt(NetworkUtils.ReceiveData(1, TCP))[0]);
                if (PacketLength == 0 && !UpdateConnectStatus())
                    throw new SocketException((int)SocketError.ConnectionReset);

                return CompatiblePacket.Depack(PacketListen.CryptoHandler.TryDecrypt(NetworkUtils.ReceiveData(PacketLength, TCP)), ProtocolVersion, CompressionThreshold);
            }
        }
        public override void SendPacket(IPacket packet)
        {
            ThrowIfNotConnected();

            SendPacketEventArgs eventArgs = new SendPacketEventArgs(packet);
            EventUtils.InvokeCancelEvent(_packetSend, this, eventArgs);
            if (eventArgs.IsBlock)
                return;

            byte[] data = PacketListen.CryptoHandler.TryEncrypt(packet.Pack(CompressionThreshold));
            //因为会异步发送Packet，不知道在不锁的情况下会不会出现乱掉的情况
            lock (SendPacketLock)
            {
                int read = TCP.Send(data);
                while (TCP != null && _connected && read < data.Length)
                {
                    read += TCP.Send(data.AsSpan().Slice(read));
                }
            }
        }
        protected virtual bool UpdateConnectStatus() => _connected = NetworkUtils.CheckConnect(TCP);
        protected BlockingCollection<PacketReceivedEventArgs> ReceiveQueue;
        protected CancellationTokenSource ReceivePacketCancellationToken;
        public override void StartListen(CancellationTokenSource cancellationToken = default)
        {
            if (ReceivePacketCancellationToken != null)
                return;
            if (VanillaLoginState != VanillaLoginStatus.Success)
                throw new InvalidOperationException("未登录服务器");

            ReceivePacketCancellationToken = cancellationToken ?? new CancellationTokenSource();
            PacketQueueHandleThread = new Thread(() =>
            {
                lock (DisconnectLock)
                {
                    PacketListen.ProtocolVersion = ProtocolVersion;
                    PacketListen.Start(ReceivePacketCancellationToken.Token);
                }

                PacketReceivedEventArgs eventArgs = null;
                try
                {
                    while (Joined || (ReceivePacketCancellationToken != null && !ReceivePacketCancellationToken.IsCancellationRequested))
                    {
                        if (ReceiveQueue.TryTake(out eventArgs, Timeout.Infinite, ReceivePacketCancellationToken.Token) && eventArgs != null)
                        {
                            EventUtils.InvokeCancelEvent(_packetReceived, this, eventArgs, (sender, e) => e.Packet.Reset());
                            eventArgs?.RawEventArgs?.Dispose();
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                finally 
                {
                    ReceivePacketCancellationToken?.Cancel();
                    eventArgs?.RawEventArgs?.Dispose(); 
                }
            });
            PacketQueueHandleThread.Name = nameof(PacketQueueHandleThread);
            PacketQueueHandleThread.IsBackground = true;
            PacketQueueHandleThread.Start();
        }
        public override void StopListen() => ReceivePacketCancellationToken?.Cancel();

        private readonly object DisconnectLock = new object();
        public override void Disconnect() => Disconnect("Unknown");
        public virtual Task DisconnectAsync(string reason, bool reuseSocket = false) => Task.Run(()=> Disconnect(reason,reuseSocket));

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="reason">断开连接的原因（支持mc的那种json）</param>
        public virtual void Disconnect(string reason, bool reuseSocket = false)
        {
            ThrowIfDisposed();

            lock (DisconnectLock)
            {
                Socket socket = TCP;
                if (socket == null)
                    return;
                StopListen();
                if (UpdateConnectStatus())
                {
                    socket.Disconnect(reuseSocket);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                _joined = false;
				_connected = false;
                TCP = null;
                PacketListen?.Dispose();
                ReceiveQueue?.Dispose();
                CompressionThreshold = -1;
                Player = null;
                PacketQueueHandleThread = null;
                ReceivePacketCancellationToken = null;
                EventUtils.InvokeCancelEvent(_disconnected, this, new DisconnectEventArgs(reason));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            bool disposed = _disposed;
            _disposed = true;
            if (!disposed && disposing)
            {
                _joined = false;
                _connected = false;
                ReceivePacketCancellationToken?.Cancel();
                ReceiveQueue?.Dispose();
                TCP?.Dispose();
            }
            TCP?.Close();
        }
        ~VanillaClient()
        {
            Dispose(false);
        }


        protected virtual T ThrowIfNotJoined<T>(T value)
        {
            if (!Joined)
                throw new InvalidOperationException("未加入服务器");
            return ThrowIfDisposed(value);
        }
        protected virtual T ThrowIfNotConnected<T>(T value)
        {
            if (TCP == null || !_connected)
                throw new InvalidOperationException("TCP未连接");
            return ThrowIfDisposed(value);
        }
        protected virtual void ThrowIfNotConnected()
        {
            if (TCP == null || !_connected)
                throw new InvalidOperationException("TCP未连接");
            ThrowIfDisposed();
        }
        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
        protected virtual void ThrowIfDisposed(Action action)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            action?.Invoke();
        }
        protected virtual T ThrowIfDisposed<T>(T value)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            return value;
        }
    }
}
