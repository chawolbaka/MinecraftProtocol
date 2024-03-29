﻿using MinecraftProtocol.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{

    public class DisconnectEventArgs : MinecraftClientEventArgs
    {
        public ChatComponent Reason { get; }
        private string rawJson;

        public DisconnectEventArgs(string reason) : this(reason, DateTime.Now) { }
        public DisconnectEventArgs(string reason, DateTime disconnectTime) : base(disconnectTime)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException(nameof(reason));
            this.rawJson = reason;
            this.Reason = ChatComponent.Deserialize(reason);
        }

        public DisconnectEventArgs(ChatComponent reason) : this(reason, DateTime.Now) { }
        public DisconnectEventArgs(ChatComponent reason, DateTime disconnectTime) : base(disconnectTime)
        {
            this.Reason = reason;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(rawJson))
                return Reason.Serialize();
            else
                return rawJson;
        }
    }
}
