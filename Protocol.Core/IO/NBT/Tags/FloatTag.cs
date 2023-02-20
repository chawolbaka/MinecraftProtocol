using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class FloatTag : NBTTag, INBTPayload<float>
    {
        public override NBTTagType Type => NBTTagType.Float;

        public float Payload { get; set; }

        public override NBTTag Read(ref NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadFloat();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteFloat(Payload);
            return this;
        }
        public override string ToString() => Payload.ToString();
    }
}
