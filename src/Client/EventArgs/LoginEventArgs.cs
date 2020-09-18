using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public abstract class LoginEventArgs : MinecraftClientEventArgs
    {
        public abstract bool IsSuccess { get; }

        public LoginEventArgs() : base() { }
        public LoginEventArgs(DateTime time) : base(time) { }
    }
}
