using MinecraftProtocol.Compatible;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MinecraftProtocol.IO
{

    [ByteReader]
    public ref partial struct ByteReader
    {
        private ReadOnlySpan<byte> _data;

        private int _offset;

        public ByteReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _offset = 0;
        }

        public ByteReader(ref ReadOnlySpan<byte> data)
        {
            _data = data;
            _offset = 0;
        }

     
    }
}
