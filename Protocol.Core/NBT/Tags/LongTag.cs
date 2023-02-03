using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.NBT.Tags
{
    public class LongTag : NBTTag, INBTPayload<long>
    {
        public override NBTTagType Type => NBTTagType.Long;

        public long Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadLong();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteLong(Payload);
            return this;
        }
        public override string ToString() => Payload.ToString();
    }
}
