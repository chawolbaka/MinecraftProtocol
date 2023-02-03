using MinecraftProtocol.IO;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MinecraftProtocol.NBT.Tags
{
    public class IntArrayTag : NBTTag, INBTPayload<int[]>, IEnumerable<int>
    {
        public override NBTTagType Type => NBTTagType.IntArray;

        public int[] Payload { get; set; }



        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = new int[reader.ReadInt()];
            for (int i = 0; i < Payload.Length; i++)
            {
                Payload[i] = reader.ReadInt();
            }
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteInt(Payload.Length);
            foreach (var l in Payload)
            {
                writer.WriteInt(l);
            }
            return this;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)Payload).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Payload.GetEnumerator();
        }
    }
}
