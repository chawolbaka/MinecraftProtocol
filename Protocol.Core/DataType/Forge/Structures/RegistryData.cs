﻿using System;
using System.Collections.Generic;
using System.IO;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// The server sends several of this packet, one for each registry.
    /// It'll keep sending them until the hasMore value is no longer true.
    /// </summary>
    public class RegistryData : IForgeStructure, IEquatable<RegistryData>
    {
        /// <summary>Always 3 for RegistryData.</summary>
        public const byte Discriminator = 3;
        /// <summary>Marks whether another RegistryData packet will be sent after this.</summary>
        public bool HasMore;
        /// <summary>Name of the registry for this packet</summary>
        public string Name;
        /// <summary>Each id</summary>
        public Dictionary<string, int> Ids { get; set; } = new Dictionary<string, int>();
        /// <summary>Each substitution</summary>
        public List<string> Substitutions { get; set; } = new List<string>();
        /// <summary>Each dummy? May not be present in older versions of forge</summary>
        public List<string> Dummies { get; set; }

        public RegistryData() { }
        public RegistryData(bool hasMore, string name, Dictionary<string, int> ids, List<string> substitutions)
        {
            HasMore = hasMore;
            Name = name;
            Ids = new Dictionary<string, int>(ids ?? throw new ArgumentNullException(nameof(ids)));
            Substitutions = new List<string>(substitutions ?? throw new ArgumentNullException(nameof(substitutions)));
        }
        public RegistryData(bool hasMore, string name, Dictionary<string, int> ids, List<string> substitutions, List<string> dummies)
        {
            HasMore = hasMore;
            Name = name;
            Dummies = dummies;
            Ids = new Dictionary<string, int>(ids ?? throw new ArgumentNullException(nameof(ids)));
            Substitutions = new List<string>(substitutions ?? throw new ArgumentNullException(nameof(substitutions)));
        }

        public byte[] ToBytes()
        {
            ByteWriter data = new ByteWriter();
            data.WriteUnsignedByte(Discriminator);
            data.WriteBoolean(HasMore);
            data.WriteString(Name);

            data.WriteVarInt(Ids.Count);
            foreach (var id in Ids)
            {
                data.WriteString(id.Key);
                data.WriteVarInt(id.Value);
            }

            data.WriteStringArray(Substitutions);

            if (Dummies != null && Dummies.Count > 0)
                data.WriteStringArray(Dummies);
            return data.AsSpan().ToArray();
        }
        public static RegistryData Read(ReadOnlySpan<byte> data)
        {
            if (data.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "data length too short");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            RegistryData RD = new RegistryData();
            ByteReader reader = new ByteReader(data.Slice(1));
            RD.HasMore = reader.ReadBoolean();
            RD.Name = reader.ReadString();
            int IdsCount = reader.ReadVarInt();
            
            for (int i = 0; i < IdsCount; i++)
            {
                RD.Ids.Add(reader.ReadString(), reader.ReadVarInt());
            }
            
            RD.Substitutions.AddRange(reader.ReadStringArray());
            
            if (!reader.IsReadToEnd)
            {
                RD.Dummies = new List<string>(reader.ReadStringArray());
            }  
            return RD;
        }

        public override bool Equals(object obj) => Equals(obj as RegistryData);
        public bool Equals(RegistryData other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null || other.HasMore != HasMore || other.Name != Name)
                return false;


            return
                CollectionUtils.Compare(Ids, other?.Ids) &&
                CollectionUtils.Compare(Dummies, other?.Dummies) &&
                CollectionUtils.Compare(Substitutions, other?.Substitutions);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(HasMore, Name, Ids, Substitutions, Dummies);
        }
    }
}
