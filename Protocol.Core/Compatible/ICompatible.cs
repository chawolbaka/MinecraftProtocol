using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Compatible
{
    public interface ICompatible
    {
        int ProtocolVersion { get; }
    }
}