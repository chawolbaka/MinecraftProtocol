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
        private static ObjectPool<SendEventArgs> SendEventArgsPool = new();

        private BlockingCollection<SendEventArgs> SendQueue = new();
        private ManualResetEvent SendSignal = new (false);

        public virtual void Enqueue(Socket socket, Memory<byte> data)
        {
            SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data, null));
        }
        public virtual void Enqueue(Socket socket, Memory<byte> data, Action callback)
        {
            SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data, callback));
        }
        public virtual void Enqueue(Socket socket, Memory<byte> data, IDisposable disposable) 
        {
            SendQueue.Add(SendEventArgsPool.Rent().Setup(socket, data, disposable != null ? disposable.Dispose : null));
        }

        public virtual void Start(CancellationToken token)
        {
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += (sender, e) => SendSignal.Set();
            token.Register(() => SendSignal.Set());

            while (!token.IsCancellationRequested)
            {
                SendEventArgs sea = SendQueue.Take(token);
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
                            SendSignal.WaitOne();
                            SendSignal.Reset();
                            if (token.IsCancellationRequested)
                            {
                                SendSignal?.Dispose();
                                return;
                            }
                        }

                        if (e.SocketError != SocketError.Success || (e.BytesTransferred <= 0 && !NetworkUtils.CheckConnect(sea.Socket)))
                            break;
                        else
                            send += e.BytesTransferred;

                    } while (send < dataLength);

                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
                finally
                {
                    sea?.Callback?.Invoke();
                    SendEventArgsPool.Return(sea);
                }
            }
        }

        private class SendEventArgs
        {
            public Socket Socket;
            public Memory<byte> Data;
            public Action Callback;

            public SendEventArgs() { }
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
