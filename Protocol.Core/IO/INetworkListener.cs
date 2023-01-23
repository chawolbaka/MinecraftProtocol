using System;
using System.Threading;

namespace MinecraftProtocol.IO
{
    public interface INetworkListener : IDisposable
    {
        event EventHandler<ListenEventArgs> StartListen;
        event EventHandler<ListenEventArgs> StopListen;
        event EventHandler<UnhandledIOExceptionEventArgs> UnhandledException;

        int ReceiveBufferSize { get; set; }
     
        void Start(CancellationToken token = default);
    }
}