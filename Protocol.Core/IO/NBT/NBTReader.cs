using MinecraftProtocol.IO.NBT.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.IO.NBT
{
    public class NBTReader
    {
        protected virtual ByteReader Reader { get; set; }
        
        public NBTReader(ByteReader reader)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public virtual NBTTagType ReadType() => (NBTTagType)Reader.ReadUnsignedByte();
        public virtual byte   ReadByte()     => Reader.ReadUnsignedByte();
        public virtual short  ReadShort()    => Reader.ReadShort();
        public virtual int    ReadInt()      => Reader.ReadInt();
        public virtual long   ReadLong()     => Reader.ReadLong();
        public virtual float  ReadFloat()    => Reader.ReadFloat();
        public virtual double ReadDouble()   => Reader.ReadDouble();
        public virtual byte[] ReadBytes(int length) => Reader.ReadBytes(length);
        public virtual string ReadString()
        {
            int length = Reader.ReadUnsignedShort();
            if (length > 0)
                return Encoding.UTF8.GetString(Reader.ReadBytes(length));
            else
                return string.Empty;
        }


    }
}
