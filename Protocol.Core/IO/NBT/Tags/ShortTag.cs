using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class ShortTag : NBTTag, INBTPayload<short>
    {
        public override NBTTagType Type => NBTTagType.Short;

        public short Payload { get; set; }

        public override NBTTag Read(ref NBTReader reader)
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

        public static implicit operator short(ShortTag tag) => tag.Payload;

        public override string ToString() => Payload.ToString();
    }
}
