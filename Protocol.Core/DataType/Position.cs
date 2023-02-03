using MinecraftProtocol.Compatible;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public struct Position
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public Position(ulong position, int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_14)
            {
                X = (int)(position >> 38);
                Y = (int)(position & 0xFFF);
                Z = (int)(position << 26 >> 38);
            }
            else
            {
                X = (int)(position >> 38);
                Y = (int)((position >> 26) & 0xFFF);
                Z = (int)(position << 38 >> 38);
            }
        }
        public Position(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public ulong Encode(int protocolVersion)
        {
            if (protocolVersion >= ProtocolVersions.V1_14)
                return ((((ulong)X) & 0x3FFFFFF) << 38) | ((((ulong)Z) & 0x3FFFFFF) << 12) | (((ulong)Y) & 0xFFF);
            else
                return ((((ulong)X) & 0x3FFFFFF) << 38) | ((((ulong)Y) & 0xFFF) << 26) | (((ulong)Z) & 0x3FFFFFF);
        }

    }
}
