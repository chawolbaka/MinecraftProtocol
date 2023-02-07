using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Utils;
using System;
using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO
{
    public abstract partial class NetworkListener : INetworkListener
    {
        public int ReceiveBufferSize
        {
            get => ThrowIfDisposed(_receiveBufferSize);
            set => ThrowIfDisposed(() => _receiveBufferSize = value > 32 ? value : throw new ArgumentOutOfRangeException(nameof(ReceiveBufferSize), $"{nameof(ReceiveBufferSize)} too short."));
        }

        public virtual event CommonEventHandler<object, ListenEventArgs> StartListen;
        public virtual event CommonEventHandler<object, ListenEventArgs> StopListen;
        public virtual event CommonEventHandler<object, UnhandledIOExceptionEventArgs> UnhandledException;

        protected static IPool<SocketAsyncEventArgs> SAEAPool = new SocketAsyncEventArgsPool();

        internal static UnsafeSawtoothArrayPool<byte>.Bucket<byte> _dataPool;
        protected CancellationTokenSource _internalToken;
        protected Socket _socket;
        protected int _receiveBufferSize;
        protected int _bufferOffset;
        protected GCHandle _bufferGCHandle;
        protected byte[] _buffer;
        protected bool _usePool;
        private int _syncCount;

        public NetworkListener(Socket socket) : this(socket, socket.ReceiveBufferSize) { }
        public NetworkListener(Socket socket, int receiveBufferSize) : this(socket, receiveBufferSize, false) { }
        public NetworkListener(Socket socket, int receiveBufferSize, bool disablePool)
        {
            if (receiveBufferSize < 32)
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), $"{nameof(receiveBufferSize)} too short.");
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            if (!disablePool && _dataPool == null)
               _dataPool = new UnsafeSawtoothArrayPool<byte>.Bucket<byte>(receiveBufferSize, 2048, Thread.CurrentThread.ManagedThreadId, true);

            _usePool = !disablePool;
            _socket = socket;
            _buffer = AllocateByteArray();
            _receiveBufferSize = receiveBufferSize;
        }


        public virtual void Start(CancellationToken token = default)
        {
            if (_internalToken != default)
                throw new InvalidOperationException("started");

            if (!NetworkUtils.CheckConnect(_socket))
                throw new InvalidOperationException("socket is not connected");

            _internalToken = new CancellationTokenSource();
            if (token != default)
                token.Register(_internalToken.Cancel);

            EventUtils.InvokeCancelEvent(StartListen, this, new ListenEventArgs(false));
            SocketAsyncEventArgs eventArgs = _usePool ? SAEAPool.Rent() : new SocketAsyncEventArgs();
            if (_usePool)
                _internalToken.Token.Register(() => SAEAPool.Return(eventArgs));

            _internalToken.Token.Register(() => EventUtils.InvokeCancelEvent(StartListen, this, new ListenEventArgs(true)));
            eventArgs.RemoteEndPoint = _socket.RemoteEndPoint;
            eventArgs.SetBuffer(_buffer);
            eventArgs.Completed -= OnReceiveCompleted;
            eventArgs.Completed += OnReceiveCompleted;
            ReceiveNextBuffer(eventArgs);
        }

        protected abstract void ReceiveCompleted(object sender, SocketAsyncEventArgs e);

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            //检查连接状态
            if (e.SocketError != SocketError.Success)
            {
                UnhandledException?.Invoke(this, new UnhandledIOExceptionEventArgs(new SocketException((int)e.SocketError)));
                _internalToken.Cancel();
            }
            else if (e.BytesTransferred <= 0 && _socket != null && !NetworkUtils.CheckConnect(_socket))
            {
                UnhandledException?.Invoke(this, new UnhandledIOExceptionEventArgs(new SocketException((int)SocketError.ConnectionReset)));
                _internalToken.Cancel();
            }
            else
            {
                _bufferOffset = 0;
                ReceiveCompleted(sender, e);
            }
        }

        /// <summary>
        /// 继续接收数据到缓存区
        /// </summary>
        protected virtual void ReceiveNextBuffer(SocketAsyncEventArgs e)
        {
            if (_disposed)
                return;

             
            _buffer = AllocateByteArray();
            try
            {
                e.SetBuffer(_buffer);

                if (!_socket.ReceiveAsync(e))
                {
                    //超过128层递归就强制异步结束掉递归
                    if (++_syncCount > 128)
                    {
                        _syncCount = 0;
                        Task task = new Task(() => OnReceiveCompleted(this, e));
                        task.ConfigureAwait(false);
                        task.Start();
                    }
                    else
                    {
                        OnReceiveCompleted(this, e);
                    }
                }
                else
                {
                    _syncCount = 0;
                }
            }
            catch (Exception ex)
            {
                UnhandledIOExceptionEventArgs eventArgs = new UnhandledIOExceptionEventArgs(ex);
                EventUtils.InvokeCancelEvent(UnhandledException, this, eventArgs);
                if (!eventArgs.Handled)
                    throw;
            }

        }

        private byte[] AllocateByteArray()
        {
            GCHandle handle = _usePool ? _dataPool.Rent() : GCHandle.Alloc(new byte[_receiveBufferSize]);
            try
            {
                _bufferGCHandle = handle;
                return (byte[])handle.Target;
            }
            catch (InvalidOperationException)
            {
                _bufferGCHandle = GCHandle.Alloc(new byte[_receiveBufferSize]);
                return (byte[])_bufferGCHandle.Target;
            }
        }

        ~NetworkListener()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            bool disposed = _disposed;
            _disposed = true;
            if (_usePool && _buffer is not null)
                _dataPool?.Return(_bufferGCHandle);
            else if (_bufferGCHandle != default && _bufferGCHandle.IsAllocated)
                _bufferGCHandle.Free();

            if (!disposed && disposing)
            {
                _buffer = null;
                _socket = null;
                GC.SuppressFinalize(this);
            }
        }

        protected T ThrowIfDisposed<T>(T value)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            return value;
        }
        protected void ThrowIfDisposed(Action action)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
            action?.Invoke();
        }
    }
}
