﻿using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets.Client
{
    public partial class LoginPluginResponsePacket:DefinedPacket
    {
        [PacketProperty]
        private int _messageId;

        [PacketProperty("Channel")]
        private bool _successful;

        [PacketProperty("Data")]
        private byte[] _messageData;

        protected override void CheckProperty() { }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _messageId = reader.ReadVarInt();
            _successful = reader.ReadBoolean();
            if (!reader.IsReadToEnd) //是这样读吗？wiki没写清楚，有问题再改吧。
                _messageData = reader.ReadByteArray();
        }

        protected override void Write()
        {
            WriteVarInt(_messageId);
            WriteBoolean(_successful);
            WriteOptionalByteArray(_messageData);
        }

        public static int GetPacketId(int protocolVersion) => 0x02;
    }
}
