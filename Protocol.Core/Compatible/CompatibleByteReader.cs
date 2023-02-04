using MinecraftProtocol.DataType;
using MinecraftProtocol.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Compatible
{
    public class CompatibleByteReader : ByteReader
    {
        public int ProtocolVersion { get; set; }
        public CompatibleByteReader(ReadOnlyMemory<byte> data,int protocolVersion) : base(data)
        {
            ProtocolVersion = protocolVersion;
        }

        
        public virtual Position ReadPosition() => ReadPosition(ProtocolVersion);
        public virtual byte[] ReadByteArray() => ReadByteArray(ProtocolVersion);
        public virtual byte[] ReadOptionalByteArray() => ReadOptionalByteArray(ProtocolVersion);

    }
}
