using MinecraftProtocol.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.NBT.Tags
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
