using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Compatible
{
    public enum ProtocolVersionType
    {
        Release,
        ReleaseCandidate,
        PreRelease,
        Snapshot,
    }
    public class ProtocolVersionAttribute : Attribute
    {
        public ProtocolVersionAttribute(string name, ProtocolVersionType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public ProtocolVersionType Type { get; }

    }
}
