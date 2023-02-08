using System;
using System.Collections;
using System.Collections.Generic;

namespace MinecraftProtocol.IO.NBT.Tags
{
    public class LongArrayTag : NBTTag, INBTPayload<long[]>, IEnumerable<long>
    {
        public override NBTTagType Type => NBTTagType.LongArray;

        public long[] Payload { get; set; }

        public override NBTTag Read(NBTReader reader)
        {
            if (!IsListItem)
                Name = reader.ReadString();
            Payload = new long[reader.ReadInt()];
            for (int i = 0; i < Payload.Length; i++)
            {
                Payload[i] = reader.ReadLong();
            }
            return this;
        }

        public override NBTTag Write(NBTWriter writer)
        {
            WriteHeader(writer);
            writer.WriteInt(Payload.Length);
            foreach (var l in Payload)
            {
                writer.WriteLong(l);
            }
            return this;
        }

        public IEnumerator<long> GetEnumerator()
        {
            return ((IEnumerable<long>)Payload).GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Payload.GetEnumerator();
        }
    }
}
