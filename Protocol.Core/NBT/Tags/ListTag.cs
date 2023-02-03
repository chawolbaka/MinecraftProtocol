using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.NBT.Tags
{
    public class ListTag : NBTTag, INBTPayload<NBTTag[]>
    {
        public override NBTTagType Type => NBTTagType.List;

        public NBTTag[] Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            NBTTagType type = reader.ReadType();
            Payload = new NBTTag[reader.ReadInt()];
            if (Payload.Length <= 0)
                return this;

            for (int i = 0; i < Payload.Length; i++)
            {
                Payload[i] = type switch
                {
                    NBTTagType.Byte      => new ByteTag()      { IsListItem = true }.Read(reader),
                    NBTTagType.Short     => new ByteTag()      { IsListItem = true }.Read(reader),
                    NBTTagType.Int       => new IntTag()       { IsListItem = true }.Read(reader),
                    NBTTagType.Long      => new LongTag()      { IsListItem = true }.Read(reader),
                    NBTTagType.Float     => new FloatTag()     { IsListItem = true }.Read(reader),
                    NBTTagType.Double    => new DoubleTag()    { IsListItem = true }.Read(reader),
                    NBTTagType.ByteArray => new ByteArrayTag() { IsListItem = true }.Read(reader),
                    NBTTagType.String    => new StringTag()    { IsListItem = true }.Read(reader),
                    NBTTagType.List      => new ListTag()      { IsListItem = true }.Read(reader),
                    NBTTagType.Compound  => new CompoundTag()  { IsListItem = true }.Read(reader),
                    NBTTagType.IntArray  => new IntArrayTag()  { IsListItem = true }.Read(reader),
                    NBTTagType.LongArray => new LongArrayTag() { IsListItem = true }.Read(reader),
                    _ => throw new InvalidCastException($"Unknow type id {type}")
                };
            
            }
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            NBTTagType type = Payload[0].Type;
            WriteHeader(writer);
            writer.WriteType(type);
            writer.WriteInt(Payload.Length);
            for (int i = 0; i < Payload.Length; i++)
            {
#if DEBUG
                if (Payload[i].Type != type)
                    throw new InvalidCastException("ListTag中的Payload所有元素都必须使用相同的TagType");
#else
                if (Payload[i].Type == type)
#endif
                    Payload[i].Write(writer);
            }
            return this;
        }
    }
}
