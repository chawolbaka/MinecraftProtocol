using System;
using System.Collections.Generic;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.DataType.Forge
{
    [Obsolete("Removed in Minecraft 1.8")]
    public class ModIdData : IForgeStructure, IEquatable<ModIdData>
    {
        /// <summary>Always 3 for ModIdData</summary>
        public const byte Discriminator = 3;

        //public Dictionary<int,string> BlockList = new Dictionary<int, string>();
        //public Dictionary<int,string> ItemList = new Dictionary<int, string>();

        /// <summary>block/item. Prefixed \u0001 = block, \u0002 = item.</summary>
        public Dictionary<string, int> Mapping = new Dictionary<string, int>();
        public List<string> BlockSubstitutions = new List<string>();
        public List<string> ItemSubstitutions = new List<string>();

        public ModIdData() { }
        public ModIdData(Dictionary<string, int> mapping,List<string> blockSubstitutions, List<string> itemSubstitutions)
        {
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            BlockSubstitutions = blockSubstitutions ?? throw new ArgumentNullException(nameof(blockSubstitutions));
            ItemSubstitutions = itemSubstitutions ?? throw new ArgumentNullException(nameof(itemSubstitutions));
        }

        public byte[] ToBytes()
        {
            ByteWriter data = new ByteWriter();

            data.WriteUnsignedByte(Discriminator);
            data.WriteVarInt(Mapping.Count);
            foreach (var item in Mapping)
            {
                data.WriteString(item.Key);
                data.WriteVarInt(item.Value);
            }
            data.WriteStringArray(BlockSubstitutions);
            data.WriteStringArray(ItemSubstitutions);
            return data.AsSpan().ToArray();
        }

        public static ModIdData Read(List<byte> data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "data length too short");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            ModIdData MID = new ModIdData();
            ReadOnlySpan<byte> buffer = data.ToArray();
            
            int MapLength = buffer.Slice(1).AsVarInt(out buffer);
            for (int i = 0; i < MapLength; i++)
                MID.Mapping.Add(buffer.AsString(out buffer), buffer.AsVarInt(out buffer));

            buffer.ReadStringArray(out MID.BlockSubstitutions).ReadStringArray(out MID.ItemSubstitutions);
            return MID;
        }

        public override bool Equals(object obj) => Equals(obj as ModIdData);

        public bool Equals(ModIdData other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;

            return
                CollectionUtils.Compare(Mapping,Mapping)&&
                CollectionUtils.Compare(BlockSubstitutions, other?.BlockSubstitutions) &&
                CollectionUtils.Compare(ItemSubstitutions, other?.ItemSubstitutions);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mapping, BlockSubstitutions, ItemSubstitutions);
        }

    }
}
