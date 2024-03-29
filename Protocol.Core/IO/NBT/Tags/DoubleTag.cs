﻿using System;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class DoubleTag : NBTTag, INBTPayload<double>
    {
        public override NBTTagType Type => NBTTagType.Double;

        public double Payload { get; set; }

        public override NBTTag Read(ref NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = reader.ReadDouble();
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteDouble(Payload);
            return this;
        }
        
        public static implicit operator double(DoubleTag tag) => tag.Payload;

        public override string ToString() => Payload.ToString();
    }
}
