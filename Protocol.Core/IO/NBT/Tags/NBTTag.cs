using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public abstract class NBTTag
    {
        public abstract NBTTagType Type { get; }
        public virtual string Name { get; set; }
        public abstract NBTTag Write(NBTWriter writer);
        public abstract NBTTag Read(ref NBTReader reader);

        public virtual bool IsListItem { get; set; }

        protected virtual NBTTag WriteHeader(NBTWriter writer)
        {
            if (!IsListItem)
            {
                writer.WriteType(Type);
                writer.WriteString(Name);
            }
            return this;
        }

        public static explicit operator byte  (NBTTag tag) => ((ByteTag)tag).Payload;
        public static explicit operator short (NBTTag tag) => ((ShortTag)tag).Payload;
        public static explicit operator int   (NBTTag tag) => ((IntTag)tag).Payload;
        public static explicit operator long  (NBTTag tag) => ((LongTag)tag).Payload;
        public static explicit operator float (NBTTag tag) => ((FloatTag)tag).Payload;
        public static explicit operator double(NBTTag tag) => ((DoubleTag)tag).Payload;
        public static explicit operator string(NBTTag tag) => ((StringTag)tag).Payload;

    }
}
