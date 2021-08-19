using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class VanillaLoginEventArgs : LoginEventArgs
    {
        public VanillaLoginStatus Status { get; }

        public override bool IsSuccess => Status == VanillaLoginStatus.Success;

        public VanillaLoginEventArgs(VanillaLoginStatus status) : this(status, DateTime.Now) { }
        public VanillaLoginEventArgs(VanillaLoginStatus status, DateTime time) : base(time)
        {
            this.Status = status;
        }
    }
}
