using MinecraftProtocol.Compatible;
using MinecraftProtocol.IO.NBT.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.NBT
{
    public ref struct NBTReader
    {
        private ByteReader Reader;

        public NBTReader(ref CompatibleByteReader reader)
        {
            Reader = new ByteReader(reader.AsSpan());
        }

        public NBTReader(ref ByteReader reader)
        {
            Reader = reader;
        }

        public NBTTagType ReadType() => (NBTTagType)Reader.ReadUnsignedByte();
        public byte   ReadByte()     => Reader.ReadUnsignedByte();
        public short  ReadShort()    => Reader.ReadShort();
        public int    ReadInt()      => Reader.ReadInt();
        public long   ReadLong()     => Reader.ReadLong();
        public float  ReadFloat()    => Reader.ReadFloat();
        public double ReadDouble()   => Reader.ReadDouble();
        public byte[] ReadBytes(int length) => Reader.ReadBytes(length);
        public string ReadString()
        {
            int length = Reader.ReadUnsignedShort();
            if (length > 0)
                return Encoding.UTF8.GetString(Reader.ReadBytes(length));
            else
                return string.Empty;
        }


    }
}
