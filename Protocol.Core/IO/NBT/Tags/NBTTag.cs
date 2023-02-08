using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public abstract class NBTTag
    {
        public abstract NBTTagType Type { get; }
        public virtual string Name { get; set; }
        public abstract NBTTag Write(NBTWriter writer);
        public abstract NBTTag Read(NBTReader reader);

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
    }
}
