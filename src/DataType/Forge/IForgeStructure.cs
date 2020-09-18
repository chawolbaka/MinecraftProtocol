using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Forge
{
    public interface IForgeStructure
    {

        //string Channel { get; }
        byte[] ToBytes();
    }
}
