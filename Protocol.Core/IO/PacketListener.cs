using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.IO
{

    public sealed class PacketListener : IDisposable
    {
        public int ReceiveBufferSize
        {
            get => ThrowIfDisposed(_receiveBufferSize);
            set => ThrowIfDisposed(() => _receiveBufferSize = value > 32 ? value : throw new ArgumentOutOfRangeException(nameof(ReceiveBufferSize), $"{nameof(ReceiveBufferSize)} too short."));
        }

        public int CompressionThreshold
        {
            get => ThrowIfDisposed(_compressionThreshold);
            set => ThrowIfDisposed(() => _compressionThreshold = value);
        }

        public int ProtocolVersion
        {
            get => ThrowIfDisposed(_protocolVersion);
            set => ThrowIfDisposed(() => _protocolVersion = value);
        }

        public CryptoHandler Crypto => ThrowIfDisposed(_crypto);

        public event EventHandler<ListenEventArgs> StartListen;
        public event EventHandler<ListenEventArgs> StopListen;
        public event EventHandler<PacketReceivedEventArgs> PacketReceived;
        public event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        private Socket _socket;
        private CryptoHandler _crypto;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private int _offset;
        private int _receiveBufferSize;
        private int _compressionThreshold;
        private int _protocolVersion;
        private byte[] _buffer;
        private ManualResetEvent ReceiveSignal = new ManualResetEvent(false);

        public PacketListener(Socket socket) : this(socket, socket.ReceiveBufferSize) { }
        public PacketListener(Socket socket, int receiveBufferSize)
        {
            if (receiveBufferSize < 32)
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), $"{nameof(receiveBufferSize)} too short.");
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            _socket = socket;
            _buffer = new byte[receiveBufferSize];
            _receiveBufferSize = receiveBufferSize;
            _compressionThreshold = -1;
            _protocolVersion = -1;
            _crypto = new CryptoHandler();
        }

        public void StartAsync(CancellationToken token)
        {
            if (token == default)
                throw new ArgumentNullException(nameof(token));
            _cancellationToken = token;
            Thread thread = new Thread(Start);
            thread.Name = nameof(PacketListener);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Start()
        {
            if (!NetworkUtils.CheckConnect(_socket))
                throw new InvalidOperationException("socket is not connected");

            if (_cancellationToken == default)
                _cancellationToken = (cancellationTokenSource = new CancellationTokenSource()).Token;

            StartListen?.Invoke(this, new ListenEventArgs(false));
            using SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.UserToken = _socket;
            eventArgs.RemoteEndPoint = _socket.RemoteEndPoint;
            eventArgs.SetBuffer(_buffer);
            eventArgs.Completed += (sender, e) => ReceiveSignal.Set();
            UpdateBuffer(eventArgs);
            try
            {
                while (!_disposed&&!_cancellationToken.IsCancellationRequested)
                {
                    DateTime ReceiveStart = DateTime.Now;
                    int PacketLength = VarInt.Read(() =>
                    {
                        if (!_disposed && _offset + 1 > eventArgs.BytesTransferred)
                            UpdateBuffer(eventArgs);
                        return _buffer[_offset++];
                    });
                    Span<byte> Data = new byte[PacketLength];
                    if (!_disposed && eventArgs.BytesTransferred - _offset >= PacketLength)
                    {
                        Data = eventArgs.MemoryBuffer.Span.Slice(_offset, PacketLength);
                        _offset += PacketLength;
                    }
                    else
                    {
                        int read = 0;
                        while (!_disposed && !_cancellationToken.IsCancellationRequested && read < PacketLength)
                        {
                            if (PacketLength - read < eventArgs.BytesTransferred - _offset)
                            {
                                eventArgs.MemoryBuffer.Span.Slice(_offset, PacketLength - read).CopyTo(Data.Slice(read, PacketLength - read));
                                _offset += PacketLength - read;
                                break;
                            }
                            else
                            {
                                //把剩余的数据全部复制进去并读取下一块
                                eventArgs.MemoryBuffer.Span.Slice(_offset, eventArgs.BytesTransferred - _offset).CopyTo(Data.Slice(read, eventArgs.BytesTransferred - _offset));
                                read += eventArgs.BytesTransferred - _offset;
                                UpdateBuffer(eventArgs);
                            }
                        }
                    }

                    if (_disposed || _cancellationToken.IsCancellationRequested)
                        break;
                    if (Data.Length >= 3) //varint(size)+varint(decompressSize)+varint(id)+data 这是一个包最小的尺寸，不知道什么mod还是插件竟然会在玩家发送聊天消息后发个比这还小的东西过来...
                        PacketReceived?.Invoke(this, new PacketReceivedEventArgs(DateTime.Now - ReceiveStart, CompatiblePacket.Depack(Data, _protocolVersion, _compressionThreshold)));
                }
                StopListen?.Invoke(this, new ListenEventArgs(true));
            }
            catch (Exception e)
            {
                if (_disposed)
                    return;
                if (UnhandledException == null)
                    throw;

                UnhandledExceptionEventArgs args = new UnhandledExceptionEventArgs(e);
                UnhandledException(this, args);
                if (!args.Handled)
                    throw;
            }
        }

        private void UpdateBuffer(SocketAsyncEventArgs e)
        {
            if (_receiveBufferSize != _buffer.Length)
            {
                _buffer = new byte[_socket.ReceiveBufferSize];
                e.SetBuffer(_buffer);
            }

            if (!_disposed && _socket.ReceiveAsync(e))
                ReceiveSignal.WaitOne();

            //检查连接状态
            if (e.SocketError != SocketError.Success)
                throw new SocketException((int)e.SocketError);
            if (e.BytesTransferred <= 0 && !NetworkUtils.CheckConnect(e.UserToken as Socket))
                throw new SocketException((int)SocketError.ConnectionReset);

           if (!_disposed && _crypto.Enable)
                _crypto.Decrypt(_buffer, 0, e.BytesTransferred).AsSpan(0, e.BytesTransferred).CopyTo(_buffer);

            ReceiveSignal.Reset();
            _offset = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposed = false;
        private void Dispose(bool disposing)
        {
            bool disposed = _disposed;
            _disposed = true;
            if (!disposed && disposing)
            {
                cancellationTokenSource?.Cancel();
                _buffer = null;
                _socket = null;
                _crypto = null;
            }
        }
        ~PacketListener()
        {
            Dispose(false);
        }

        private T ThrowIfDisposed<T>(T value)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            return value;
        }
        private void ThrowIfDisposed(Action action)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            action?.Invoke();
        }


        public class ListenEventArgs : EventArgs
        {
            public DateTime Time { get; }
            public bool IsStop { get; }

            public ListenEventArgs(bool isStop)
            {
                Time = DateTime.Now;
                IsStop = isStop;
            }
        }

        public class PacketReceivedEventArgs : EventArgs
        {
            public DateTime ReceivedTime { get; }
            public TimeSpan RoundTripTime { get; }
            public CompatiblePacket Packet { get; }

            public PacketReceivedEventArgs(TimeSpan roundTripTime, CompatiblePacket packet)
            {
                ReceivedTime = DateTime.Now;
                RoundTripTime = roundTripTime;
                Packet = packet;
            }
        }
        public class UnhandledExceptionEventArgs : EventArgs
        {
            public Exception Exception { get; }

            /// <summary>
            /// 异常发生的时间
            /// </summary>
            public DateTime Time { get; }

            /// <summary>
            /// 阻止异常被抛出
            /// </summary>
            public bool Handled { get; set; }

            public UnhandledExceptionEventArgs(Exception exception)
            {
                Time = DateTime.Now;
                Exception = exception;
            }
        }
    }
}
