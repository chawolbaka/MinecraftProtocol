using System;
using System.Collections;

namespace MinecraftProtocol.Protocol.Packets
{
    public interface IPacket
    {
        byte this[int index] { get; set; }

        int ID { get; }
        int Length { get; }
        byte[] ToBytes(int compress);
    }
}
