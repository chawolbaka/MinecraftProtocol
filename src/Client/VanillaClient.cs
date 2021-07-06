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
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Entity;
using MinecraftProtocol.IO;

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

        public bool IsServerInOnlineMode         { get => ThrowIfNotJoined(PacketListen.Crypto.Enable); }

        public bool AutoKeepAlive    { get => ThrowIfDisposed(_autoKeepAlive);        set => _autoKeepAlive = ThrowIfDisposed(value); }

        public delegate void VanillaLoginEventHandler(MinecraftClient sender, VanillaLoginEventArgs args);
        
        public override event PacketReceivedEventHandler PacketReceived   { add => _packetReceived     += ThrowIfDisposed(value); remove => _packetReceived -= ThrowIfDisposed(value); }
        public override event SendPacketEventHandler PacketSend           { add => _packetSend         += ThrowIfDisposed(value); remove => _packetSend     -= ThrowIfDisposed(value); }

        public override event LoginEventHandler LoginSuccess              { add => _loginSuccess       += ThrowIfDisposed(value); remove => _loginSuccess       -= ThrowIfDisposed(value); }
        public event VanillaLoginEventHandler VanillaLoginStatusChanged   { add => _loginStatusChanged += ThrowIfDisposed(value); remove => _loginStatusChanged -= ThrowIfDisposed(value); }

        //这两个事件有什么区别?
        //Kicked是收到了断开连接的包，Disconnected是Disconnect方法被调用
        public virtual event DisconnectEventHandler Kicked                { add => _kicked             += ThrowIfDisposed(value); remove => _kicked             -= ThrowIfDisposed(value); }
        public override event DisconnectEventHandler Disconnected         { add => _disconnected       += ThrowIfDisposed(value); remove => _disconnected       -= ThrowIfDisposed(value); }

        protected PacketReceivedEventHandler _packetReceived;
        protected SendPacketEventHandler _packetSend;
        
        protected LoginEventHandler _loginSuccess;
        protected DisconnectEventHandler _kicked;
        protected DisconnectEventHandler _disconnected;
        protected Player _player;

        private VanillaLoginEventHandler _loginStatusChanged;
        private void UpdateLoginStatus(VanillaLoginStatus status)
        {
            if (status == VanillaLoginStatus.Success)
                _loginSuccess?.Invoke(this, new VanillaLoginEventArgs(status));

            _loginStatusChanged?.Invoke(this, new VanillaLoginEventArgs(status));
        }


        protected IClientSettings _settings;
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

        protected Socket TCP;
        protected PacketListener PacketListen;
        protected Thread PacketQueueHandleThread;
        private bool _autoKeepAlive = true;

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
            Settings = settings;
            ProtocolVersion = protocolVersion >= 0 ? protocolVersion : throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议号不能小于0");
            if (Settings == null)
                Settings = ProtocolVersion >= ProtocolVersionNumbers.V1_12_pre3 ? ClientSettings.Default : ClientSettings.LegacyDefault;
            _loginSuccess += (sender, e) => _player.Init(this);
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
            PacketListen = new PacketListener(TCP ??= new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp));
            PacketListen.PacketReceived += (sender, e) =>
            {
                if (DisconnectPacket.Verify(e.Packet, ProtocolVersion, out DisconnectPacket dp))
                {
                    _kicked?.Invoke(this, new DisconnectEventArgs(dp.Reason, e.ReceivedTime));
                    DisconnectAsync(dp.Reason.ToString());
                }
                else if (_autoKeepAlive && KeepAliveRequestPacket.Verify(e.Packet.AsReadOnly(), ProtocolVersion))
                    SendPacketAsync(new KeepAliveResponsePacket(e.Packet.AsSpan(), ProtocolVersion));
                else
                    ReceiveQueue.Enqueue((e.ReceivedTime, e.RoundTripTime, e.Packet));
            };
            PacketListen.UnhandledException += (sender, e) =>
            {
                if (e.Exception is ObjectDisposedException || (e.Exception is SocketException se && se.SocketErrorCode != SocketError.Success))
                {
                    StopListen();
                    if (!_disposed)
                        Disconnect();
                    e.Handled = true;
                }
                else if (e.Exception is OverflowException oe && oe.StackTrace.Contains(nameof(VarInt)))
                    throw new InvalidDataException("无法读取数据包长度", e.Exception);
                else
                    e.Handled = false;
            };
            if (!TCP.Connected)
            {
                TCP.Connect(ServerIP, ServerPort);
                if (!(_connected = NetworkUtils.CheckConnect(TCP)))
                    return false;

                UpdateLoginStatus(VanillaLoginStatus.Connected);
                return true;
            }
            else
            {
                throw new InvalidOperationException("socket已连接");
            }    
        }

        public override bool Join(string playerName, out ChatMessage disconnectReason) => Join(new SessionToken(null, playerName, null, null), out disconnectReason);
        public virtual bool Join(string email, string password) => Join(email, password, out _, out _);
        public virtual bool Join(string email, string password, out ChatMessage disconnectReason) => Join(email, password, out _, out disconnectReason);
        public virtual bool Join(string email, string password, out SessionToken token) => Join(email, password, out token, out _);
        public virtual bool Join(string email, string password, out SessionToken token, out ChatMessage disconnectReason)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            SessionToken Token = YggdrasilService.Authenticate(email, password);
            token = Token;
            return Join(Token, out disconnectReason);
        }
        public virtual bool Join(SessionToken token) => Join(token, out _);
        public virtual bool Join(SessionToken token, out ChatMessage disconnectReason)
        {
            ThrowIfDisposed();

            if (Joined)
                throw new InvalidOperationException("玩家已加入服务器.");

            Packet lastPacket = SendLoginPacket(token);
            if (lastPacket == null)
            {
                disconnectReason = null;
                DisconnectAsync();
                return false;
            }
            else if (LoginSuccessPacket.Verify(lastPacket, ProtocolVersion, out LoginSuccessPacket lsp))
            {
                SetPlayer(lsp);
                _joined = true;
                disconnectReason = null;
                UpdateLoginStatus(VanillaLoginStatus.Success);
                return true;
            }
            else if (DisconnectLoginPacket.Verify(lastPacket, ProtocolVersion, out DisconnectLoginPacket dp))
            {
                UpdateLoginStatus(VanillaLoginStatus.Failed);
                disconnectReason = dp.Reason;
                DisconnectAsync(dp.Json);
                return false;
            }
            else
                throw new InvalidPacketException("登录末期接到了无法被处理的包", lastPacket);
        }

        protected virtual Packet SendLoginPacket(SessionToken token)
        {
            ThrowIfNotConnected();

            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrEmpty(token.PlayerName))
                throw new ArgumentNullException(nameof(token.PlayerName),"玩家名不可为空");
            if (ProtocolVersion < 0)
                throw new LoginException("协议号不能小于0");

            //开始握手
            SendPacket(new HandshakePacket(ServerHost, ServerPort, ProtocolVersion, HandshakePacket.State.Login));
            UpdateLoginStatus(VanillaLoginStatus.Handshake);
            //申请加入服务器
            SendPacket(new LoginStartPacket(token.PlayerName, ProtocolVersion));
            UpdateLoginStatus(VanillaLoginStatus.LoginStart);

            Packet ServerResponse = ReadPacket();
            if (ServerResponse != null && EncryptionRequestPacket.Verify(ServerResponse, ProtocolVersion, out EncryptionRequestPacket EncryptionRequest))
            {
                //原版遇到这种情况是直接断开连接的,所以这边也直接断开吧
                if (token.AccessToken == null)
                    throw new LoginException($"服务器开启了正版验证，但是{nameof(SessionToken)}中没有提供可用的AccessToken。", Disconnect);

                UpdateLoginStatus(VanillaLoginStatus.EncryptionRequest);
                byte[] SessionKey = CryptoUtils.GenerateSecretKey();
                string ServerHash = CryptoUtils.GetServerHash(EncryptionRequest.ServerID, SessionKey, EncryptionRequest.PublicKey);
                YggdrasilService.Join(token, ServerHash);
                RSA RSAService = RSA.Create();
                RSAService.ImportSubjectPublicKeyInfo(EncryptionRequest.PublicKey, out _);
                SendPacket(new EncryptionResponsePacket(
                    RSAService.Encrypt(SessionKey, RSAEncryptionPadding.Pkcs1),
                    RSAService.Encrypt(EncryptionRequest.VerifyToken, RSAEncryptionPadding.Pkcs1),
                    ProtocolVersion));
                UpdateLoginStatus(VanillaLoginStatus.EncryptionResponse);

                PacketListen.Crypto.Init(SessionKey);
                ServerResponse = ReadPacket();
            }
            if (ServerResponse != null && SetCompressionPacket.Verify(ServerResponse, ProtocolVersion, out int? threshold))
            {
                CompressionThreshold = threshold.Value;
                PacketListen.CompressionThreshold = threshold.Value;
                UpdateLoginStatus(VanillaLoginStatus.SetCompression);
                ServerResponse = ReadPacket();
            }
            return ServerResponse;
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
                return _player;
        }

        protected virtual void SetPlayer(Packet packet)
        {
            ThrowIfDisposed();

            LoginSuccessPacket lsp;
            if (packet is LoginSuccessPacket)
                lsp = packet as LoginSuccessPacket;
            else if (!LoginSuccessPacket.Verify(packet, ProtocolVersion, out lsp))
                throw new InvalidPacketException(packet);
            _player = new Player(lsp.PlayerName, UUID.Parse(lsp.PlayerUUID));
        }


        //抄的,不知道为什么要加readonly 也不知道去掉会有什么区别
        private readonly object SendPacketLock = new object();
        private readonly object ReadPacketLock = new object();
        protected virtual Packet ReadPacket()
        {
            lock (ReadPacketLock)
            {
                int PacketLength = VarInt.Read(() => PacketListen.Crypto.Enable ? PacketListen.Crypto.Decrypt(NetworkUtils.ReceiveData(1, TCP))[0] : NetworkUtils.ReceiveData(1, TCP)[0]);
                if (PacketLength == 0 && !UpdateConnectStatus())
                    throw new SocketException((int)SocketError.ConnectionReset);

                if (PacketListen.Crypto.Enable)
                    return Packet.Depack(PacketListen.Crypto.Decrypt(NetworkUtils.ReceiveData(PacketLength, TCP)), CompressionThreshold);
                else
                    return Packet.Depack(NetworkUtils.ReceiveData(PacketLength, TCP), CompressionThreshold);
            }
        }
        public override void SendPacket(IPacket packet)
        {
            ThrowIfNotConnected();
            var packetSend = _packetSend;
            if (packetSend != null)
            {
                foreach (SendPacketEventHandler eventMethod in packetSend.GetInvocationList())
                {
                    SendPacketEventArgs args = new SendPacketEventArgs(packet);
                    eventMethod.Invoke(this, args);
                    if (args.IsCancelled)
                        return;
                }
            }
            byte[] data = PacketListen.Crypto.Enable? PacketListen.Crypto.Encrypt(packet.Pack(CompressionThreshold)): packet.Pack(CompressionThreshold);
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
        protected ConcurrentQueue<(DateTime ReceivedTime, TimeSpan RoundTripTime, Packet Packet)> ReceiveQueue = new ConcurrentQueue<(DateTime, TimeSpan, Packet)>();
        protected CancellationTokenSource ReceivePacketCancellationToken;
        public override void StartListen(CancellationTokenSource cancellationToken = default)
        {
            if (ReceivePacketCancellationToken != null)
                return;
            ReceivePacketCancellationToken = cancellationToken ?? new CancellationTokenSource();

            PacketQueueHandleThread = new Thread(() =>
            {
                lock (DisconnectLock)
                    if (Joined)
                        PacketListen.StartAsync(ReceivePacketCancellationToken.Token);
                try
                {
                    while (Joined || (ReceivePacketCancellationToken != null && !ReceivePacketCancellationToken.IsCancellationRequested))
                    {
                        if (_packetReceived != null && ReceiveQueue.TryDequeue(out var data))
                        {
                            foreach (PacketReceivedEventHandler Method in _packetReceived.GetInvocationList())
                            {
                                //ReadOnlyPacket内部有个offset，所以必须保证大家拿到的不指向同一个引用。
                                PacketReceivedEventArgs EventArgs = new PacketReceivedEventArgs(data.Packet.AsReadOnly(), data.RoundTripTime, data.ReceivedTime);
                                Method.Invoke(this, EventArgs);
                                if (EventArgs.IsCancelled)
                                    break;
                            }
                        }
                        else
                        {
                            Thread.Sleep(200);   
                        }
                    }
                }
                catch (ObjectDisposedException) { }
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
                PacketListen.Dispose();
                ReceiveQueue?.Clear();
                CompressionThreshold = -1;
                _player = null;
                PacketQueueHandleThread = null;
                ReceivePacketCancellationToken = null;
                _disconnected?.Invoke(this, new DisconnectEventArgs(reason));
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
                ReceiveQueue?.Clear();
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
