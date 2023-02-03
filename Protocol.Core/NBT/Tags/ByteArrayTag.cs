using MinecraftProtocol.IO;
using System;
using System.Collections.Generic;

namespace MinecraftProtocol.NBT.Tags
{
    public class ByteArrayTag : NBTTag, INBTPayload<byte[]>
    {
        public override NBTTagType Type => NBTTagType.ByteArray;

        public byte[] Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = new byte[reader.ReadInt()];
            for (int i = 0; i < Payload.Length; i++)
            {
                Payload[i] = reader.ReadByte();
            }
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteInt(Payload.Length);
            foreach (var l in Payload)
            {
                writer.WriteByte(l);
            }
            return this;
        }
    }
}
