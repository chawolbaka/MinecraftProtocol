using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class IntTag : NBTTag, INBTPayload<int>
    {
        public override NBTTagType Type => NBTTagType.Int;

        public int Payload { get; set; }

        public override NBTTag Read(ref NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadInt();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteInt(Payload);
            return this;
        }
        public override string ToString() => Payload.ToString();
    }
}
