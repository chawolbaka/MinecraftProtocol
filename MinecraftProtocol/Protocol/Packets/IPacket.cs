using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Protocol.Packets
{
    public interface IPacket
    {
        //int GetID();
        //IEnumerable<byte> GetData();
        byte[] ToBytes(int compress = -1);
    }
}
