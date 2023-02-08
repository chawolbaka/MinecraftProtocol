using System;
using System.IO;
using System.Text;
using MinecraftProtocol.IO.NBT.Tags;

namespace MinecraftProtocol.IO.NBT
{
    public class NBTWriter
    {
        protected virtual ByteWriter Writer { get; set; }
     
        public NBTWriter(ByteWriter writer)
        {
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public virtual NBTWriter WriteType(NBTTagType type)
        {
            Writer.WriteUnsignedByte((byte)type);
            return this;
        }
        public virtual NBTWriter WriteByte(byte value)      { Writer.WriteUnsignedByte(value); return this; }
        public virtual NBTWriter WriteShort(short value)    { Writer.WriteShort(value);        return this; }
        public virtual NBTWriter WriteInt(int value)        { Writer.WriteInt(value);          return this; }
        public virtual NBTWriter WriteLong(long value)      { Writer.WriteLong(value);         return this; }
        public virtual NBTWriter WriteFloat(float value)    { Writer.WriteFloat(value);        return this; }
        public virtual NBTWriter WriteDouble(double value)  { Writer.WriteDouble(value);       return this; }
        public virtual NBTWriter WriteBytes(ReadOnlySpan<byte> array) { WriteBytes(array);     return this; }
        public virtual NBTWriter WriteString(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                byte[] str = Encoding.UTF8.GetBytes(value);
                if (str.Length > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value));

                Writer.WriteUnsignedShort((ushort)str.Length);
                Writer.WriteBytes(str);
            }
            else
            {
                Writer.WriteUnsignedShort(0);
            }
            return this;
        }
        public virtual void WriteFile(string file)
        {
            File.WriteAllBytes(file, Writer.AsSpan().ToArray());
        }
    }
}
