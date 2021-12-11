using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.IO.Pools;
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

        private static IPool<SocketAsyncEventArgs> Pool = new SocketAsyncEventArgsPool();

        private CancellationTokenSource _internalToken;
        private CryptoHandler _crypto;
        private Socket _socket;
        private int _offset;
        private int _receiveBufferSize;
        private int _compressionThreshold;
        private int _protocolVersion;
        private byte[] _buffer;
        private int _packetLength;
        private byte[] _packetData;
        private int _read;
        private ReadState _state;

        public PacketListener(Socket socket) : this(socket, socket.ReceiveBufferSize) { }
        public PacketListener(Socket socket, int receiveBufferSize)
        {
            if (receiveBufferSize < 32)
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), $"{nameof(receiveBufferSize)} too short.");
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            _socket = socket;
            _buffer = ArrayPool<byte>.Shared.Rent(receiveBufferSize);
            _receiveBufferSize = receiveBufferSize;
            _compressionThreshold = -1;
            _protocolVersion = -1;
            _crypto = new CryptoHandler();
            _internalToken = new CancellationTokenSource();
        }

        
        public void Start(CancellationToken token = default)
        {
            if (!NetworkUtils.CheckConnect(_socket))
                throw new InvalidOperationException("socket is not connected");

            if (token != default)
                token.Register(_internalToken.Cancel);
             

            StartListen?.Invoke(this, new ListenEventArgs(false));
            SocketAsyncEventArgs eventArgs = Pool.Rent();
            
            _internalToken.Token.Register(() => Pool.Return(eventArgs));
            _internalToken.Token.Register(() => StopListen?.Invoke(this, new ListenEventArgs(true)));
            _state = ReadState.PacketLength;
            eventArgs.UserToken = _socket;
            eventArgs.RemoteEndPoint = _socket.RemoteEndPoint;
            eventArgs.SetBuffer(_buffer);
            eventArgs.Completed -= ReceiveCompleted;
            eventArgs.Completed += ReceiveCompleted;
            ReceiveNextBuffer(eventArgs);
        }

        public void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            //检查连接状态
            if (e.SocketError != SocketError.Success)
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(new SocketException((int)e.SocketError)));
                _internalToken.Cancel();
            }
            else if (e.BytesTransferred <= 0 && !NetworkUtils.CheckConnect(e.UserToken as Socket))
            {
                UnhandledException?.Invoke(this, new UnhandledExceptionEventArgs(new SocketException((int)SocketError.ConnectionReset)));
                _internalToken.Cancel();
            }

            if (!_disposed && !_internalToken.IsCancellationRequested && _crypto.Enable)
                _crypto.Decrypt(_buffer, 0, e.BytesTransferred).AsSpan(0, e.BytesTransferred).CopyTo(_buffer);

            _offset = 0;
            while (!_disposed && !_internalToken.IsCancellationRequested)
            {
                try
                {
                    if (_state == ReadState.PacketLength)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            //如果当前buffer不足以读取一个完整的varint就等待缓存区刷新后继续向_readLength写入
                            if (!_disposed && _offset + 1 > e.BytesTransferred)
                            {
                                ReceiveNextBuffer(e);
                                return;
                            }
                            byte b = _buffer[_offset++];
                            _packetLength |= (b & 0b0111_1111) << i * 7;
                            if ((b & 0b1000_0000) == 0)
                            {
                                _packetData = ArrayPool<byte>.Shared.Rent(_packetLength);
                                _state = ReadState.PacketData;
                                break;
                            }
                        }
                    }

                    if (!_disposed && _read == 0 && e.BytesTransferred - _offset >= _packetLength)
                    {
                        //缓冲区内的可用数据足够读取一个包，直接按_packetLength取出来
                        InvokeReceived(e.MemoryBuffer.Span.Slice(_offset, _packetLength));
                        _offset += _packetLength;
                        _packetLength = 0;
                    }
                    else
                    {
                        //包的大小超出缓冲区，先会执行到else把现在缓冲区的数据都复制到_packetData内
                        //然后在下次接收到数据时继续回到这边，如果缓冲内的足够读取一个包了就进入下面，否则继续进else把接收到的填充进_packetData已被填充的数据后面
                        //（上面的read == 0确保数据收到后能回到这边而不是被上面的读取导致炸掉）
                        if (!_disposed && _packetLength - _read < e.BytesTransferred - _offset)
                        {
                            Array.Copy(_buffer, _offset, _packetData, _read, _packetLength - _read);
                            InvokeReceived(_packetData.AsSpan().Slice(0,_packetLength));

                            _offset += _packetLength - _read;
                            _packetLength = 0;
                            _read = 0;
                        }
                        else
                        {
                            Array.Copy(_buffer, _offset, _packetData, _read, e.BytesTransferred - _offset);
                            _read += e.BytesTransferred - _offset;
                            ReceiveNextBuffer(e);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_disposed)
                        return;
                    if (UnhandledException == null)
                        throw;

                    
                    UnhandledExceptionEventArgs args = new UnhandledExceptionEventArgs(ex);
                    UnhandledException(this, args);
                    if (!args.Handled)
                        throw;
                }
               
            }
        }
        private void ReceiveNextBuffer(SocketAsyncEventArgs e)
        {
            if (_receiveBufferSize != _buffer.Length)
            {
                _buffer = new byte[_receiveBufferSize];
                e.SetBuffer(_buffer);
            }

            if (!_disposed && !_socket.ReceiveAsync(e))
                ReceiveCompleted(this, e);
        }

        private void InvokeReceived(ReadOnlySpan<byte> data)
        {
            if (data.Length >= (_compressionThreshold > 0 ? 3 : 1)) //varint(size)+varint(decompressSize)+varint(id)+data 这是一个包最小的尺寸，不知道什么mod还是插件竟然会在玩家发送聊天消息后发个比这还小的东西过来...
                PacketReceived?.Invoke(this, new PacketReceivedEventArgs(CompatiblePacket.Depack(data, _protocolVersion, _compressionThreshold)));
            _state = ReadState.PacketLength;
            ArrayPool<byte>.Shared.Return(_packetData);
            _packetData = null;
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
            if (_packetData is not null)
                ArrayPool<byte>.Shared.Return(_packetData);
            if(_buffer is not null)
                ArrayPool<byte>.Shared.Return(_buffer);
      
            if (!disposed && disposing)
            {
                _packetData = null;
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
            public CompatiblePacket Packet { get; }

            public PacketReceivedEventArgs(CompatiblePacket packet)
            {
                ReceivedTime = DateTime.Now;
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
        private enum ReadState
        {
            PacketLength,
            PacketData
        }
    }
}
