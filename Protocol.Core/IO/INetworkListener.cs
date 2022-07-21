using System;
using System.Threading;

namespace MinecraftProtocol.IO
{
    public interface INetworkListener : IDisposable
    {
        event EventHandler<NetworkListener.ListenEventArgs> StartListen;
        event EventHandler<NetworkListener.ListenEventArgs> StopListen;
        event EventHandler<NetworkListener.UnhandledExceptionEventArgs> UnhandledException;

        int ReceiveBufferSize { get; set; }
     
        void Start(CancellationToken token = default);
    }
}