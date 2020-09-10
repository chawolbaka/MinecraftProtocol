using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Utils
{
    
   public class LoginException:Exception
    {
        public delegate void Disconnect();
        public LoginException() : base() { }
        public LoginException(string message) : base(message) { }
        public LoginException(string message, Exception innerException) : base(message, innerException) { }

        public LoginException(Disconnect disconnect) : base() { disconnect(); }
        public LoginException(string message, Disconnect disconnect) : base(message) { disconnect(); }
        public LoginException(string message, Disconnect disconnect, Exception innerException) : base(message, innerException) { disconnect(); }
    }
}
