using MinecraftProtocol.DataType;
using MinecraftProtocol.IO;
using MinecraftProtocol.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Compatible
{
    [ByteReader]
    public ref partial struct CompatibleByteReader
    {
        //节省一点点内存分配，反正协议版本暂时不可能超过short
        public readonly short ProtocolVersion;
        private ReadOnlySpan<byte> _data;
        private int _offset;


        public CompatibleByteReader(ReadOnlySpan<byte> data, int protocolVersion)
        {
            ProtocolVersion = (short)protocolVersion;
            _data = data;
            _offset = 0;
        }

        public CompatibleByteReader(ref ReadOnlySpan<byte> data, int protocolVersion)
        {
            ProtocolVersion = (short)protocolVersion;
            _data = data;
            _offset = 0;
        }

        public Position ReadPosition() => ReadPosition(ProtocolVersion);
        public byte[] ReadByteArray() => ReadByteArray(ProtocolVersion);
        public byte[] ReadOptionalByteArray() => ReadOptionalByteArray(ProtocolVersion);

    }
}
