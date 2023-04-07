using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Utils;
using System;
using System.Buffers;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO
{
    public abstract partial class NetworkListener : INetworkListener
    {

        public event CommonEventHandler<object, ListenEventArgs> StartListen;
        public event CommonEventHandler<object, ListenEventArgs> StopListen;
        public event CommonEventHandler<object, UnhandledIOExceptionEventArgs> UnhandledException;

        protected static IPool<SocketAsyncEventArgs> SAEAPool = new SocketAsyncEventArgsPool();

        internal static Bucket<byte> _bufferPool;
        protected CancellationTokenSource _internalToken;
        protected Socket _socket;
        protected int _bufferOffset;
        protected byte[] _buffer;
        internal protected bool _usePool;

        protected bool _disposed = false;
        private int _syncCount;

        public NetworkListener(Socket socket) : this(socket, false) { }
        public NetworkListener(Socket socket,  bool disablePool)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            if (!disablePool && _bufferPool == null)
                SetPoolSize(socket.ReceiveBufferSize, 2048);

            _usePool = !disablePool;
            _socket = socket;
            AllocateBuffer();
        }

        public static void SetPoolSize(int bufferLength, int numberOfBuffers)
        {
            if (_bufferPool != null)
                throw new InvalidOperationException("数组池已被设置");

            _bufferPool = new Bucket<byte>(bufferLength, numberOfBuffers, Thread.CurrentThread.ManagedThreadId, true);
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

            _internalToken.Token.Register(() => EventUtils.InvokeCancelEvent(StopListen, this, new ListenEventArgs(true)));
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
                InvokeUnhandledException(new SocketException((int)e.SocketError));
                _internalToken.Cancel();
            }
            else if (e.BytesTransferred <= 0 && _socket != null && !NetworkUtils.CheckConnect(_socket))
            {
                InvokeUnhandledException(new SocketException((int)SocketError.ConnectionReset));
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

             
            AllocateBuffer();
            try
            {
                e.SetBuffer(_buffer);

                if (!_socket.ReceiveAsync(e))
                {
                    //超过96层递归就强制异步结束掉递归
                    if (++_syncCount > 96)
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
                if (!InvokeUnhandledException(ex))
                    throw;
            }

        }

        
        /// <returns>异常是否被处理</returns>
        protected bool InvokeUnhandledException(Exception exception)
        {
            UnhandledIOExceptionEventArgs eventArgs = new UnhandledIOExceptionEventArgs(exception);
            EventUtils.InvokeCancelEvent(UnhandledException, this, eventArgs);
            return eventArgs.Handled;
        }

        private void AllocateBuffer()
        {
            _buffer = _usePool ? _bufferPool.Rent() : new byte[8192];
            if(_buffer == null)
                _buffer = new byte[8192];
            
        }
    }
}
