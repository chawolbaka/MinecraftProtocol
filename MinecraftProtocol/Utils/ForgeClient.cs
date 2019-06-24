using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Utils
{
    public class ForgeClient : VanillaClient
    {
        public ForgeClient(string serverHost, ushort port) : base(serverHost, port)
        {
            throw new NotImplementedException();
        }
    }
}
