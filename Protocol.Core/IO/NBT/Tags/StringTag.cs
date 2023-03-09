using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class StringTag : NBTTag, INBTPayload<string>
    {
        public override NBTTagType Type => NBTTagType.String;

        public string Payload { get; set; }

        public override NBTTag Read(ref NBTReader reader)
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


        public static implicit operator string(StringTag tag) => tag.Payload;

        public override string ToString() => Payload.ToString();
    }
}
