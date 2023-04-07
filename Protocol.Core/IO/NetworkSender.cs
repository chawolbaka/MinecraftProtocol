using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using MinecraftProtocol.Utils;
using MinecraftProtocol.IO.Pools;

namespace MinecraftProtocol.IO
{
    /// <summary>
    /// 简单的通过阻塞队列和IOCP发送数据
    /// </summary>
    public class NetworkSender
    {
        private static ObjectPool<SendEventArgs> _sendEventArgsPool = new();
        private BlockingCollection<SendEventArgs> _sendQueue = new();
        private CancellationToken _token;
        private SendEventArgs _sea;
        private int _sendLength;
        private int _sendOffset;

        public virtual void Start(CancellationToken token)
        {
            _token = token;
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += (sender, e) => _sendOffset += e.BytesTransferred;
            e.Completed += OnSendCompleted;
            TakeNext();
            OnSendCompleted(this, e);
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            while (!_token.IsCancellationRequested)
            {
                try
                {
                    if (_sendOffset >= _sendLength)
                        TakeNext();

                    if (_sendOffset > 0)
                        e.SetBuffer(_sea.Data.Slice(_sendOffset));
                    else
                        e.SetBuffer(_sea.Data);

                    if (_sea.Socket.SendAsync(e))
                        return;
                    else
                        _sendOffset += e.BytesTransferred;

                    if (_sendOffset >= _sendLength)
                        TakeNext();
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }

            }
        }

        private void TakeNext()
        {
            _sea?.Callback?.Invoke();
            _sea?.Disposable?.Dispose();
            _sendEventArgsPool.Return(_sea);
            _sea = _sendQueue.Take();
            _sendLength = _sea.Data.Length;
            _sendOffset = 0;
        }


        public virtual void Enqueue(Socket socket, Memory<byte> data)
        {
            _sendQueue.Add(_sendEventArgsPool.Rent().Setup(socket, data));
        }
        public virtual void Enqueue(Socket socket, Memory<byte> data, Action callback)
        {
            _sendQueue.Add(_sendEventArgsPool.Rent().Setup(socket, data, callback));
        }
        public virtual void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable)
        {
            _sendQueue.Add(_sendEventArgsPool.Rent().Setup(socket, data, disposable));
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
                return this;
            }
            public SendEventArgs Setup(Socket socket, Memory<byte> data, IDisposable disposable)
            {
                Socket = socket;
                Data = data;
                Disposable = disposable;
                return this;
            }
            public SendEventArgs Setup(Socket socket, Memory<byte> data, Action callback)
            {
                Socket = socket;
                Data = data;
                Callback = callback;
                return this;
            }
        }
    }
}
