﻿using MinecraftProtocol.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.Pools
{
    public class CompatiblePacketPool : IPool<CompatiblePacket>
    {
        public bool AutoClear { get; set; }

        protected ConcurrentBag<CompatiblePacket> _packets;
        
        public CompatiblePacketPool(bool autoClear)
        {
            AutoClear = autoClear;
            _packets = new ConcurrentBag<CompatiblePacket>();
        }

        public virtual CompatiblePacket Rent()
        {
            if (_packets.TryTake(out CompatiblePacket cp) && !cp._disposed)
            {
                return cp;
            }
            else
            {
                byte[] empty = null;
                CompatiblePacket packet = new CompatiblePacket(-1, 0, 0, ref empty, -1, -1);
                packet._needDisable = false;
                return packet;
            }
        }

        public virtual void Return(CompatiblePacket packet)
        {
            if (packet == null)
                return;

            if (AutoClear)
                packet.ClearToNullable();
            _packets.Add(packet);
        }

        public virtual void Clear()
        {
            var packets = _packets; _packets = new ConcurrentBag<CompatiblePacket>();
            foreach (CompatiblePacket packet in packets)
            {
                packet?.Dispose();
            }
        }
    }
}
