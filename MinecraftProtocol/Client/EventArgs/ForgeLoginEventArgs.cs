using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class ForgeLoginEventArgs : LoginEventArgs
    {
        public ForgeLoginStatus Status { get; }

        public override bool IsSuccess => Status == ForgeLoginStatus.Success;

        public ForgeLoginEventArgs(ForgeLoginStatus status) : this(status, DateTime.Now) { }
        public ForgeLoginEventArgs(ForgeLoginStatus status, DateTime time) : base(time)
        {
            Status = status;
        }
    }
}
