using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.NBT.Tags
{
    public class ShortTag : NBTTag, INBTPayload<short>
    {
        public override NBTTagType Type => NBTTagType.Short;

        public short Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadShort();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteShort(Payload);
            return this;
        }
    }
}
