using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.IO
{
    /// <summary>
    /// 简单的通过阻塞队列和IOCP发送数据
    /// </summary>
    public class NetworkSender
    {
        private static ObjectPool<SendEventArgs> _sendEventArgsPool = new();
        private ConcurrentQueue<SendEventArgs> _sendQueue;
        private BlockingCollection<SendEventArgs> _sendBlockingQueue;
        private ManualResetEvent _sendSignal = new(false);
        private bool _useBlockingQueue;

        public NetworkSender(bool useBlockingQueue)
        {
            _useBlockingQueue = useBlockingQueue;
            if (useBlockingQueue)
                _sendBlockingQueue = new();
            else
                _sendQueue = new();
        }

        public virtual void Start(CancellationToken token)
        {
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += (sender, e) => _sendSignal.Set();
            token.Register(() => _sendSignal.Set());

            while (!token.IsCancellationRequested)
            {
                SendEventArgs sea;
                if(_useBlockingQueue)
                {
                    sea = _sendBlockingQueue.Take();
                    if (sea == null)
                        continue;
                }
                else
                {
                    while (!_sendQueue.TryDequeue(out sea) || sea == null)
                    {
                        Thread.Sleep(40); //随便写的，我也不知道写多少才适合
                    }
                }


                int dataLength = sea.Data.Length;
                int send = 0;
                try
                {
                    do
                    {
                        if (send > 0)
                            e.SetBuffer(sea.Data.Slice(send));
                        else
                            e.SetBuffer(sea.Data);

                        if (sea.Socket.SendAsync(e))
                        {
                            _sendSignal.WaitOne();
                            _sendSignal.Reset();
                            if (token.IsCancellationRequested)
                            {
                                _sendSignal?.Dispose();
                                return;
                            }
                        }

                        if (e.SocketError != SocketError.Success && !NetworkUtils.CheckConnect(sea.Socket))
                            break;
                        else
                            send += e.BytesTransferred;

                    } while (send < dataLength);

                }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
                finally
                {
                    sea?.Callback?.Invoke();
                    sea?.Disposable?.Dispose();
                    _sendEventArgsPool.Return(sea);

                }
            }
        }


        public virtual void Enqueue(Socket socket, Memory<byte> data)
        {
            if (_useBlockingQueue)
                _sendBlockingQueue.Add(_sendEventArgsPool.Rent().Setup(socket, data));
            else
                _sendQueue.Enqueue(_sendEventArgsPool.Rent().Setup(socket, data));
        }
        public virtual void Enqueue(Socket socket, Memory<byte> data, Action callback)
        {
            if (_useBlockingQueue)
                _sendBlockingQueue.Add(_sendEventArgsPool.Rent().Setup(socket, data, callback));
            else
                _sendQueue.Enqueue(_sendEventArgsPool.Rent().Setup(socket, data, callback));
        }
        public virtual void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable)
        {
            if (_useBlockingQueue)
                _sendBlockingQueue.Add(_sendEventArgsPool.Rent().Setup(socket, data, disposable));
            else
                _sendQueue.Enqueue(_sendEventArgsPool.Rent().Setup(socket, data, disposable));
        }

        private class SendEventArgs
        {
            public Socket Socket;
            public Memory<byte> Data;
            public Action Callback;
            public IDisposable Disposable;

            public SendEventArgs() { }
            public SendEventArgs Setup(Socket socket, Memory<byte> data)
            {
                Socket = socket;
                Data = data;
                Disposable = null;
                Callback = null;
                return this;
            }
            public SendEventArgs Setup(Socket socket, Memory<byte> data, IDisposable disposable)
            {
                Socket = socket;
                Data = data;
                Disposable = disposable;
                Callback = null; 
                return this;
            }
            public SendEventArgs Setup(Socket socket, Memory<byte> data, Action callback)
            {
                Socket = socket;
                Data = data;
                Disposable = null;
                Callback = callback;
                return this;
            }
        }
    }
}
