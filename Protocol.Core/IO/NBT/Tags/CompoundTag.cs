using System;
using System.Collections.Generic;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class CompoundTag : NBTTag, INBTPayload<List<NBTTag>>
    {
        public override NBTTagType Type => NBTTagType.Compound;

        public List<NBTTag> Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if(!IsListItem)
                Name = reader.ReadString();
            NBTTagType type;
            Payload = new List<NBTTag>();
            do
            {
                type = reader.ReadType();
                switch (type)
                {
                    case NBTTagType.Byte:      Payload.Add(new ByteTag().Read(reader)); break;
                    case NBTTagType.Short:     Payload.Add(new ByteTag().Read(reader)); break;
                    case NBTTagType.Int:       Payload.Add(new IntTag().Read(reader)); break;
                    case NBTTagType.Long:      Payload.Add(new LongTag().Read(reader)); break;
                    case NBTTagType.Float:     Payload.Add(new FloatTag().Read(reader)); break;
                    case NBTTagType.Double:    Payload.Add(new DoubleTag().Read(reader)); break;
                    case NBTTagType.ByteArray: Payload.Add(new ByteArrayTag().Read(reader)); break;
                    case NBTTagType.String:    Payload.Add(new StringTag().Read(reader)); break;
                    case NBTTagType.List:      Payload.Add(new ListTag().Read(reader)); break;
                    case NBTTagType.Compound:  Payload.Add(new CompoundTag().Read(reader)); break;
                    case NBTTagType.IntArray:  Payload.Add(new IntArrayTag().Read(reader)); break;
                    case NBTTagType.LongArray: Payload.Add(new LongArrayTag().Read(reader)); break;
                    case NBTTagType.End: break;
                    default: throw new InvalidCastException($"Unknow type id {type}");
                }
            } while (type != NBTTagType.End);
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            foreach (var tag in Payload)
            {
                tag.Write(writer);
            }
            writer.WriteType(NBTTagType.End);
            return this;
        }
    }
}
