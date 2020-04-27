using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using MinecraftProtocol.IO;
using MinecraftProtocol.Auth;
using MinecraftProtocol.Auth.Yggdrasil;
using MinecraftProtocol.DataType;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.Protocol.Packets;
using MinecraftProtocol.Protocol.Packets.Client;
using MinecraftProtocol.Protocol.Packets.Server;
using System.Collections.Concurrent;

namespace MinecraftProtocol.Utils
{
    /// <summary>
    /// 原版客户端
    /// </summary>
    public class VanillaClient : MinecraftClient, IDisposable
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

        public bool IsServerInOnlineMode         { get => ThrowIfNotJoined(DataStream.Encrypted); }

        public int ReceiveBufferSize { get => ThrowIfDisposed(TCP.ReceiveBufferSize); set => TCP.ReceiveBufferSize = ThrowIfDisposed(value); }
        public bool AutoKeepAlive    { get => ThrowIfDisposed(_autoKeepAlive);        set => _autoKeepAlive = ThrowIfDisposed(value); }

        public override event PacketReceivedEventHandler PacketReceived { add => _packetReceived += ThrowIfDisposed(value); remove => _packetReceived -= ThrowIfDisposed(value); }
        public virtual event PacketReceivedEventHandler LoginSuccess    { add => _loginSuccess   += ThrowIfDisposed(value); remove => _loginSuccess   -= ThrowIfDisposed(value); }
        public virtual event PacketReceivedEventHandler LoginFailed     { add => _loginFailed    += ThrowIfDisposed(value); remove => _loginFailed    -= ThrowIfDisposed(value); }
        public virtual event PacketReceivedEventHandler Kicked          { add => _kicked         += ThrowIfDisposed(value); remove => _kicked         -= ThrowIfDisposed(value); }
        public virtual event EventHandler Disconnected                  { add => _disconnected   += ThrowIfDisposed(value); remove => _disconnected   -= ThrowIfDisposed(value); }

        protected virtual event PacketReceivedEventHandler _packetReceived;
        protected virtual event PacketReceivedEventHandler _loginSuccess;
        protected virtual event PacketReceivedEventHandler _loginFailed;
        protected virtual event PacketReceivedEventHandler _kicked;
        protected virtual event EventHandler _disconnected;

        public override MinecraftStream GetStream() => ThrowIfDisposed(DataStream);
        
        protected Socket TCP;
        protected Thread ListenThread;
        private MinecraftStream DataStream;
        private bool _autoKeepAlive = true;

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
            TCP ??= new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (!TCP.Connected)
            {
                TCP.Connect(ServerIP, ServerPort);
                _connected = TCP.Connected;
                DataStream = new MinecraftStream(new NetworkStream(TCP,false));
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

            if (this.Joined)
                throw new Exception("玩家已加入服务器.");

            Packet lastPacket = SendLoginPacket(token);
            if (LoginSuccessPacket.Verify(lastPacket, ProtocolVersion))
            {
                this._joined = true;
                _loginSuccess?.Invoke(this, new MinecraftClientEventArgs(lastPacket));
                disconnectReason = null;
                return true;
            }
            else if (DisconnectLoginPacket.Verify(lastPacket, ProtocolVersion, out DisconnectLoginPacket dp))
            {
                _loginFailed?.Invoke(this, new MinecraftClientEventArgs(lastPacket));
                disconnectReason = dp.Reason;
                Disconnect(); 
                return false;
            }
            else
                throw new InvalidPacketException("登录末期接到了不存在的包", lastPacket);
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

            Packet Handshake = new HandshakePacket(ServerHost, ServerPort, ProtocolVersion, HandshakePacket.State.Login);
            Packet LoginStart = new LoginStartPacket(token.PlayerName, ProtocolVersion);
            SendPacket(Handshake); 
            SendPacket(LoginStart);
            Packet ServerResponse = ReadPacket();
            if (EncryptionRequestPacket.Verify(ServerResponse, ProtocolVersion, out EncryptionRequestPacket EncryptionRequest))
            {
                //原版遇到这种情况是直接断开连接的,所以这边也直接断开吧
                if (token.AccessToken == null)    
                    throw new LoginException($"服务器开启了正版验证，但是{nameof(SessionToken)}中没有提供可用的AccessToken。", Disconnect);
                
                byte[] SecretKey = CryptoHandler.GenerateSecretKey();
                string ServerHash = CryptoHandler.GetServerHash(EncryptionRequest.ServerID, SecretKey, EncryptionRequest.PublicKey);
                YggdrasilService.Join(token, ServerHash);
                    RSA RSAService = RSA.Create();
                    RSAService.ImportSubjectPublicKeyInfo(EncryptionRequest.PublicKey, out _);
                    SendPacket(new EncryptionResponsePacket(
                        RSAService.Encrypt(SecretKey, RSAEncryptionPadding.Pkcs1),
                        RSAService.Encrypt(EncryptionRequest.VerifyToken, RSAEncryptionPadding.Pkcs1),
                        ProtocolVersion));

                    DataStream = DataStream.ToCryptoStream(SecretKey);
                    ServerResponse = ReadPacket();
            }
            if (SetCompressionPacket.Verify(ServerResponse, ProtocolVersion,out int? threshold))
            {
                this.CompressionThreshold = threshold.Value;
                ServerResponse = ReadPacket();
            }
            return ServerResponse;
        }

        //抄的,不知道为什么要加readonly 也不知道去掉会有什么区别
        private readonly object SendPacketLock = new object();
        protected virtual Packet ReadPacket() => ThrowIfDisposed(DataStream.ReadPacket(CompressionThreshold));
        public override void SendPacket(IPacket packet)
        {
            lock (SendPacketLock)
            {
                if (DataStream == null && !_connected)
                    throw new InvalidOperationException("TCP未连接");
                else
                    ThrowIfDisposed(() => DataStream.Write(packet.ToBytes(CompressionThreshold)));
            }
        }

        protected virtual bool UpdateConnectStatus() => this._connected = ProtocolHandler.CheckConnect(TCP);
        private bool CheckSocketError(SocketError se)
        {
            //我是不是不需要判断是哪种错误呀,反正抛出SocketException应该就是连接断开了
            return 
                se == SocketError.ConnectionRefused ||
                se == SocketError.ConnectionAborted ||
                se == SocketError.ConnectionReset ||
                !UpdateConnectStatus();
        }
        public override Thread StartListen() => StartListen(32, 36);
        public virtual Thread StartListen(int frequency, int roundTripTime)
        {
            ThrowIfDisposed();
            //必须加入服务器了才能开始监听包,登录阶段的包暂时是无法获取到的.
            if (!Joined)
                throw new InvalidOperationException("not joined");
            if (ListenThread != null && ListenThread.IsAlive)
                return ListenThread;

            ListenThread = new Thread(() =>
            {
                ConcurrentQueue<(DateTime Time,Packet Packet)> EventQueue = new ConcurrentQueue<(DateTime Time, Packet Packet)>();
                Task.Run(() => {
                    try
                    {
                        int RTT = roundTripTime < 2 ? 32 : roundTripTime;
                        while (Joined)
                        {
                            if (_packetReceived!=null&&EventQueue.TryDequeue(out var data))
                            {
                                Delegate[] invocationList = _packetReceived.GetInvocationList();
                                for (int i = 0; i < invocationList.Length; i++)
                                {
                                    //ReadOnlyPacket内部有个offset，所以必须保证大家拿到的不指向同一个引用。
                                    PacketReceivedEventHandler Method = (PacketReceivedEventHandler)invocationList[i];
                                    Method(this, new MinecraftClientEventArgs(data.Time, data.Packet.AsReadOnly()));
                                }
                            }
                            else
                            {
                                Task.Delay(RTT);
                            }
                        }
                    }
                    catch (ObjectDisposedException) { }
                });

                try
                {
                    int DisconnectPacketID = DisconnectPacket.GetPacketID(ProtocolVersion);
                    while (Joined)
                    {
                        if (TCP.Available <= 0)
                            Thread.Sleep(roundTripTime + frequency);

                        (DateTime Time, Packet Packet) Respones = (DateTime.Now, ReadPacket());
                       
                        if (Respones.Packet.ID == DisconnectPacketID)
                        {
                            _kicked?.Invoke(this, new MinecraftClientEventArgs(Respones.Time, Respones.Packet));
                            break;
                        }
                        //防止事件里面塞了太多东西卡住了来不及发送响应，所以直接塞在这里而不是独立建一个事件来处理心跳包
                        else if (_autoKeepAlive && KeepAliveRequestPacket.Verify(Respones.Packet, ProtocolVersion))
                            SendPacketAsync(new KeepAliveResponsePacket(Respones.Packet.Data, ProtocolVersion));
                        else if (_packetReceived != null)
                            EventQueue.Enqueue(Respones);
                    }
                }
                catch (ObjectDisposedException) { }
                catch (IOException ie) when (ie.InnerException is SocketException se)
                {
                    if (!CheckSocketError(se.SocketErrorCode))
                        throw;
                }
                catch (SocketException se)
                {
                    if (!CheckSocketError(se.SocketErrorCode))
                        throw;
                }
                catch (InvalidDataException)
                {
                    if (!UpdateConnectStatus())
                        throw;
                }
                finally
                {
                    EventQueue.Clear();
                    //Disconnect内部需要等待当前线程结束，所以这边需要在另外一个线程上面请求断开连接，然后结束当前线程，这样子另外一个线程才能执行完Disconnect。
                    DisconnectAsync();
                }
            });
            ListenThread.Name = "PacketListenThread";
            ListenThread.IsBackground = false;
            ListenThread.Start();
            return ListenThread;
        }
        public override void StopListen()
        {
            ThrowIfDisposed();
            _joined = false;
            while (ListenThread != null && ListenThread.IsAlive)
                Thread.Sleep(64);
        }
        

        public override void Disconnect() => Disconnect(false);
        public virtual void Disconnect(bool reuseSocket = false)
        {
            ThrowIfDisposed();
            //防止多次调用Disconnect导致离线事件被重复触发
            if (TCP == null) 
                return;
            
            StopListen();
            if(Connected)
            {
                _connected = false;
                TCP.Disconnect(reuseSocket);
                TCP.Shutdown(SocketShutdown.Both);
                TCP.Close();
            }
            _disconnected?.Invoke(this, new EventArgs());
            ListenThread = null;
            DataStream = null;
            TCP = null;
            //ProtocolVersion = -1;
            CompressionThreshold = -1;
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
                DataStream?.Dispose();
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
            if (DataStream == null && !_connected)
                throw new InvalidOperationException("TCP未连接");
            return ThrowIfDisposed(value);
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
