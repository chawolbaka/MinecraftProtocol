using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using MinecraftProtocol.Auth;
using MinecraftProtocol.Auth.Yggdrasil;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.Protocol.Packets;
using MinecraftProtocol.Protocol.Packets.Client;
using MinecraftProtocol.Protocol.Packets.Server;
using MinecraftProtocol.Compression;
using System.Threading.Tasks;

namespace MinecraftProtocol.Utils
{
    public enum VanillaLoginStatus
    {
        Handshake,
        LoginStart,
        EncryptionRequest,
        EncryptionResponse,
        SetCompression,
        Success,
        Failed
    }
    public delegate void VanillaLoginEventHandler(MinecraftClient sender, VanillaLoginEventArgs args);
    public class VanillaLoginEventArgs : LoginEventArgs
    {
        public VanillaLoginStatus Status { get; }

        public override bool IsSuccess => Status == VanillaLoginStatus.Success;

        public VanillaLoginEventArgs(VanillaLoginStatus status) : this(status, DateTime.Now) { }
        public VanillaLoginEventArgs(VanillaLoginStatus status, DateTime time) : base(time)
        {
            this.Status = status;
        }
    }

    /// <summary>
    /// 原版客户端
    /// </summary>
    public partial class VanillaClient : MinecraftClient, IDisposable
    {
        //如果连接断开了直接返回false，因为已经不可能是加入状态了。
        public bool Joined => Connected ? _joined : false;
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

        public bool IsServerInOnlineMode         { get => ThrowIfNotJoined(Crypto.Enable); }

        public int ReceiveBufferSize { get => ThrowIfDisposed(TCP.ReceiveBufferSize); set => TCP.ReceiveBufferSize = ThrowIfDisposed(value); }
        public bool AutoKeepAlive    { get => ThrowIfDisposed(_autoKeepAlive);        set => _autoKeepAlive = ThrowIfDisposed(value); }



        public override event PacketReceiveEventHandler PacketReceived  { add => _packetReceived += ThrowIfDisposed(value); remove => _packetReceived -= ThrowIfDisposed(value); }
        public override event SendPacketEventHandler PacketSend         { add => _packetSend     += ThrowIfDisposed(value); remove => _packetSend     -= ThrowIfDisposed(value); }

        public override event LoginEventHandler LoginSuccess              { add => _loginSuccess       += ThrowIfDisposed(value); remove => _loginSuccess       -= ThrowIfDisposed(value); }
        public virtual event VanillaLoginEventHandler LoginStatusChanged  { add => _loginStatusChanged += ThrowIfDisposed(value); remove => _loginStatusChanged -= ThrowIfDisposed(value); }
        public virtual event DisconnectEventHandler Kicked                { add => _kicked             += ThrowIfDisposed(value); remove => _kicked             -= ThrowIfDisposed(value); }
        public virtual event DisconnectEventHandler Disconnected          { add => _disconnected       += ThrowIfDisposed(value); remove => _disconnected       -= ThrowIfDisposed(value); }

        protected virtual event PacketReceiveEventHandler _packetReceived;
        protected virtual event SendPacketEventHandler _packetSend;
        protected virtual event VanillaLoginEventHandler _loginStatusChanged;
        protected virtual event LoginEventHandler _loginSuccess;
        protected virtual event DisconnectEventHandler _kicked;
        protected virtual event DisconnectEventHandler _disconnected;

        private VanillaLoginStatus _loginStatus;
        private VanillaLoginStatus LoginStatus
        {
            get => _loginStatus;
            set
            {
                if (value == VanillaLoginStatus.Success)
                    _loginSuccess?.BeginInvoke(this, new VanillaLoginEventArgs(value), null, null);

                _loginStatusChanged?.BeginInvoke(this, new VanillaLoginEventArgs(value), null, null);
                _loginStatus = value;
            }
        }

        protected CryptoHandler Crypto = new CryptoHandler();

        protected Socket TCP;
        protected Thread PacketQueueHandleThread;
        private bool _autoKeepAlive = true;

        public override Socket GetSocket() => TCP;


        /// <summary>
        /// 初始化一个原版客户端
        /// </summary>
        /// <param name="host">服务器地址(仅用于反向代理,如果服务器没有使用反向代理可以传入null)</param>
        /// <param name="serverIP">服务器IP地址(用于TCP连接)</param>
        /// <param name="serverPort">服务器端口号(用于TCP连接)</param>
        public VanillaClient(string host, IPAddress serverIP, ushort serverPort, int protocolVersion)
        {
            this.ServerHost = string.IsNullOrEmpty(host) ? serverIP.ToString() : host;
            this.ServerIP = serverIP;
            this.ServerPort = serverPort;
            this.ProtocolVersion = protocolVersion;

        }

        public VanillaClient(IPAddress ip, ushort port, int protocolVersion) : this(ip.ToString(), ip, port, protocolVersion) { }
        public VanillaClient(IPEndPoint remoteEP, int protocolVersion) : this(remoteEP.Address.ToString(), remoteEP.Address, (ushort)remoteEP.Port, protocolVersion) { }

        public override void Connect()
        {
            ThrowIfDisposed();
            TCP ??= new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { ReceiveBufferSize = 1024 * 512 };//不知道这样子能不能让开头的那些包快点接收完
            if (!TCP.Connected)
            {
                TCP.Connect(ServerIP, ServerPort);
                _connected = TCP.Connected;
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
                DisconnectAsync();
                disconnectReason = null;
                return false;
            }
            else if (LoginSuccessPacket.Verify(lastPacket, ProtocolVersion, out LoginSuccessPacket lsp))
            {
                LoginStatus = VanillaLoginStatus.Success;
                this._joined = true;
                disconnectReason = null;
                return true;
            }
            else if (DisconnectLoginPacket.Verify(lastPacket, ProtocolVersion, out DisconnectLoginPacket dp))
            {
                LoginStatus = VanillaLoginStatus.Failed;
                disconnectReason = dp.Reason;
                Disconnect(dp.Json);
                return false;
            }
            else
                throw new InvalidPacketException("登录末期接到了无法被处理的包", lastPacket);
        }
        protected virtual Packet SendLoginPacket(SessionToken token)
        {
            ThrowIfDisposed();

            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrEmpty(token.PlayerName))
                throw new ArgumentNullException(nameof(token.PlayerName));
            if (!TCP.Connected)
                throw new SocketException((int)SocketError.NotConnected);
            if (ProtocolVersion < 0)
                throw new LoginException("协议号不能小于0");

            //开始握手
            SendPacket(new HandshakePacket(ServerHost, ServerPort, ProtocolVersion, HandshakePacket.State.Login));
            LoginStatus = VanillaLoginStatus.Handshake;
            //申请加入服务器
            SendPacket(new LoginStartPacket(token.PlayerName, ProtocolVersion));
            LoginStatus = VanillaLoginStatus.LoginStart;

            Packet ServerResponse = ReadPacket();
            if (ServerResponse != null && EncryptionRequestPacket.Verify(ServerResponse, ProtocolVersion, out EncryptionRequestPacket EncryptionRequest))
            {
                //原版遇到这种情况是直接断开连接的,所以这边也直接断开吧
                if (token.AccessToken == null)
                    throw new LoginException($"服务器开启了正版验证，但是{nameof(SessionToken)}中没有提供可用的AccessToken。", Disconnect);

                LoginStatus = VanillaLoginStatus.EncryptionRequest;
                byte[] SecretKey = CryptoUtils.GenerateSecretKey();
                string ServerHash = CryptoUtils.GetServerHash(EncryptionRequest.ServerID, SecretKey, EncryptionRequest.PublicKey);
                YggdrasilService.Join(token, ServerHash);
                RSA RSAService = RSA.Create();
                RSAService.ImportSubjectPublicKeyInfo(EncryptionRequest.PublicKey, out _);
                SendPacket(new EncryptionResponsePacket(
                    RSAService.Encrypt(SecretKey, RSAEncryptionPadding.Pkcs1),
                    RSAService.Encrypt(EncryptionRequest.VerifyToken, RSAEncryptionPadding.Pkcs1),
                    ProtocolVersion));
                LoginStatus = VanillaLoginStatus.EncryptionResponse;

                Crypto.Init(SecretKey);
                ServerResponse = ReadPacket();
            }
            if (ServerResponse != null && SetCompressionPacket.Verify(ServerResponse, ProtocolVersion, out int? threshold))
            {
                this.CompressionThreshold = threshold.Value;
                LoginStatus = VanillaLoginStatus.SetCompression;
                ServerResponse = ReadPacket();
            }
            return ServerResponse;
        }

        //抄的,不知道为什么要加readonly 也不知道去掉会有什么区别
        private readonly object SendPacketLock = new object();
        private readonly object ReadPacketLock = new object();

        protected override Packet DecodePacket(ReadOnlySpan<byte> packet) => ThrowIfNotConnected(Crypto.Enable ? base.DecodePacket(Crypto.Decrypt(packet.ToArray())) : base.DecodePacket(packet));
        protected override int ReceivePacketLength()
        {
            return ThrowIfNotConnected(VarInt.Read(() =>
            {
                byte[] buffer = new byte[1];
                TCP.Receive(buffer);
                return Crypto.Enable ? Crypto.Decrypt(buffer[0]) : buffer[0];
            }));
        }
        protected virtual Packet ReadPacket()
        {
            lock (ReadPacketLock)
            {
                int ReadCount = 0, RetryCount = 0;
                int PacketLength = VarInt.Read(TCP);
                if (PacketLength == 0 && !UpdateConnectStatus())
                    return null;

                Span<byte> Data = new byte[PacketLength];
                while (Connected && ReadCount < PacketLength)
                {
                    //简单的防止一下死循环
                    if (++RetryCount >= 128 && !UpdateConnectStatus())
                        DisconnectAsync();
                    else
                        ReadCount += TCP.Receive(Data.Slice(ReadCount), SocketFlags.None);
                }
                return DecodePacket(Data);
            }
        }
        public override void SendPacket(IPacket packet)
        {

            ThrowIfDisposed();
            lock (SendPacketLock)
            {
                if (TCP == null || !_connected)
                    throw new InvalidOperationException("TCP未连接");
                if (_packetSend != null)
                {
                    foreach (var item in _packetSend.GetInvocationList())
                    {
                        SendPacketEventArgs args = new SendPacketEventArgs(packet);
                        ((SendPacketEventHandler)item)(this, args);
                        if (args.IsCancelled)
                            return;
                    }
                }

                if (Crypto.Enable)
                    TCP.Send(Crypto.Encrypt(packet.ToBytes(CompressionThreshold)));
                else
                    TCP.Send(packet.ToBytes(CompressionThreshold));
            }
        }

        protected virtual bool UpdateConnectStatus() => this._connected = ProtocolHandler.CheckConnect(TCP);
        public override void StartListen(CancellationTokenSource cancellationToken = default)
        {
            base.StartListen(cancellationToken);
            PacketQueueHandleThread = new Thread(() =>
            {
                try
                {
                    bool IsReserved = ServerIP.IsReserved();
                    int TimeSpanOffset = 0;
                    TimeSpan[] TimeSpans = IsReserved ? null : new TimeSpan[23];

                    while (Joined || !ReceivePacketCancellationToken.IsCancellationRequested)
                    {
                        if (_packetReceived != null && ReceiveQueue.TryDequeue(out var data))
                        {
                            if (!IsReserved && TimeSpanOffset < TimeSpans.Length && data.RoundTripTime.TotalMilliseconds > 1.0)
                                TimeSpans[TimeSpanOffset++] = data.RoundTripTime;
                            Delegate[] InvocationList = _packetReceived.GetInvocationList();
                            for (int i = 0; i < InvocationList.Length; i++)
                            {
                                //ReadOnlyPacket内部有个offset，所以必须保证大家拿到的不指向同一个引用。
                                PacketReceiveEventHandler Method = (PacketReceiveEventHandler)InvocationList[i];
                                PacketReceiveEventArgs EventArgs = new PacketReceiveEventArgs(data.Packet.AsReadOnly(), data.RoundTripTime, data.ReceivedTime);
                                Method.Invoke(this, EventArgs);
                                if (EventArgs.IsCancelled)
                                    break;
                            }
                        }
                        else
                        {
                            //我也不知道为什么就写了个这种东西，感觉好像并没有什么用。
                            if (!IsReserved && TimeSpanOffset > 1)
                            {
                                double avg = 0;
                                for (int i = --TimeSpanOffset; i >= 0; i--)
                                    avg += TimeSpans[i].TotalMilliseconds;
                                Thread.Sleep(avg > 0 ? (int)avg / TimeSpanOffset : 32);
                                TimeSpanOffset = 0;
                            }
                            else
                            {
                                Thread.Sleep(32);
                            }
                        }
                    }
                }
                catch (ObjectDisposedException) { }
            });
            PacketQueueHandleThread.Name = nameof(PacketQueueHandleThread);
            PacketQueueHandleThread.IsBackground = false;
            PacketQueueHandleThread.Start();
        }
        protected override bool BasePacketHandler(Packet packet)
        {
            if (packet.ID == DisconnectPacket.GetPacketID(ProtocolVersion))
            {
                _kicked?.Invoke(this, new DisconnectEventArgs(ChatMessage.Deserialize(packet.AsReadOnly().ReadString())));
                DisconnectAsync();
                return true;
            }
            else if (_autoKeepAlive && KeepAliveRequestPacket.Verify(packet, ProtocolVersion))
            {
                SendPacketAsync(new KeepAliveResponsePacket(packet.Data, ProtocolVersion));
                return true;
            }

            return false;
        }

        private readonly object DisconnectLock = new object();
        public override void Disconnect() => Disconnect("Unknown");
        public virtual Task DisconnectAsync(string reason, bool reuseSocket = false) => Task.Run(() => Disconnect(reason, reuseSocket));

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="reason">断开连接的原因（支持mc的那种json）</param>
        public virtual void Disconnect(string reason, bool reuseSocket = false)
        {
            ThrowIfDisposed();

            lock (DisconnectLock)
            {
                if (TCP == null)
                    return;
                StopListen();
                if (UpdateConnectStatus())
                {

                    TCP.Disconnect(reuseSocket);
                    TCP.Shutdown(SocketShutdown.Both);
                    TCP.Close();
                }
                _connected = false;
                PacketQueueHandleThread = null;
                TCP = null;
                CompressionThreshold = -1;
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
                this._joined = false;
                this._connected = false;
                TCP?.Dispose();
            }
            //DataStream?.Close();
            TCP?.Close();
        }
        ~VanillaClient()
        {
            Dispose(false);
        }


        protected virtual T ThrowIfNotJoined<T>(T value)
        {
            if (!Joined)
                throw new Exception("未加入服务器");
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
