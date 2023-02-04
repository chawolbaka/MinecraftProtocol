﻿using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Packets
{
    public class ReadOnlyCompatiblePacket : ReadOnlyPacket, ICompatiblePacket
    {
        public int ProtocolVersion => _cpacket.ProtocolVersion;
        public int CompressionThreshold => _cpacket.CompressionThreshold;

        internal CompatiblePacket _cpacket;

        public virtual Position ReadPosition() => ReadPosition(ProtocolVersion);
        public virtual byte[] ReadByteArray() => ReadByteArray(ProtocolVersion);
        public virtual byte[] ReadOptionalByteArray() => ReadOptionalByteArray(ProtocolVersion);

        public ReadOnlyCompatiblePacket(CompatiblePacket packet) : base(packet)
        {
            _cpacket = packet;
        }

        public virtual byte[] Pack()
        {
            return base.Pack(CompressionThreshold);
        }
    }
}
