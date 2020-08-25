using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.Protocol.Packets;

namespace MinecraftProtocol
{
    public class MinecraftClientEventArgs : EventArgs
    {
        public virtual DateTime Time { get; }
        public MinecraftClientEventArgs() { this.Time = DateTime.Now; }
        public MinecraftClientEventArgs(DateTime time) { this.Time = time; }
    }

    public class PacketEventArgs : MinecraftClientEventArgs
    {
        public virtual bool IsCancelled => _isCancelled;
        private bool _isCancelled;

        public PacketEventArgs() : base() { }
        public PacketEventArgs(DateTime time): base(time) { }

        public virtual void Cancel()
        {
            _isCancelled = true;
        }
    }

    public class PacketReceiveEventArgs : PacketEventArgs
    {
        public virtual TimeSpan RoundTripTime { get; }
        public virtual ReadOnlyPacket Packet { get; }
        
        public PacketReceiveEventArgs(ReadOnlyPacket packet, TimeSpan roundTripTime) : this(packet, roundTripTime, DateTime.Now) { }
        public PacketReceiveEventArgs(ReadOnlyPacket packet, TimeSpan roundTripTime, DateTime time) : base(time)
        {
            this.Packet = packet;
            this.RoundTripTime = roundTripTime;
        }
    }

    public class SendPacketEventArgs : PacketEventArgs
    {
        public virtual IPacket Packet { get; }

        public SendPacketEventArgs(IPacket packet) : this(packet, DateTime.Now) { }
        public SendPacketEventArgs(IPacket packet, DateTime time) : base(time)
        {
            this.Packet = packet;
        }

        /// <summary>
        /// 阻止包被发送
        /// </summary>
        public override void Cancel() => base.Cancel(); //为了加个注释才重写的
        
    }

    public abstract class LoginEventArgs: MinecraftClientEventArgs
    {
        public abstract bool IsSuccess { get; }

        public LoginEventArgs(): base() { }
        public LoginEventArgs(DateTime time) : base(time) { }
    }

    public class DisconnectEventArgs : MinecraftClientEventArgs
    {
        public ChatMessage Reason { get; }
        private string rawJson;

        public DisconnectEventArgs(string reason) : this(reason, DateTime.Now) { }
        public DisconnectEventArgs(string reason, DateTime disconnectTime) : base(disconnectTime)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));
            this.rawJson = reason;
            this.Reason = ChatMessage.Deserialize(reason);
        }

        public DisconnectEventArgs(ChatMessage reason) : this(reason, DateTime.Now) { }
        public DisconnectEventArgs(ChatMessage reason, DateTime disconnectTime) : base(disconnectTime)
        {
            this.Reason = reason;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(rawJson))
                return Reason.Serialize();
            else
                return rawJson;
        }
    }

    public abstract class MinecraftClient
    {
        public const ushort DefaultServerPort = 25565;

        public virtual string ServerHost { get; protected set; }
        public virtual IPAddress ServerIP { get; protected set; }
        public virtual ushort ServerPort { get; protected set; }
        public virtual int CompressionThreshold { get; set; } = -1;
        public virtual int ProtocolVersion { get; set; } = -1;

        public delegate void DisconnectEventHandler(MinecraftClient sender, DisconnectEventArgs args);

        /// <summary>
        /// 接收到包事件
        /// </summary>
        public abstract event PacketReceiveEventHandler PacketReceived;
        public delegate void PacketReceiveEventHandler(MinecraftClient sender, PacketReceiveEventArgs args);

        /// <summary>
        /// 发送包事件
        /// </summary>
        public abstract event SendPacketEventHandler PacketSend;
        public delegate void SendPacketEventHandler(MinecraftClient sender, SendPacketEventArgs args);

        /// <summary>
        /// TCP连接状态
        /// </summary>
        public abstract bool Connected { get; }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// 发送登录服务器的请求
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <returns>如果是false代表登陆失败</returns>
        public virtual bool Join(string playerName) => Join(playerName, out _);

        /// <summary>
        /// 发送登录服务器的请求
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <param name="disconnectReason">无法加入服务器的原因</param>
        /// <returns>如果是false代表登陆失败</returns>
        public abstract bool Join(string playerName, out ChatMessage disconnectReason);


        /// <summary>
        /// 断开连接
        /// </summary>
        public abstract void Disconnect();
        public virtual Task DisconnectAsync() => Task.Run(Disconnect);


        private readonly object ReceivePacketLock = new object();
        protected CancellationTokenSource ReceivePacketCancellationToken;
       
        /// <summary>
        /// 开始监听数据包
        /// </summary>
        public virtual void StartListen(CancellationTokenSource cancellationToken = default)
        {
            lock(ReceivePacketLock)
            {
                if (ReceivePacketCancellationToken == null)
                {
                    ReceivePacketCancellationToken = cancellationToken ?? new CancellationTokenSource();
                    ReceiveNextPacket(GetSocket());
                }
            }
        }

        /// <summary>
        /// 请求停止监听数据包
        /// </summary>
        public virtual void StopListen() => ReceivePacketCancellationToken?.Cancel();

        /// <summary>
        /// 监听到的数据包会被塞到这个队列里面
        /// </summary>
        protected ConcurrentQueue<(DateTime ReceivedTime, TimeSpan RoundTripTime, Packet Packet)> ReceiveQueue = new ConcurrentQueue<(DateTime, TimeSpan, Packet)>();
        private class StateObject
        {

            //这个检查方法效率不行，我之后可能需要设置前几次使用高效一点的方法。
            public bool Connected => ProtocolHandler.CheckConnect(Socket);
            public DateTime StartTime;
            public Socket Socket;
            public int Length;
            public int Offset;
            public byte[] Data;

            public StateObject(Socket socket, int packetLength,DateTime startTime)
            {
                Socket = socket;
                Offset = 0;
                Length = packetLength;
                Data = new byte[packetLength];
                StartTime = startTime;
            }
        }
        private void ReceiveNextPacket(Socket tcp)
        {
            if (!Connected|| ReceivePacketCancellationToken.IsCancellationRequested) return;
            try
            {
                DateTime ReceiveStartTime = DateTime.Now;
                int PacketLength = VarInt.Read(tcp);
                if (PacketLength > 0)
                {
                    StateObject so = new StateObject(tcp, PacketLength, ReceiveStartTime);
                    tcp.BeginReceive(so.Data, 0, PacketLength, SocketFlags.None, new AsyncCallback(ReceiveCallback), so);

                }
                else if (!ProtocolHandler.CheckConnect(tcp))
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }
                else
                {
#if DEBUG
                    throw new InvalidDataException("发生了很魔法的错误，服务器并没有断开连接但是无法收到数据了(0 Byte)");
#endif
                }
            }
            catch (Exception e) 
            {
                if (!ReceiveExceptionHandler(e)) 
                    throw;
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!Connected || ReceivePacketCancellationToken.IsCancellationRequested) return;
            
            StateObject State = (StateObject)ar.AsyncState;
            try
            {
                State.Offset += State.Socket.EndReceive(ar);
                //如果已经完整的把包接收到了就开始解析包（好像这部分可以异步处理，但是异步可能会导致顺序乱掉）
                if (State.Offset == State.Length)
                {
                    DateTime ReceivedTime = DateTime.Now;
                    Packet packet = DecodePacket(State.Data);
                    //用于处理一些对优先级有要求的包，比如断开连接的包。
                    if (!BasePacketHandler(packet))
                        ReceiveQueue.Enqueue((ReceivedTime, ReceivedTime - State.StartTime, packet));
                    
                    //开始接收下一个包
                    ReceiveNextPacket(State.Socket);
                }
                else if (State.Offset < State.Length)
                {
                    if (!State.Connected)
                        throw new SocketException((int)SocketError.ConnectionReset);
                    if (State.Socket.Available <= 0)
                        Thread.Sleep(64);
                    State.Socket.BeginReceive(State.Data, State.Offset, State.Length - State.Offset, SocketFlags.None, new AsyncCallback(ReceiveCallback), State);
                }
                else
                {
                    throw new Exception("你遇到了魔法！");
                }
            }
            catch (Exception e)
            { 
                if (!ReceiveExceptionHandler(e)) 
                    throw;
            }
        }

        /// <summary>
        /// 接收到数据包后优先级最高的处理方法
        /// </summary>
        /// <param name="packet">接收到的包</param>
        /// <returns>阻止包被加入队列</returns>
        protected virtual bool BasePacketHandler(Packet packet) => false;

        /// <summary>
        /// 将接收到的数据解码成Packet
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        protected virtual Packet DecodePacket(ReadOnlySpan<byte> packet)
        {
            if (CompressionThreshold > 0)
            {
                int size = VarInt.Read(packet, out int SizeOffset);
                packet = packet.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    packet = ZlibUtils.Decompress(packet.ToArray(), size);
            }
            return new Packet(VarInt.Read(packet, out int IdOffset), packet.Slice(IdOffset));
        }

        /// <summary>
        /// 在接收数据包时发生的异常会交给这个方法处理
        /// </summary>
        /// <param name="e"></param>
        /// <returns>阻止异常被抛出</returns>
        protected virtual bool ReceiveExceptionHandler(Exception e)
        {
            if(e is ObjectDisposedException || (e is SocketException se && se.SocketErrorCode != SocketError.Success))
            {
                if (!ReceivePacketCancellationToken.IsCancellationRequested)
                    ReceivePacketCancellationToken.Cancel();
                DisconnectAsync();
                return true;
            }
            else if (e is OverflowException oe && oe.StackTrace.Contains(nameof(VarInt)))
            {
                throw new InvalidDataException("无法读取数据包长度", e);
            }
            else
            {
                return false;
            }
        }


        public abstract void SendPacket(IPacket packet);
        public virtual Task SendPacketAsync(IPacket packet)
        {
            Task task = Task.Run(() =>
             {
                 IPacket p = packet;
                 SendPacket(p);
             });
            return task;
        }


        /// <summary>
        /// 获取Socket,用于实现自己的的包监听(也就是说调用了StartListen就不要在获取了,不然不安全)
        /// </summary>
        public abstract Socket GetSocket();

        /// <summary>
        /// 获取服务器地址
        /// </summary>
        public override string ToString() => $"{ServerHost ?? (ServerIP != null ? ServerIP.ToString() : "Unknown")}{(ServerPort != DefaultServerPort ? $":{ServerPort}" : "")}";

    }
}
