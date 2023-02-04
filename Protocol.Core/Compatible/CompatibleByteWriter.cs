using MinecraftProtocol.DataType;
using MinecraftProtocol.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Compatible
{
    public class CompatibleByteWriter : ByteWriter
    {
        public int ProtocolVersion { get => ThrowIfDisposed(_protocolVersion); set => _protocolVersion = ThrowIfDisposed(value); }
        private int _protocolVersion;

        public CompatibleByteWriter(int protocolVersion) : base()
        {
            _protocolVersion = protocolVersion;
        }
        public CompatibleByteWriter(int capacity, int protocolVersion) : base(capacity)
        {
            _protocolVersion = protocolVersion;
        }
        public CompatibleByteWriter(int size, ref byte[] data, int protocolVersion) : base(size, ref data)
        {
            _protocolVersion = protocolVersion;
        }
        internal CompatibleByteWriter(ref int size, ref byte[] data, int protocolVersion) : base(ref size, ref data)
        {
            _protocolVersion = protocolVersion;
        }

        public virtual ByteWriter WritePosition(Position position) => WritePosition(position, ProtocolVersion);
        public virtual ByteWriter WriteByteArray(ReadOnlySpan<byte> bytes) => WriteByteArray(bytes, ProtocolVersion);
        public virtual ByteWriter WriteOptionalByteArray(ReadOnlySpan<byte> bytes) => WriteOptionalByteArray(bytes, ProtocolVersion);
    }
}
