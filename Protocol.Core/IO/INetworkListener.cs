﻿using MinecraftProtocol.Utils;
using System;
using System.Threading;

namespace MinecraftProtocol.IO
{
    public interface INetworkListener : IDisposable
    {
        event CommonEventHandler<object, ListenEventArgs> StartListen;
        event CommonEventHandler<object, ListenEventArgs> StopListen;
        event CommonEventHandler<object, UnhandledIOExceptionEventArgs> UnhandledException;

        int ReceiveBufferSize { get; set; }
     
        void Start(CancellationToken token = default);
    }
}