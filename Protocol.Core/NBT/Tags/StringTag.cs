using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.NBT.Tags
{
    public class StringTag : NBTTag, INBTPayload<string>
    {
        public override NBTTagType Type => NBTTagType.String;

        public string Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadString();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteString(Payload);
            return this;
        }
    }
}
