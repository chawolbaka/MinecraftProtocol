using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Packets
{
    public class ReadOnlyCompatiblePacket : ReadOnlyPacket, ICompatiblePacket
    {
        public int ProtocolVersion => _packet.ProtocolVersion;
        public int CompressionThreshold => _packet.CompressionThreshold;

        private CompatiblePacket _packet;

        public ReadOnlyCompatiblePacket(CompatiblePacket packet) : base(packet)
        {
            _packet = packet;
        }

        public virtual byte[] Pack()
        {
            return base.Pack(CompressionThreshold);
        }
    }
}
