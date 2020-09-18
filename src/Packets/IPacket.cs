using System;
using System.Collections;

namespace MinecraftProtocol.Packets
{
    public interface IPacket
    {
        byte this[int index] { get; set; }

        int ID { get; }
        int Count { get; }
        byte[] ToBytes(int compress);
    }
}
